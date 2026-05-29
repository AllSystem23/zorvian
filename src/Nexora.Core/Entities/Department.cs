namespace Nexora.Core.Entities;

public sealed class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public Department? ParentDepartment { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Employee> Employees { get; set; } = [];
    public ICollection<Department> ChildDepartments { get; set; } = [];
}
