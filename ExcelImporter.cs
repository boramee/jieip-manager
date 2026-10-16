using ClosedXML.Excel;

namespace JieipManager;

public static class ExcelImporter
{
    public static ImportResult Import(string path, CompanySettings settings)
    {
        using var wb = new XLWorkbook(path);
        foreach (var required in new[] { "매입처 정보 입력", "매출처 정보 입력", "상품정보 입력", "거래내역 입력", "지급수금 내역 입력" })
            if (!wb.TryGetWorksheet(required, out _)) throw new InvalidDataException($"필수 시트가 없습니다: {required}");
        var data = new AppData { Settings = settings };
        ReadPartners(wb.Worksheet("매입처 정보 입력"), "매입처", data.Partners);
        ReadPartners(wb.Worksheet("매출처 정보 입력"), "매출처", data.Partners);
        ReadProducts(wb.Worksheet("상품정보 입력"), data.Products);
        ReadTrades(wb.Worksheet("거래내역 입력"), data.Trades);
        int skipped = ReadSettlements(wb.Worksheet("지급수금 내역 입력"), data.Settlements);
        return new(data, data.Partners.Count, data.Products.Count, data.Trades.Count, data.Settlements.Count, skipped);
    }

    private static void ReadPartners(IXLWorksheet ws, string type, List<Partner> output)
    {
        int last=ws.LastRowUsed()?.RowNumber() ?? 5;
        for(int r=6;r<=last;r++){var name=S(ws,r,2);if(name=="")continue;output.Add(new Partner{Type=type,Name=name,Representative=S(ws,r,3),BusinessNumber=S(ws,r,4),MainPhone=S(ws,r,5),Fax=S(ws,r,6),Address=S(ws,r,7),BusinessType=S(ws,r,8),BusinessItem=S(ws,r,9),ContactName=S(ws,r,10),Department=S(ws,r,11),Position=S(ws,r,12),ContactPhone=S(ws,r,13),Email=S(ws,r,14),Memo=S(ws,r,18)});}
    }
    private static void ReadProducts(IXLWorksheet ws,List<Product> output)
    {
        int last=ws.LastRowUsed()?.RowNumber() ?? 5;
        for(int r=6;r<=last;r++){var name=S(ws,r,2);if(name=="")continue;output.Add(new Product{Name=name,Code=S(ws,r,3),Specification=S(ws,r,4),Unit=S(ws,r,5),UnitPrice=N(ws,r,6),InitialStock=N(ws,r,7),Memo=S(ws,r,11)});}
    }
    private static void ReadTrades(IXLWorksheet ws,List<Trade> output)
    {
        int last=ws.LastRowUsed()?.RowNumber() ?? 5;
        for(int r=6;r<=last;r++)
        {
            if(!D(ws,r,2,out var date))continue;var type=S(ws,r,3);var partner=S(ws,r,4);var product=S(ws,r,17);
            if(type is not ("매입" or "매출")||partner==""||product=="")continue;
            var qty=N(ws,r,21);var unit=N(ws,r,22);var taxType=S(ws,r,23);var supply=N(ws,r,24);var tax=N(ws,r,25);var add=N(ws,r,26);var discount=N(ws,r,27);var total=N(ws,r,28);
            if(supply==0&&qty!=0&&unit!=0)supply=taxType=="포함"?qty*unit-tax:qty*unit;
            if(tax==0&&qty!=0&&unit!=0)tax=taxType=="별도"?Math.Truncate(qty*unit*.1m/10m)*10m:taxType=="포함"?Math.Truncate((qty*unit-qty*unit/1.1m)/10m)*10m:0;
            if(total==0)total=supply+tax+add-discount;
            output.Add(new Trade{TradeDate=date,Type=type,PartnerName=partner,ProductName=product,Quantity=qty,UnitPrice=unit,TaxType=taxType,SupplyAmount=supply,TaxAmount=tax,AdditionalAmount=add,DiscountAmount=discount,Total=total,Memo=S(ws,r,29)});
        }
    }
    private static int ReadSettlements(IXLWorksheet ws,List<Settlement> output)
    {
        int skipped=0,last=ws.LastRowUsed()?.RowNumber()??5;
        for(int r=6;r<=last;r++)
        {
            var type=S(ws,r,3);if(type is not ("지급" or "수금"))continue;
            if(!D(ws,r,2,out var date)||S(ws,r,4)==""||!TryN(ws,r,18,out var amount)||amount==0){if(ws.Cell(r,2).IsEmpty()&&ws.Cell(r,4).IsEmpty()&&ws.Cell(r,18).IsEmpty())continue;skipped++;continue;}
            output.Add(new Settlement{Date=date,Type=type,PartnerName=S(ws,r,4),Description=S(ws,r,17),Amount=amount,Memo=S(ws,r,19)});
        }return skipped;
    }
    private static string S(IXLWorksheet w,int r,int c)=>w.Cell(r,c).GetFormattedString().Trim();
    private static decimal N(IXLWorksheet w,int r,int c)=>TryN(w,r,c,out var n)?n:0;
    private static bool TryN(IXLWorksheet w,int r,int c,out decimal n)=>decimal.TryParse(w.Cell(r,c).GetFormattedString().Replace(",",""),out n);
    private static bool D(IXLWorksheet w,int r,int c,out DateTime d){var cell=w.Cell(r,c);if(cell.TryGetValue(out d)){d=d.Date;return true;}return DateTime.TryParse(cell.GetFormattedString(),out d);}
}
