using Microsoft.EntityFrameworkCore.Migrations;

namespace Zorvian.Infrastructure.Migrations;

/// <summary>
/// Abre una excepción ACOTADA a AUD-004 en la capa de base de datos para que la
/// retención pueda purgar <c>AuditLogs</c>.
///
/// <para>
/// Diagnóstico: la inmutabilidad está implementada DOS veces y había que abrir las dos.
/// El interceptor de EF (<c>AuditImmutabilityInterceptor</c>) rechaza el
/// <c>EntityState.Deleted</c> y ya quedó cubierto con <c>AuditRetentionScope</c>; pero el
/// trigger <c>trg_audit_logs_immutable</c> → <c>audit_logs_block_mutation()</c> rechaza
/// <c>UPDATE</c>, <c>DELETE</c> y <c>TRUNCATE</c> en el servidor con
/// <c>SQLSTATE P0001</c>, así que la purga moría con:
/// <c>AUD-004: AuditLogs es inmutable. Operación TRUNCATE sobre tabla AuditLogs...</c>
/// Ningún cambio de C# puede evitar eso: la sentencia nunca llega a ejecutarse.
/// </para>
///
/// <para>
/// La clase de migración original (<c>20260605220000_AddAuditLogImmutability</c>) figura
/// en <c>__EFMigrationsHistory</c> de la base de producción pero <b>no existe en el
/// repo</b>, así que en bases nuevas el trigger ni siquiera se crea. Esta migración es
/// idempotente y autosuficiente: crea o reemplaza la función, recrea el trigger y
/// re-aplica los permisos.
/// </para>
///
/// <para>
/// El escape es una GUC de transacción: <c>set_config('zorvian.audit_retention', 'on',
/// true)</c> desde <c>AuditRetentionScope.EnableDatabaseSettingSql</c>, ejecutada en la
/// MISMA transacción del <c>DELETE</c>/<c>TRUNCATE</c>. Al terminar la transacción la GUC
/// vuelve sola a su valor anterior, así que no queda pegada en la conexión del pool.
/// <c>UPDATE</c> sigue bloqueado SIEMPRE: el contenido de un registro de auditoría no se
/// puede alterar ni siquiera durante la purga.
/// </para>
///
/// <para>
/// Sin <c>.Designer.cs</c> a propósito (igual que <c>EnableRLS</c>): la migración es SQL
/// puro, no toca el modelo, y así
/// <c>dotnet ef migrations has-pending-model-changes</c> sigue limpio.
/// </para>
/// </summary>
[Migration("20261031120000_AddAuditLogRetentionBypass")]
public partial class AddAuditLogRetentionBypass : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Función con el escape por GUC. Se usa $fn$ (no $$) para no chocar con los
        //    delimitadores de los bloques DO de abajo (PostgreSQL no anida dollar-quotes
        //    con la misma etiqueta — ese bug ya rompió EnableRLS en producción).
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION audit_logs_block_mutation()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $fn$
            BEGIN
                -- Retención/mantenimiento: la purga habilita el GUC dentro de la misma
                -- transacción del borrado (set_config(..., is_local = true)). Solo se
                -- permite el BORRADO físico; UPDATE sigue bloqueado siempre.
                IF TG_OP IN ('DELETE', 'TRUNCATE')
                   AND coalesce(current_setting('zorvian.audit_retention', true), '') = 'on' THEN
                    RETURN NULL;
                END IF;

                RAISE EXCEPTION
                    'AUD-004: AuditLogs es inmutable. Operación % sobre tabla AuditLogs está bloqueada por la política de inmutabilidad. Para cumplir con requisitos de auditoría, ningún registro de auditoría puede ser modificado ni eliminado. Si necesita archivar registros antiguos, créelos en una tabla de archivo separada.',
                    TG_OP
                    USING ERRCODE = 'P0001';

                -- No se retorna ninguna fila: se cancela la operación.
                RETURN NULL;
            END;
            $fn$;
            """);

        // 2. Trigger de sentencia (cubre UPDATE/DELETE/TRUNCATE). Se recrea siempre para
        //    que apunte a la función nueva; en bases nuevas es la primera vez que existe.
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS trg_audit_logs_immutable ON "AuditLogs";

            CREATE TRIGGER trg_audit_logs_immutable
            BEFORE UPDATE OR DELETE OR TRUNCATE
            ON "AuditLogs"
            FOR EACH STATEMENT
            EXECUTE FUNCTION audit_logs_block_mutation();
            """);

        // 3. Permisos del rol de aplicación (idempotente, guardado si el rol no existe).
        //    Ojo: el REVOKE deja la tabla solo-lectura para `zorvian_app`, así que si algún
        //    día la app se conecta con ese rol, la retención debe ejecutarse con el rol
        //    owner (hoy la app conecta como owner de Neon y el trigger es el único guardia).
        migrationBuilder.Sql("""
            DO $do$
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'zorvian_app') THEN
                    REVOKE UPDATE, DELETE, TRUNCATE ON "AuditLogs" FROM zorvian_app;
                    GRANT SELECT, INSERT ON "AuditLogs" TO zorvian_app;
                    RAISE NOTICE 'AUD-004: Permisos UPDATE/DELETE/TRUNCATE revocados del rol zorvian_app sobre AuditLogs';
                ELSE
                    RAISE NOTICE 'AUD-004: Rol zorvian_app no existe; se omite REVOKE (trigger sigue activo).';
                END IF;
            END
            $do$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Vuelve a la versión ESTRICTA (sin escape): cualquier DELETE/TRUNCATE vuelve a
        // fallar con P0001, tal como estaba antes de la excepción de retención.
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION audit_logs_block_mutation()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $fn$
            BEGIN
                RAISE EXCEPTION
                    'AUD-004: AuditLogs es inmutable. Operación % sobre tabla AuditLogs está bloqueada por la política de inmutabilidad. Para cumplir con requisitos de auditoría, ningún registro de auditoría puede ser modificado ni eliminado. Si necesita archivar registros antiguos, créelos en una tabla de archivo separada.',
                    TG_OP
                    USING ERRCODE = 'P0001';

                RETURN NULL;
            END;
            $fn$;
            """);

        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS trg_audit_logs_immutable ON "AuditLogs";

            CREATE TRIGGER trg_audit_logs_immutable
            BEFORE UPDATE OR DELETE OR TRUNCATE
            ON "AuditLogs"
            FOR EACH STATEMENT
            EXECUTE FUNCTION audit_logs_block_mutation();
            """);
    }
}
