# Plan de Recuperación ante Desastres (DRP)

## Resumen Ejecutivo

Este documento describe el plan de recuperación ante desastres (DRP) de Zorvian ERP. El objetivo es restaurar el servicio en caso de una interrupción mayor en el menor tiempo posible (RTO) y minimizar la pérdida de datos (RPO).

## Objetivos

| Métrica | Objetivo | Crítico |
|---------|----------|---------|
| RTO (Recovery Time Objective) | 1 hora | 4 horas |
| RPO (Recovery Point Objective) | 1 hora | 24 horas |
| MTTR (Mean Time To Recover) | 30 min | 2 horas |
| Disponibilidad mensual | 99.9% | 99% |

## Inventario de Sistemas Críticos

| Sistema | Criticidad | Ubicación | Backup | Restauración |
|---------|-----------|-----------|---------|--------------|
| PostgreSQL (Neon) | Crítica | AWS US-East | Automático (7d free / 30d scale) | Point-in-time recovery |
| Firebase Auth | Crítica | Google Cloud | N/A (managed) | Restaurar desde Firebase console |
| Firebase Storage | Alta | Google Cloud | Automático (versioning) | Restaurar bucket |
| Backend (Render) | Crítica | Render US-East | Docker image | Redeploy desde GitHub |
| Frontend (Firebase) | Media | Firebase CDN | Automático | Redeploy desde GitHub Actions |
| Hangfire Jobs | Baja | PostgreSQL | BD backup | Se restauran con BD |
| Configuración Secrets | Crítica | GitHub Secrets | Git history | Reconfigurar manualmente |

## Procedimiento de Respuesta a Incidentes

### Fase 1: Detección y Evaluación (0-15 min)

1. **Detección**: Monitoreo automático vía Health Checks
   - Endpoint: `https://api.zorvian.com/health`
   - Alertas: Sentry + Email/SMS a equipo de guardia
2. **Evaluación inicial**:
   - ¿Es un problema de aplicación o infraestructura?
   - ¿Cuántos usuarios están afectados?
   - ¿Es un problema parcial o total?
3. **Activación del equipo DR**:
   - Tech Lead: Juan Pérez (+505-XXXX-XXXX)
   - DBA: Maria Lopez (+505-XXXX-XXXX)
   - DevOps: Carlos Mendoza (+505-XXXX-XXXX)
   - Product Manager: Ana Martinez (+505-XXXX-XXXX)

### Fase 2: Comunicación (5-30 min)

1. **Stakeholders internos**:
   - Notificar a CEO y VP Engineering
   - Status update cada 30 minutos
2. **Clientes**:
   - Status page: status.zorvian.com
   - Email a admins de tenants afectados
   - Publicación en redes sociales
3. **Plantilla de comunicación**:

```
INCIDENTE: [Severidad] - [Descripción corta]
INICIO: [Hora]
IMPACTO: [Sistemas afectados, % usuarios]
ESTADO: [Investigando / Mitigando / Resolviendo]
PRÓXIMO UPDATE: [Hora estimada]
```

### Fase 3: Mitigación (15-60 min)

#### Escenario A: Caída de Neon PostgreSQL

1. **Verificar estado de Neon**:
   ```bash
   curl -I https://console.neon.tech/api/v1/projects/[PROJECT_ID]
   ```
2. **Si Neon está caído**: Esperar recuperación o restaurar desde backup
3. **Restaurar desde backup**:
   ```bash
   # Neon CLI
   neonctl branches restore --project [PROJECT_ID] --branch main --timestamp [ISO_TIMESTAMP]
   ```
4. **Actualizar connection string** en Render
5. **Verificar conectividad**:
   ```bash
   curl https://api.zorvian.com/health
   ```

#### Escenario B: Caída de Firebase Auth

1. **Verificar estado**: https://status.firebase.google.com
2. **Implementar fallback**: Permitir login con JWT directo (sin Firebase verification)
3. **Si Firebase está caído más de 1 hora**: Activar plan de contingencia

#### Escenario C: Caída de Render (Backend)

1. **Verificar estado**: https://status.render.com
2. **Redesplegar a región alternativa**:
   ```bash
   render deploy --service [SERVICE_ID] --region oregon
   ```
3. **Actualizar DNS** para apuntar a nueva región
4. **Si Render está caído más de 4 horas**: Migrar a AWS ECS como contingencia

#### Escenario D: Código corrupto o eliminado

1. **Restaurar desde GitHub**:
   ```bash
   git log --oneline -20
   git checkout [COMMIT_HASH] -- .
   git push origin main --force-with-lease
   ```
2. **GitHub Actions** se encargará del deploy automático
3. **Verificar**: `curl https://api.zorvian.com/health`

### Fase 4: Recuperación (1-4 horas)

1. **Verificar integridad de datos**:
   - Contar empleados: `SELECT COUNT(*) FROM employees;`
   - Contar ventas: `SELECT COUNT(*) FROM sales;`
   - Contar usuarios activos
2. **Validar conexiones externas**:
   - Firebase: Verificar tokens
   - Redis: Verificar conexión
   - Storage: Verificar buckets
3. **Ejecutar smoke tests**:
   - Login
   - Crear venta
   - Generar reporte
   - Subir documento
4. **Reactivar jobs de Hangfire**

### Fase 5: Post-Recuperación

1. **Monitoreo intensificado** (24-48 horas)
2. **Postmortem**:
   - ¿Qué pasó?
   - ¿Por qué pasó?
   - ¿Cómo lo prevenimos?
3. **Actualizar runbook** con lecciones aprendidas
4. **Comunicación final** a stakeholders

## Backups

### Política de Backups

| Recurso | Frecuencia | Retención | Almacenamiento |
|---------|-----------|-----------|----------------|
| PostgreSQL completo | Diario 02:00 UTC | 30 días | Neon nativo + S3 |
| PostgreSQL incremental | Cada 6 horas | 7 días | Neon nativo |
| Point-in-time recovery | Continuo | 7 días | Neon nativo |
| Firebase Storage | Continuo | Indefinido | Google Cloud |
| Logs de auditoría | Continuo | 5 años | Neon (cold storage) |

### Procedimiento de Backup

```bash
#!/bin/bash
# daily-backup.sh - Ejecutar a las 02:00 UTC vía Hangfire

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="zorvian_backup_${TIMESTAMP}.sql.gz"

# 1. Ejecutar pg_dump
pg_dump $DATABASE_URL | gzip > /tmp/$BACKUP_FILE

# 2. Subir a S3
aws s3 cp /tmp/$BACKUP_FILE s3://zorvian-backups/$BACKUP_FILE

# 3. Limpiar local
rm /tmp/$BACKUP_FILE

# 4. Verificar tamaño
SIZE=$(aws s3api head-object --bucket zorvian-backups --key $BACKUP_FILE --query 'ContentLength')
if [ $SIZE -lt 1000000 ]; then
  echo "ALERTA: Backup muy pequeño ($SIZE bytes)"
  exit 1
fi

echo "Backup completado: $BACKUP_FILE ($SIZE bytes)"
```

### Procedimiento de Restauración

```bash
#!/bin/bash
# restore-backup.sh

BACKUP_FILE=$1
if [ -z "$BACKUP_FILE" ]; then
  echo "Uso: restore-backup.sh <backup_file>"
  exit 1
fi

# 1. Descargar de S3
aws s3 cp s3://zorvian-backups/$BACKUP_FILE /tmp/$BACKUP_FILE

# 2. Crear nueva base de datos temporal
psql -c "CREATE DATABASE zorvian_restore;"

# 3. Restaurar
gunzip -c /tmp/$BACKUP_FILE | psql zorvian_restore

# 4. Verificar
psql zorvian_restore -c "SELECT COUNT(*) FROM employees;"

# 5. Si OK, swap con producción
# (Requiere ventana de mantenimiento)
```

## Testing del DRP

### Frecuencia

- **Trimestral**: Tabletop exercise (reunión de 2 horas)
- **Semestral**: Game day (simulacro de 4 horas)
- **Anual**: Full DRP test (8 horas, región alternativa)

### Escenarios de Test

1. **T1**: Restaurar BD desde backup (4 horas)
2. **T2**: Migrar a región alternativa (2 horas)
3. **T3**: Recuperar desde pérdida de código (1 hora)
4. **T4**: Manejar breach de seguridad (variable)

## Comunicación con Clientes

### Status Page

**URL**: https://status.zorvian.com

Componentes:
- Estado actual de cada servicio
- Historial de incidentes
- Suscripción a notificaciones
- RSS feed

### SLA por Plan

| Plan | Uptime SLA | Créditos por Caída |
|------|-----------|---------------------|
| Starter | 99% | Sin créditos |
| Pro | 99.5% | 5% por hora |
| Enterprise | 99.9% | 10% por hora |

## Contactos de Emergencia

| Rol | Persona | Email | Teléfono |
|-----|---------|-------|----------|
| Tech Lead | Juan Pérez | juan@zorvian.com | +505-XXXX-XXXX |
| DBA | Maria Lopez | maria@zorvian.com | +505-XXXX-XXXX |
| DevOps | Carlos Mendoza | carlos@zorvian.com | +505-XXXX-XXXX |
| CEO | Ana Martinez | ana@zorvian.com | +505-XXXX-XXXX |
| Render Support | - | support@render.com | - |
| Neon Support | - | support@neon.tech | - |
| Firebase Support | - | firebase-support@google.com | - |

## Revisión

- **Frecuencia**: Trimestral
- **Owner**: VP Engineering
- **Próxima revisión**: [Fecha]
- **Historial**:
  - 2026-06-09: Versión inicial creada
</content>
<parameter name="task_progress">