using Microsoft.EntityFrameworkCore.Migrations;

# nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedProfessionalTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var hrContractContent = @"
<div style='font-family: Arial, sans-serif; line-height: 1.6;'>
    <h1 style='text-align: center; color: #2c3e50;'>CONTRATO INDIVIDUAL DE TRABAJO</h1>
    <p><strong>EMPLEADOR:</strong> {{ Company.Name }}</p>
    <p><strong>TRABAJADOR:</strong> {{ Employee.FullName }}</p>
    <p><strong>IDENTIFICACI&Oacute;N:</strong> {{ Employee.Identification }}</p>
    <p><strong>CARGO:</strong> {{ Employee.Position }}</p>

    <p style='margin-top: 20px;'>En la ciudad de Managua, a los {{ Company.Date }}, celebramos el presente contrato bajo las siguientes cl&aacute;usulas:</p>
    
    <h3>PRIMERA: OBJETO</h3>
    <p>El trabajador se obliga a prestar sus servicios personales como <strong>{{ Employee.Position }}</strong>, desempeñando sus funciones con diligencia y lealtad.</p>

    <h3>SEGUNDA: REMUNERACI&Oacute;N</h3>
    <p>El empleador pagar&aacute; al trabajador un salario de <strong>{{ Employee.Salary }}</strong>, pagaderos de forma mensual.</p>

    <h3>TERCERA: FECHA DE INICIO</h3>
    <p>La relaci&oacute;n laboral inicia el d&iacute;a {{ Employee.HireDate }}.</p>

    <div style='margin-top: 50px;'>
        <table style='width: 100%;'>
            <tr>
                <td style='text-align: center;'>__________________________<br>Empresa</td>
                <td style='text-align: center;'>__________________________<br>Trabajador</td>
            </tr>
        </table>
    </div>
</div>";

            var salesInvoiceContent = @"
<div style='font-family: Helvetica, sans-serif;'>
    <div style='display: flex; justify-content: space-between;'>
        <h2>{{ Company.Name }}</h2>
        <h3>FACTURA: {{ Sale.Number }}</h3>
    </div>
    <hr>
    <p><strong>Cliente:</strong> {{ Sale.ClientName }}</p>
    <p><strong>Fecha:</strong> {{ Sale.Date }}</p>
    
    <div style='margin-top: 30px; border: 1px solid #ddd; padding: 20px;'>
        <h2 style='text-align: right; color: #27ae60;'>TOTAL: {{ Sale.Total }}</h2>
    </div>
    
    <p style='font-size: 10px; margin-top: 50px;'>Documento generado automáticamente por Zorvian ERP Documentary Engine.</p>
</div>";

            // Seed HR Contract Template
            migrationBuilder.InsertData(
                table: "DocumentTemplates",
                columns: new[] { "Id", "Name", "Category", "Content", "CountryCode", "Module", "IsActive", "IsDeleted", "Version", "TenantId", "CreatedAt", "CreatedBy" },
                values: new object[] { 
                    System.Guid.Parse("11111111-1111-1111-1111-111111111111"), 
                    "Contrato Laboral Estándar", 
                    "HR", 
                    hrContractContent, 
                    "ALL", 
                    "Employee", 
                    true, 
                    false,
                    "1.0", 
                    "system", 
                    System.DateTime.UtcNow, 
                    "System" 
                });

            // Seed Sales Invoice Template
            migrationBuilder.InsertData(
                table: "DocumentTemplates",
                columns: new[] { "Id", "Name", "Category", "Content", "CountryCode", "Module", "IsActive", "IsDeleted", "Version", "TenantId", "CreatedAt", "CreatedBy" },
                values: new object[] { 
                    System.Guid.Parse("22222222-2222-2222-2222-222222222222"), 
                    "Factura de Venta Corporativa", 
                    "Sales", 
                    salesInvoiceContent, 
                    "ALL", 
                    "Sale", 
                    true, 
                    false,
                    "1.0", 
                    "system", 
                    System.DateTime.UtcNow, 
                    "System" 
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DocumentTemplates",
                keyColumn: "Id",
                keyValues: new object[] { 
                    System.Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    System.Guid.Parse("22222222-2222-2222-2222-222222222222")
                });
        }
    }
}
