-- ============================================================
-- Secuencias PostgreSQL para generación thread-safe de números
-- Reemplaza el patrón inseguro CountAsync + 1
-- ============================================================

-- Secuencia para números de asiento contable (AS-YYYYMMDD-NNNN)
CREATE SEQUENCE IF NOT EXISTS seq_entry_number
    INCREMENT BY 1
    START WITH 1
    NO CYCLE
    CACHE 1;

-- Secuencia para números de factura/invoice (FAC-YYYYMMDD-NNNN)
CREATE SEQUENCE IF NOT EXISTS seq_invoice_number
    INCREMENT BY 1
    START WITH 1
    NO CYCLE
    CACHE 1;

-- Secuencia para números de crédito (CRE-YYYYMMDD-NNNN)
CREATE SEQUENCE IF NOT EXISTS seq_credit_number
    INCREMENT BY 1
    START WITH 1
    NO CYCLE
    CACHE 1;

-- Secuencia para números de nota de crédito (NC-YYYYMMDD-NNNN)
CREATE SEQUENCE IF NOT EXISTS seq_credit_note_number
    INCREMENT BY 1
    START WITH 1
    NO CYCLE
    CACHE 1;

-- Secuencia para números de compra (COMP-YYYYMMDD-NNNN)
CREATE SEQUENCE IF NOT EXISTS seq_purchase_number
    INCREMENT BY 1
    START WITH 1
    NO CYCLE
    CACHE 1;

-- Secuencia para números de orden de compra (OC-YYYYMM-NNNN)
CREATE SEQUENCE IF NOT EXISTS seq_order_number
    INCREMENT BY 1
    START WITH 1
    NO CYCLE
    CACHE 1;
