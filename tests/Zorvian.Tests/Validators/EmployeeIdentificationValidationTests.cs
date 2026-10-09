using FluentAssertions;
using FluentValidation.TestHelper;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.Validators;

namespace Zorvian.Tests.Validators;

public sealed class EmployeeIdentificationValidationTests
{
    private readonly CreateEmployeeValidator _createValidator = new();
    private readonly UpdateEmployeeValidator _updateValidator = new();

    private static CreateEmployeeRequest ValidRequest(string? type = "cedula_ni", string? number = "001-123456-0001A")
        => new(
            FirstName: "Ana",
            LastName: "Pérez",
            Email: "ana@ejemplo.com",
            Phone: "555-0100",
            EmployeeCode: null,
            CollaboratorType: "employee",
            DateOfBirth: null,
            Gender: null,
            IdentificationType: type,
            IdentificationNumber: number,
            DepartmentId: null,
            Position: "Contadora",
            HireDate: null,
            Salary: 25000m,
            SalaryType: "monthly",
            BankName: null,
            BankAccountNumber: null,
            BankAccountType: null,
            ContractId: null);

    [Theory]
    [InlineData("001-123456-0001A")] // formato completo con letra verificadora
    [InlineData("001-123456-0001a")] // letra minúscula aceptada
    public void Create_CedulaNicaragua_Valid(string number)
    {
        var result = _createValidator.TestValidate(ValidRequest("cedula_ni", number));
        result.ShouldNotHaveValidationErrorFor(x => x.IdentificationNumber);
    }

    [Theory]
    [InlineData("0011234560001A")] // sin guiones
    [InlineData("001-123456-001A")] // solo 3 dígitos finales
    [InlineData("001-123456-00001A")] // 5 dígitos finales
    [InlineData("001-123456-0001")] // falta letra verificadora
    [InlineData("abc-def-ghij-k")] // no numérico
    public void Create_CedulaNicaragua_Invalid(string number)
    {
        var result = _createValidator.TestValidate(ValidRequest("cedula_ni", number));
        result.ShouldHaveValidationErrorFor(x => x.IdentificationNumber);
    }

    [Theory]
    [InlineData("nit_ni", "0000-123456-000-1")]
    [InlineData("cedula_cr", "123456789")]
    [InlineData("cedula_pa", "81234567")]
    [InlineData("cedula_pa", "8123456")]
    [InlineData("dui_sv", "00123456-7")]
    [InlineData("nit_sv", "06140000000000")]
    [InlineData("dpi_gt", "1234567890123")]
    [InlineData("nit_gt", "1234-567890-123")]
    [InlineData("dui_hn", "0801199012345")]
    [InlineData("rtn_hn", "08019901234567")]
    [InlineData("pasaporte", "A1234567")]
    public void Create_AllCentralAmericanDocuments_Valid(string type, string number)
    {
        var result = _createValidator.TestValidate(ValidRequest(type, number));
        result.ShouldNotHaveValidationErrorFor(x => x.IdentificationNumber);
    }

    [Theory]
    [InlineData("cedula_cr", "12345678")] // 8 dígitos en vez de 9
    [InlineData("dpi_gt", "123456789")] // 9 dígitos en vez de 13
    [InlineData("rtn_hn", "0801990123456")] // 13 dígitos en vez de 14
    [InlineData("nit_ni", "0000-123456-000")] // sin dígito verificador
    public void Create_WrongLength_IsInvalid(string type, string number)
    {
        var result = _createValidator.TestValidate(ValidRequest(type, number));
        result.ShouldHaveValidationErrorFor(x => x.IdentificationNumber);
    }

    [Fact]
    public void Create_UnknownType_SkipsFormatValidation()
    {
        // Integraciones externas pueden usar otros códigos (p. ej. "CC").
        var result = _createValidator.TestValidate(ValidRequest("CC", "cualquier-codigo"));
        result.ShouldNotHaveValidationErrorFor(x => x.IdentificationNumber);
    }

    [Fact]
    public void Create_EmptyNumber_IsAllowedOnBackend()
    {
        // El frontend lo exige; el backend mantiene compatibilidad con
        // clientes/integraciones que aún no envían el documento.
        var result = _createValidator.TestValidate(ValidRequest("cedula_ni", null));
        result.ShouldNotHaveValidationErrorFor(x => x.IdentificationNumber);
    }

    [Fact]
    public void Create_NumberTooLong_IsInvalid()
    {
        var result = _createValidator.TestValidate(ValidRequest("cedula_ni", new string('9', 50)));
        result.ShouldHaveValidationErrorFor(x => x.IdentificationNumber);
    }

    private static UpdateEmployeeRequest ValidUpdateRequest(string? type, string? number)
        => new(
            FirstName: null,
            LastName: null,
            Email: null,
            Phone: null,
            EmployeeCode: null,
            CollaboratorType: null,
            DateOfBirth: null,
            Gender: null,
            IdentificationType: type,
            IdentificationNumber: number,
            DepartmentId: null,
            Position: null,
            HireDate: null,
            Salary: null,
            SalaryType: null,
            Status: null,
            BankName: null,
            BankAccountNumber: null,
            BankAccountType: null,
            ContractId: null);

    [Fact]
    public void Update_CedulaNicaragua_Valid()
    {
        var request = ValidUpdateRequest("cedula_ni", "001-123456-0001A");
        var result = _updateValidator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.IdentificationNumber);
    }

    [Fact]
    public void Update_CedulaNicaragua_Invalid()
    {
        var request = ValidUpdateRequest("cedula_ni", "invalida");
        var result = _updateValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.IdentificationNumber);
    }

    [Fact]
    public void Update_NumberWithoutType_Invalid()
    {
        // Con número provisto pero sin tipo, no hay patrón que validar →
        // se acepta (solo longitud). Documentado como comportamiento permisivo.
        var request = ValidUpdateRequest(null, "12345");
        var result = _updateValidator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.IdentificationNumber);
    }
}
