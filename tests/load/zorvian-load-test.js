// k6 Load Testing Script for Zorvian ERP
// Run: k6 run zorvian-load-test.js
// Docs: https://k6.io/docs/

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const loginDuration = new Trend('login_duration');
const salesQueryDuration = new Trend('sales_query_duration');
const writeOperations = new Counter('write_operations');

// Test configuration
export const options = {
    stages: [
        { duration: '30s', target: 50 },   // Ramp up to 50 users
        { duration: '1m', target: 100 },   // Ramp to 100 users
        { duration: '3m', target: 200 },   // Sustain 200 users (normal load)
        { duration: '1m', target: 500 },   // Stress test: 500 users
        { duration: '30s', target: 1000 },  // Spike test: 1000 users
        { duration: '2m', target: 200 },   // Recovery
        { duration: '30s', target: 0 },     // Ramp down
    ],
    thresholds: {
        http_req_duration: ['p(95)<500', 'p(99)<1500'], // 95% < 500ms, 99% < 1.5s
        http_req_failed: ['rate<0.01'],                    // Error rate < 1%
        errors: ['rate<0.05'],                              // Custom error rate < 5%
        http_reqs: ['rate>100'],                            // At least 100 RPS
        'login_duration': ['p(95)<800'],
        'sales_query_duration': ['p(95)<600'],
    },
};

const BASE_URL = __ENV.BASE_URL || 'https://staging.zorvian.com';
const TEST_USER = __ENV.TEST_USER || 'loadtest@zorvian.com';
const TEST_PASSWORD = __ENV.TEST_PASSWORD || 'LoadTest2026!';

export default function () {
    // Scenario 1: Health check
    group('Health Check', () => {
        const res = http.get(`${BASE_URL}/health`);
        check(res, {
            'status is 200': (r) => r.status === 200,
            'response time < 200ms': (r) => r.timings.duration < 200,
        });
    });

    // Scenario 2: Login flow
    let token = null;
    group('Login', () => {
        const start = Date.now();
        const res = http.post(
            `${BASE_URL}/zorvian/v1/auth/login`,
            JSON.stringify({ email: TEST_USER, password: TEST_PASSWORD }),
            { headers: { 'Content-Type': 'application/json' } }
        );
        loginDuration.add(Date.now() - start);

        const success = check(res, {
            'status is 200': (r) => r.status === 200,
            'has access token': (r) => r.json('data.accessToken') !== undefined,
        });

        if (success) {
            token = res.json('data.accessToken');
        } else {
            errorRate.add(1);
        }
    });

    if (!token) {
        sleep(1);
        return;
    }

    const authHeaders = {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
    };

    // Scenario 3: List clients (read-heavy)
    group('List Clients', () => {
        const res = http.get(
            `${BASE_URL}/zorvian/v1/clients?page=1&pageSize=50`,
            { headers: authHeaders }
        );
        check(res, {
            'status is 200': (r) => r.status === 200,
            'has data array': (r) => Array.isArray(r.json('data')),
        });
    });

    // Scenario 4: List sales (read-heavy, business critical)
    group('List Sales', () => {
        const start = Date.now();
        const res = http.get(
            `${BASE_URL}/zorvian/v1/sales?page=1&pageSize=20&status=paid`,
            { headers: authHeaders }
        );
        salesQueryDuration.add(Date.now() - start);
        check(res, {
            'status is 200': (r) => r.status === 200,
            'response time < 600ms': (r) => r.timings.duration < 600,
        });
    });

    // Scenario 5: List products
    group('List Products', () => {
        const res = http.get(
            `${BASE_URL}/zorvian/v1/products?page=1&pageSize=30`,
            { headers: authHeaders }
        );
        check(res, {
            'status is 200': (r) => r.status === 200,
        });
    });

    // Scenario 6: Dashboard KPIs
    group('Dashboard KPIs', () => {
        const res = http.get(
            `${BASE_URL}/zorvian/v1/dashboard/kpis`,
            { headers: authHeaders }
        );
        check(res, {
            'status is 200': (r) => r.status === 200,
            'has headcount': (r) => r.json('data.headcount') !== undefined,
        });
    });

    // Scenario 7: Create sale (write operation)
    group('Create Sale', () => {
        const payload = JSON.stringify({
            clientId: 'cli_test_001',
            items: [
                { productId: 'prd_001', quantity: 1, unitPrice: 100.0, discount: 0 }
            ],
            payments: [{ method: 'cash', amount: 100.0 }]
        });
        const res = http.post(
            `${BASE_URL}/zorvian/v1/sales/cash`,
            payload,
            { headers: authHeaders }
        );
        const success = check(res, {
            'status is 201': (r) => r.status === 201,
            'has sale id': (r) => r.json('data.id') !== undefined,
        });
        if (success) writeOperations.add(1);
        else errorRate.add(1);
    });

    // Scenario 8: Reports
    group('Reports', () => {
        const res = http.get(
            `${BASE_URL}/zorvian/v1/reports/sales-history?from=2026-01-01&to=2026-12-31`,
            { headers: authHeaders }
        );
        check(res, {
            'status is 200': (r) => r.status === 200,
            'response time < 2000ms': (r) => r.timings.duration < 2000,
        });
    });

    // Sleep between iterations
    sleep(Math.random() * 2 + 1); // 1-3 seconds
}

// Teardown function (runs once at the end)
export function teardown(data) {
    console.log('Load test completed. Review the metrics above.');
    console.log('\nRecommended SLOs:');
    console.log('- p95 latency < 500ms');
    console.log('- p99 latency < 1500ms');
    console.log('- Error rate < 1%');
    console.log('- RPS > 100 sustained');
}

// Handle errors gracefully
export function handleSummary(data) {
    return {
        'stdout': textSummary(data, { indent: ' ', enableColors: true }),
        'load-test-results.json': JSON.stringify(data),
    };
}

// Helper for text summary (k6 standard)
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.2/index.js';
