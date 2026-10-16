namespace JieipManager;

public sealed class AppData
{
    public CompanySettings Settings { get; set; } = new();
    public List<Owner> Owners { get; set; } = [];
    public List<Vehicle> Vehicles { get; set; } = [];
    public List<Charge> Charges { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public List<WorkItem> WorkItems { get; set; } = [];
}

public sealed class CompanySettings
{
    public string CompanyName { get; set; } = "지입회사";
    public string BusinessNumber { get; set; } = "";
    public string Representative { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public int ManagementFee { get; set; }
    public int UtilityFee { get; set; }
    public int InsuranceFee { get; set; }
    public int AssociationFee { get; set; }
    public int EtcFee { get; set; }
    public int DueDay { get; set; } = 25;
    public string BackupFolder { get; set; } = "";
}

public sealed class Owner
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string BusinessNumber { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public DateTime JoinDate { get; set; } = DateTime.Today;
    public bool Active { get; set; } = true;
    public string Memo { get; set; } = "";
}

public sealed class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public string PlateNumber { get; set; } = "";
    public string VehicleType { get; set; } = "";
    public string Model { get; set; } = "";
    public int Year { get; set; } = DateTime.Today.Year;
    public DateTime? InspectionDue { get; set; }
    public DateTime? InsuranceDue { get; set; }
    public bool Active { get; set; } = true;
    public string Memo { get; set; } = "";
}

public sealed class Charge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BillingMonth { get; set; } = DateTime.Today.ToString("yyyy-MM");
    public Guid OwnerId { get; set; }
    public Guid VehicleId { get; set; }
    public int ManagementFee { get; set; }
    public int UtilityFee { get; set; }
    public int InsuranceFee { get; set; }
    public int AssociationFee { get; set; }
    public int EtcFee { get; set; }
    public DateTime DueDate { get; set; } = DateTime.Today;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string Memo { get; set; } = "";
    public int Total => ManagementFee + UtilityFee + InsuranceFee + AssociationFee + EtcFee;
}

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChargeId { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.Today;
    public int Amount { get; set; }
    public string Method { get; set; } = "계좌이체";
    public string Memo { get; set; } = "";
}

public sealed class WorkItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? VehicleId { get; set; }
    public string Type { get; set; } = "검사";
    public string Title { get; set; } = "";
    public DateTime DueDate { get; set; } = DateTime.Today;
    public string Status { get; set; } = "대기";
    public string Memo { get; set; } = "";
}
