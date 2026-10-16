namespace JieipManager;

public sealed class AppData
{
    public int SchemaVersion { get; set; } = 2;
    public CompanySettings Settings { get; set; } = new();
    public List<Partner> Partners { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    public List<Trade> Trades { get; set; } = [];
    public List<Settlement> Settlements { get; set; } = [];
}

public sealed class CompanySettings
{
    public string CompanyName { get; set; } = "지입회사";
    public string BusinessNumber { get; set; } = "";
    public string Representative { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string BackupFolder { get; set; } = "";
}

public sealed class Partner
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = "매출처";
    public string Name { get; set; } = "";
    public string Representative { get; set; } = "";
    public string BusinessNumber { get; set; } = "";
    public string MainPhone { get; set; } = "";
    public string Fax { get; set; } = "";
    public string Address { get; set; } = "";
    public string BusinessType { get; set; } = "";
    public string BusinessItem { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string Department { get; set; } = "";
    public string Position { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Memo { get; set; } = "";
}

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Specification { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal InitialStock { get; set; }
    public string Memo { get; set; } = "";
}

public sealed class Trade
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime TradeDate { get; set; } = DateTime.Today;
    public string Type { get; set; } = "매출";
    public string PartnerName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string TaxType { get; set; } = "별도";
    public decimal SupplyAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal AdditionalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public string Memo { get; set; } = "";
}

public sealed class Settlement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; } = DateTime.Today;
    public string Type { get; set; } = "수금";
    public string PartnerName { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string Memo { get; set; } = "";
}

public sealed record ImportResult(AppData Data, int PartnerCount, int ProductCount, int TradeCount, int SettlementCount, int SkippedSettlementCount);
