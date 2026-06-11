namespace Zorvian.Core.Enums;

public enum CollaboratorType
{
    Employee,
    ServiceProvider,
    Contractor,
    Consultant,
    Freelancer,
    CommissionAgent,
    Temporary,
    CommercialAgent,
    CollectionAgent,
    ExternalTechnician
}

public static class CollaboratorTypeExtensions
{
    public static string ToDbValue(this CollaboratorType type) => type switch
    {
        CollaboratorType.Employee => "employee",
        CollaboratorType.ServiceProvider => "service_provider",
        CollaboratorType.Contractor => "contractor",
        CollaboratorType.Consultant => "consultant",
        CollaboratorType.Freelancer => "freelancer",
        CollaboratorType.CommissionAgent => "commission_agent",
        CollaboratorType.Temporary => "temporary",
        CollaboratorType.CommercialAgent => "commercial_agent",
        CollaboratorType.CollectionAgent => "collection_agent",
        CollaboratorType.ExternalTechnician => "external_technician",
        _ => "employee"
    };

    public static CollaboratorType FromDbValue(string value) => value switch
    {
        "employee" => CollaboratorType.Employee,
        "service_provider" => CollaboratorType.ServiceProvider,
        "contractor" => CollaboratorType.Contractor,
        "consultant" => CollaboratorType.Consultant,
        "freelancer" => CollaboratorType.Freelancer,
        "commission_agent" => CollaboratorType.CommissionAgent,
        "temporary" => CollaboratorType.Temporary,
        "commercial_agent" => CollaboratorType.CommercialAgent,
        "collection_agent" => CollaboratorType.CollectionAgent,
        "external_technician" => CollaboratorType.ExternalTechnician,
        _ => CollaboratorType.Employee
    };
}
