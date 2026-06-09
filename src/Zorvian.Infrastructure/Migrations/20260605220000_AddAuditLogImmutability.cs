using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <summary>
    /// AUD-004: Garantiza la inmutabilidad de la tabla <c>AuditLogs</c> en PostgreSQL.
    ///
    /// Esta migración crea, a nivel de base de datos, los mecanismos de control de acceso
    /// que ningún código de la aplicación puede bypassear:
    ///
    ///   1. Función <c>audit_logs_block_mutation()</c>: trigger function que aborta la
    ///      operación con <c>RAISE EXCEPTION</c> si se intenta UPDATE o DELETE
    ///      sobre la tabla <c>"AuditLogs"</c>.
    ///
    ///   2. Trigger <c>trg_audit_logs_immutable</c>: BEFORE UPDATE OR DELETE
    ///      que invoca la función anterior, garantizando que ningún registro
    ///      de auditoría pueda ser modificado o eliminado desde la base de datos.
    ///
    /// Complementa al <c>AuditImmutabilityInterceptor</c> en EF Core (capa de
    /// aplicación) con un segundo nivel de defensa: incluso si un atacante
    /// evade la capa de aplicación y ejecuta SQL directo contra la base
    /// de datos, el trigger rechazará la operación.
    /// </summary>
    public partial class AddAuditLogImmutability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Crear la función que será ejecutada por el trigger.
            //    Rechaza UPDATE y DELETE sobre "AuditLogs" con un mensaje claro.
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION audit_logs_block_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION
                        'AUD-004: AuditLogs es inmutable. Operación % sobre tabla AuditLogs está bloqueada por la política de inmutabilidad. Para cumplir con requisitos de auditoría, ningún registro de auditoría puede ser modificado ni eliminado. Si necesita archivar registros antiguos, créelos en una tabla de archivo separada.',
                        TG_OP
                        USING ERRCODE = 'P0001';
                    -- No se retorna ninguna fila: se cancela la operación.
                    RETURN NULL;
                END;
                $$;
            ");

            // 2. Crear el trigger BEFORE UPDATE OR DELETE sobre la tabla AuditLogs.
            //    El trigger se dispara ANTES de que la operación tenga efecto,
            //    por lo que abortar la operación dentro de la función evita
            //    cualquier cambio en la tabla.
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trg_audit_logs_immutable ON ""AuditLogs"";

                CREATE TRIGGER trg_audit_logs_immutable
                BEFORE UPDATE OR DELETE OR TRUNCATE
                ON ""AuditLogs""
                FOR EACH STATEMENT
                EXECUTE FUNCTION audit_logs_block_mutation();
            ");

            // 3. (Opcional, pero recomendado) Revocar permisos UPDATE/DELETE/TRUNCATE
            //    del rol PUBLIC. La aplicación debería usar un rol específico
            //    (por ejemplo, zorvian_app) que solo tenga INSERT y SELECT.
            //
            //    Este REVOKE se aplica solo si el rol zorvian_app existe, para
            //    no romper instalaciones donde la app se conecta como superusuario
            //    (por ejemplo, en desarrollo). El trigger en (2) sigue siendo
            //    efectivo incluso si el rol tiene todos los permisos.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'zorvian_app') THEN
                        REVOKE UPDATE, DELETE, TRUNCATE ON ""AuditLogs"" FROM zorvian_app;
                        GRANT SELECT, INSERT ON ""AuditLogs"" TO zorvian_app;
                        RAISE NOTICE 'AUD-004: Permisos UPDATE/DELETE/TRUNCATE revocados del rol zorvian_app sobre AuditLogs';
                    ELSE
                        RAISE NOTICE 'AUD-004: Rol zorvian_app no existe; se omite REVOKE (trigger sigue activo).';
                    END IF;
                END
                $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Al revertir, se elimina el trigger, la función y se restablecen
            // los permisos. La tabla AuditLogs vuelve a ser modificable.
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trg_audit_logs_immutable ON ""AuditLogs"";
            ");

            migrationBuilder.Sql(@"
                DROP FUNCTION IF EXISTS audit_logs_block_mutation();
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'zorvian_app') THEN
                        GRANT UPDATE, DELETE, TRUNCATE ON ""AuditLogs"" TO zorvian_app;
                    END IF;
                END
                $$;
            ");
        }
    }
}
