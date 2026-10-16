using JieipManager;

var result = ExcelImporter.Import(args[0], new CompanySettings());
Console.WriteLine($"partners={result.PartnerCount}");
Console.WriteLine($"products={result.ProductCount}");
Console.WriteLine($"trades={result.TradeCount}");
Console.WriteLine($"settlements={result.SettlementCount}");
Console.WriteLine($"skipped={result.SkippedSettlementCount}");
Console.WriteLine("buy=" + result.Data.Trades.Where(x=>x.Type=="매입").Sum(x=>x.Total));
Console.WriteLine("sales=" + result.Data.Trades.Where(x=>x.Type=="매출").Sum(x=>x.Total));
Console.WriteLine("paid=" + result.Data.Settlements.Where(x=>x.Type=="지급").Sum(x=>x.Amount));
Console.WriteLine("received=" + result.Data.Settlements.Where(x=>x.Type=="수금").Sum(x=>x.Amount));
