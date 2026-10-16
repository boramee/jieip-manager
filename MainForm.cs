using System.Diagnostics;

namespace JieipManager;

public sealed class MainForm : Form
{
    private readonly DataStore _store = new();
    private readonly TabControl _tabs = new(){Dock=DockStyle.Fill};
    private readonly DataGridView _partners=Grid(),_products=Grid(),_trades=Grid(),_settlements=Grid(),_ledger=Grid();
    private readonly Label _summary=new(){Dock=DockStyle.Top,Height=110,Font=new Font("맑은 고딕",15,FontStyle.Bold),Padding=new Padding(14,12,14,12),AutoEllipsis=false};
    private readonly Label _backup=new(){Dock=DockStyle.Bottom,Height=34,Padding=new Padding(10,7,10,7),AutoEllipsis=true};
    private readonly ComboBox _partnerType=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=90};
    private readonly ComboBox _ledgerPartner=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=240};

    public MainForm()
    {
        AutoScaleDimensions=new SizeF(96F,96F);AutoScaleMode=AutoScaleMode.Dpi;
        Text=$"지입회사 거래처관리 {UpdateService.CurrentVersion}";Width=1400;Height=860;MinimumSize=new Size(980,650);StartPosition=FormStartPosition.CenterScreen;Font=new Font("맑은 고딕",9.5f);
        Controls.Add(_tabs);BuildDashboard();BuildPartners();BuildProducts();BuildTrades();BuildSettlements();BuildLedger();BuildSettings();RefreshAll();
        FormClosing+=(_,_)=>_store.Save();
    }

    private static DataGridView Grid()=>new(){Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,RowHeadersVisible=false,BackgroundColor=Color.White};
    private static Button Btn(string text,EventHandler click){var b=new Button{Text=text,AutoSize=true,Height=34,Margin=new Padding(4)};b.Click+=click;return b;}
    private static FlowLayoutPanel Bar(params Control[] c){var p=new FlowLayoutPanel{Dock=DockStyle.Top,Height=46,Padding=new Padding(6),WrapContents=false};p.Controls.AddRange(c);return p;}
    private static Label Caption(string text)=>new(){Text=text,AutoSize=true,Padding=new Padding(8,9,0,0)};

    private void BuildDashboard()
    {
        var p=new TabPage("현황");p.Controls.Add(new Label{Dock=DockStyle.Fill,Text="제공된 거래처관리대장의 구조를 기준으로 합니다.\n\n• 매입처·매출처 및 상품 등록\n• 매입·매출과 부가세 계산\n• 지급·수금 및 미지급·미수 관리\n• 기존 XLSX 전체 가져오기",Font=new Font("맑은 고딕",12),Padding=new Padding(24),TextAlign=ContentAlignment.MiddleLeft});
        p.Controls.Add(_backup);p.Controls.Add(_summary);p.Controls.Add(Bar(Btn("거래처관리대장 XLSX 가져오기",(_,_)=>ImportExcel()),Btn("데이터 폴더 열기",(_,_)=>Process.Start(new ProcessStartInfo("explorer.exe",_store.BaseFolder){UseShellExecute=true}))));_tabs.TabPages.Add(p);
    }
    private void BuildPartners(){_partnerType.Items.AddRange(["전체","매입처","매출처"]);_partnerType.SelectedIndex=0;_partnerType.SelectedIndexChanged+=(_,_)=>RefreshPartners();var p=new TabPage("거래처");p.Controls.Add(_partners);p.Controls.Add(Bar(Caption("구분"),_partnerType,Btn("추가",(_,_)=>EditPartner(null)),Btn("수정",(_,_)=>EditPartner(Selected<Partner>(_partners,_store.Data.Partners))),Btn("삭제",(_,_)=>DeletePartner())));_tabs.TabPages.Add(p);}
    private void BuildProducts(){var p=new TabPage("상품");p.Controls.Add(_products);p.Controls.Add(Bar(Btn("추가",(_,_)=>EditProduct(null)),Btn("수정",(_,_)=>EditProduct(Selected<Product>(_products,_store.Data.Products))),Btn("삭제",(_,_)=>DeleteProduct())));_tabs.TabPages.Add(p);}
    private void BuildTrades(){var p=new TabPage("매입·매출");p.Controls.Add(_trades);p.Controls.Add(Bar(Btn("거래 추가",(_,_)=>EditTrade(null)),Btn("수정",(_,_)=>EditTrade(Selected<Trade>(_trades,_store.Data.Trades))),Btn("삭제",(_,_)=>DeleteTrade())));_tabs.TabPages.Add(p);}
    private void BuildSettlements(){var p=new TabPage("지급·수금");p.Controls.Add(_settlements);p.Controls.Add(Bar(Btn("지급·수금 추가",(_,_)=>EditSettlement(null)),Btn("수정",(_,_)=>EditSettlement(Selected<Settlement>(_settlements,_store.Data.Settlements))),Btn("삭제",(_,_)=>DeleteSettlement())));_tabs.TabPages.Add(p);}
    private void BuildLedger(){_ledgerPartner.SelectedIndexChanged+=(_,_)=>RefreshLedger();var p=new TabPage("거래처 원장");p.Controls.Add(_ledger);p.Controls.Add(Bar(Caption("거래처"),_ledgerPartner));_tabs.TabPages.Add(p);}
    private void BuildSettings()
    {
        var s=_store.Data.Settings;var f=new Dictionary<string,Control>{{"회사명",new TextBox{Text=s.CompanyName}},{"사업자번호",new TextBox{Text=s.BusinessNumber}},{"대표자",new TextBox{Text=s.Representative}},{"전화",new TextBox{Text=s.Phone}},{"주소",new TextBox{Text=s.Address}},{"외부 백업 폴더",new TextBox{Text=s.BackupFolder}}};
        var p=new TabPage("설정");var panel=FormPanel(f);panel.Dock=DockStyle.Top;panel.Height=300;p.Controls.Add(panel);p.Controls.Add(Bar(Btn("설정 저장",(_,_)=>{s.CompanyName=T(f,"회사명");s.BusinessNumber=T(f,"사업자번호");s.Representative=T(f,"대표자");s.Phone=T(f,"전화");s.Address=T(f,"주소");s.BackupFolder=T(f,"외부 백업 폴더");Save("설정을 저장했습니다.");}),Btn("업데이트 확인",async(_,_)=>await CheckUpdate())));_tabs.TabPages.Add(p);
    }

    private void ImportExcel()
    {
        using var d=new OpenFileDialog{Filter="Excel 통합문서|*.xlsx",Title="거래처관리대장 선택"};if(d.ShowDialog()!=DialogResult.OK)return;
        if(MessageBox.Show("현재 거래처·상품·거래·지급수금 자료를 선택한 엑셀 내용으로 교체합니다.\n기존 자료는 먼저 백업됩니다. 계속할까요?","엑셀 가져오기",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
        try{UseWaitCursor=true;var backup=_store.CreateImportBackup();var result=ExcelImporter.Import(d.FileName,_store.Data.Settings);_store.Replace(result.Data);RefreshAll();MessageBox.Show($"가져오기 완료\n\n거래처 {result.PartnerCount:N0}곳\n상품 {result.ProductCount:N0}개\n매입·매출 {result.TradeCount:N0}건\n지급·수금 {result.SettlementCount:N0}건\n불완전하여 제외 {result.SkippedSettlementCount:N0}건\n\n기존 데이터 백업: {backup}","완료");}catch(Exception ex){MessageBox.Show($"가져오기에 실패했습니다. 기존 데이터는 유지됩니다.\n\n{ex.Message}","가져오기 오류");}finally{UseWaitCursor=false;}
    }

    private void EditPartner(Partner? item)
    {
        var x=item??new Partner();var type=Choice(["매입처","매출처"],x.Type);var f=new Dictionary<string,Control>{{"구분",type},{"거래처명",new TextBox{Text=x.Name}},{"대표자",new TextBox{Text=x.Representative}},{"사업자번호",new TextBox{Text=x.BusinessNumber}},{"대표번호",new TextBox{Text=x.MainPhone}},{"팩스",new TextBox{Text=x.Fax}},{"주소",new TextBox{Text=x.Address}},{"업태",new TextBox{Text=x.BusinessType}},{"종목",new TextBox{Text=x.BusinessItem}},{"담당자",new TextBox{Text=x.ContactName}},{"전화번호",new TextBox{Text=x.ContactPhone}},{"이메일",new TextBox{Text=x.Email}},{"비고",new TextBox{Text=x.Memo}}};if(!Dialog("거래처",f))return;if(T(f,"거래처명")==""){MessageBox.Show("거래처명을 입력하세요.");return;}x.Type=type.Text;x.Name=T(f,"거래처명");x.Representative=T(f,"대표자");x.BusinessNumber=T(f,"사업자번호");x.MainPhone=T(f,"대표번호");x.Fax=T(f,"팩스");x.Address=T(f,"주소");x.BusinessType=T(f,"업태");x.BusinessItem=T(f,"종목");x.ContactName=T(f,"담당자");x.ContactPhone=T(f,"전화번호");x.Email=T(f,"이메일");x.Memo=T(f,"비고");if(item is null)_store.Data.Partners.Add(x);Save();
    }
    private void EditProduct(Product? item)
    {
        var x=item??new Product();var f=new Dictionary<string,Control>{{"상품명",new TextBox{Text=x.Name}},{"상품코드",new TextBox{Text=x.Code}},{"규격",new TextBox{Text=x.Specification}},{"단위",new TextBox{Text=x.Unit}},{"단가",Money(x.UnitPrice)},{"초기재고",Money(x.InitialStock)},{"비고",new TextBox{Text=x.Memo}}};if(!Dialog("상품",f))return;if(T(f,"상품명")==""){MessageBox.Show("상품명을 입력하세요.");return;}x.Name=T(f,"상품명");x.Code=T(f,"상품코드");x.Specification=T(f,"규격");x.Unit=T(f,"단위");x.UnitPrice=N(f,"단가");x.InitialStock=N(f,"초기재고");x.Memo=T(f,"비고");if(item is null)_store.Data.Products.Add(x);Save();
    }
    private void EditTrade(Trade? item)
    {
        if(_store.Data.Partners.Count==0||_store.Data.Products.Count==0){MessageBox.Show("거래처와 상품을 먼저 등록하세요.");return;}var x=item??new Trade();var type=Choice(["매입","매출"],x.Type);var partner=Choice(_store.Data.Partners.Select(z=>z.Name).Distinct().Order().ToArray(),x.PartnerName);var product=Choice(_store.Data.Products.Select(z=>z.Name).Distinct().Order().ToArray(),x.ProductName);var tax=Choice(["별도","포함","없음"],x.TaxType);var f=new Dictionary<string,Control>{{"거래일자",new DateTimePicker{Value=x.TradeDate}},{"구분",type},{"거래처명",partner},{"상품명",product},{"수량",Money(x.Quantity)},{"단가",Money(x.UnitPrice)},{"세액구분",tax},{"추가금액",Money(x.AdditionalAmount)},{"할인금액",Money(x.DiscountAmount)},{"비고",new TextBox{Text=x.Memo}}};if(!Dialog("매입·매출",f))return;x.TradeDate=((DateTimePicker)f["거래일자"]).Value.Date;x.Type=type.Text;x.PartnerName=partner.Text;x.ProductName=product.Text;x.Quantity=N(f,"수량");x.UnitPrice=N(f,"단가");x.TaxType=tax.Text;x.AdditionalAmount=N(f,"추가금액");x.DiscountAmount=N(f,"할인금액");var gross=x.Quantity*x.UnitPrice;x.TaxAmount=x.TaxType=="별도"?Math.Truncate(gross*.1m/10m)*10m:x.TaxType=="포함"?Math.Truncate((gross-gross/1.1m)/10m)*10m:0;x.SupplyAmount=x.TaxType=="포함"?gross-x.TaxAmount:gross;x.Total=x.SupplyAmount+x.TaxAmount+x.AdditionalAmount-x.DiscountAmount;x.Memo=T(f,"비고");if(item is null)_store.Data.Trades.Add(x);Save();
    }
    private void EditSettlement(Settlement? item)
    {
        if(_store.Data.Partners.Count==0){MessageBox.Show("거래처를 먼저 등록하세요.");return;}var x=item??new Settlement();var type=Choice(["지급","수금"],x.Type);var partner=Choice(_store.Data.Partners.Select(z=>z.Name).Distinct().Order().ToArray(),x.PartnerName);var f=new Dictionary<string,Control>{{"일자",new DateTimePicker{Value=x.Date}},{"구분",type},{"거래처명",partner},{"적요",new TextBox{Text=x.Description}},{"금액",Money(x.Amount)},{"비고",new TextBox{Text=x.Memo}}};if(!Dialog("지급·수금",f))return;x.Date=((DateTimePicker)f["일자"]).Value.Date;x.Type=type.Text;x.PartnerName=partner.Text;x.Description=T(f,"적요");x.Amount=N(f,"금액");x.Memo=T(f,"비고");if(item is null)_store.Data.Settlements.Add(x);Save();
    }

    private void DeletePartner(){var x=Selected<Partner>(_partners,_store.Data.Partners);if(x is null||!Confirm("거래처를 삭제할까요?"))return;if(_store.Data.Trades.Any(z=>z.PartnerName==x.Name)||_store.Data.Settlements.Any(z=>z.PartnerName==x.Name)){MessageBox.Show("연결된 거래 또는 지급·수금 내역이 있어 삭제할 수 없습니다.");return;}_store.Data.Partners.Remove(x);Save();}
    private void DeleteProduct(){var x=Selected<Product>(_products,_store.Data.Products);if(x is null||!Confirm("상품을 삭제할까요?"))return;if(_store.Data.Trades.Any(z=>z.ProductName==x.Name)){MessageBox.Show("연결된 거래가 있어 삭제할 수 없습니다.");return;}_store.Data.Products.Remove(x);Save();}
    private void DeleteTrade(){var x=Selected<Trade>(_trades,_store.Data.Trades);if(x is null||!Confirm("거래를 삭제할까요?"))return;_store.Data.Trades.Remove(x);Save();}
    private void DeleteSettlement(){var x=Selected<Settlement>(_settlements,_store.Data.Settlements);if(x is null||!Confirm("지급·수금 내역을 삭제할까요?"))return;_store.Data.Settlements.Remove(x);Save();}

    private void RefreshAll()
    {
        RefreshPartners();_products.DataSource=_store.Data.Products.OrderBy(x=>x.Name).Select(x=>new{x.Id,상품명=x.Name,상품코드=x.Code,규격=x.Specification,단위=x.Unit,단가=x.UnitPrice,초기재고=x.InitialStock,매입수량=_store.Data.Trades.Where(t=>t.Type=="매입"&&t.ProductName==x.Name).Sum(t=>t.Quantity),매출수량=_store.Data.Trades.Where(t=>t.Type=="매출"&&t.ProductName==x.Name).Sum(t=>t.Quantity),재고=x.InitialStock+_store.Data.Trades.Where(t=>t.Type=="매입"&&t.ProductName==x.Name).Sum(t=>t.Quantity)-_store.Data.Trades.Where(t=>t.Type=="매출"&&t.ProductName==x.Name).Sum(t=>t.Quantity)}).ToList();Hide(_products);
        _trades.DataSource=_store.Data.Trades.OrderByDescending(x=>x.TradeDate).Select(x=>new{x.Id,거래일자=x.TradeDate.ToString("yyyy-MM-dd"),구분=x.Type,거래처=x.PartnerName,상품=x.ProductName,수량=x.Quantity,단가=x.UnitPrice,세액구분=x.TaxType,공급가액=x.SupplyAmount,세액=x.TaxAmount,추가=x.AdditionalAmount,할인=x.DiscountAmount,합계=x.Total,x.Memo}).ToList();Hide(_trades);
        _settlements.DataSource=_store.Data.Settlements.OrderByDescending(x=>x.Date).Select(x=>new{x.Id,일자=x.Date.ToString("yyyy-MM-dd"),구분=x.Type,거래처=x.PartnerName,적요=x.Description,금액=x.Amount,x.Memo}).ToList();Hide(_settlements);
        var names=_store.Data.Partners.Select(x=>x.Name).Distinct().Order().ToArray();var old=_ledgerPartner.Text;_ledgerPartner.DataSource=names;if(names.Contains(old))_ledgerPartner.SelectedItem=old;RefreshLedger();
        var buy=_store.Data.Trades.Where(x=>x.Type=="매입").Sum(x=>x.Total);var sales=_store.Data.Trades.Where(x=>x.Type=="매출").Sum(x=>x.Total);var paid=_store.Data.Settlements.Where(x=>x.Type=="지급").Sum(x=>x.Amount);var received=_store.Data.Settlements.Where(x=>x.Type=="수금").Sum(x=>x.Amount);_summary.Text=$"매입 {buy:N0}원  지급 {paid:N0}원  미지급 {buy-paid:N0}원\n매출 {sales:N0}원  수금 {received:N0}원  미수 {sales-received:N0}원";_backup.Text=_store.LastBackupStatus;
    }
    private void RefreshPartners(){if(_partnerType.SelectedItem is null)return;var type=_partnerType.Text;_partners.DataSource=_store.Data.Partners.Where(x=>type=="전체"||x.Type==type).OrderBy(x=>x.Name).Select(x=>new{x.Id,구분=x.Type,거래처명=x.Name,대표자=x.Representative,사업자번호=x.BusinessNumber,대표번호=x.MainPhone,담당자=x.ContactName,전화=x.ContactPhone,이메일=x.Email,주소=x.Address}).ToList();Hide(_partners);}
    private void RefreshLedger(){var name=_ledgerPartner.Text;if(name==""){_ledger.DataSource=null;return;}var rows=new List<object>();rows.AddRange(_store.Data.Trades.Where(x=>x.PartnerName==name).Select(x=>(object)new{일자=x.TradeDate.ToString("yyyy-MM-dd"),구분=x.Type,내용=x.ProductName,매입매출=x.Total,지급수금=0m}));rows.AddRange(_store.Data.Settlements.Where(x=>x.PartnerName==name).Select(x=>(object)new{일자=x.Date.ToString("yyyy-MM-dd"),구분=x.Type,내용=x.Description,매입매출=0m,지급수금=x.Amount}));_ledger.DataSource=rows.OrderBy(x=>x.GetType().GetProperty("일자")?.GetValue(x)).ToList();MoneyFormat(_ledger);}

    private async Task CheckUpdate(){try{UseWaitCursor=true;var r=await UpdateService.GetLatestReleaseAsync();if(!UpdateService.IsNewer(r.Version)){MessageBox.Show($"현재 최신 버전입니다. ({UpdateService.CurrentVersion})");return;}if(MessageBox.Show($"새 버전 {r.Version}이 있습니다. 데이터를 백업하고 업데이트할까요?","업데이트",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;var b=_store.CreateUpdateBackup();await UpdateService.DownloadAndRestartAsync(r);MessageBox.Show($"업데이트 파일을 받았습니다.\n백업: {b}");Application.Exit();}catch(Exception ex){MessageBox.Show(ex.Message,"업데이트 오류");}finally{UseWaitCursor=false;}}
    private void Save(string? msg=null){_store.Save();RefreshAll();if(msg is not null)MessageBox.Show(msg);}
    private static T? Selected<T>(DataGridView g,List<T> list) where T:class {if(g.CurrentRow?.Cells["Id"].Value is not object v||!Guid.TryParse(v.ToString(),out var id))return null;return list.FirstOrDefault(x=>(Guid)(typeof(T).GetProperty("Id")?.GetValue(x)??Guid.Empty)==id);}
    private static bool Confirm(string s)=>MessageBox.Show(s,"확인",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes;
    private static ComboBox Choice(string[] a,string value){var c=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,DataSource=a};if(a.Contains(value))c.SelectedItem=value;return c;}
    private static NumericUpDown Money(decimal v)=>new(){Minimum=0,Maximum=1_000_000_000,DecimalPlaces=2,ThousandsSeparator=true,Value=Math.Clamp(v,0,1_000_000_000)};
    private static string T(Dictionary<string,Control> f,string k)=>((TextBox)f[k]).Text.Trim();private static decimal N(Dictionary<string,Control> f,string k)=>((NumericUpDown)f[k]).Value;
    private static TableLayoutPanel FormPanel(Dictionary<string,Control> f){var p=new TableLayoutPanel{ColumnCount=2,RowCount=f.Count,Padding=new Padding(18),AutoScroll=true};p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,130));p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));int r=0;foreach(var(x,c)in f){p.RowStyles.Add(new RowStyle(SizeType.Absolute,38));p.Controls.Add(new Label{Text=x,AutoSize=true,Padding=new Padding(0,8,0,0)},0,r);c.Dock=DockStyle.Fill;p.Controls.Add(c,1,r++);}return p;}
    private static bool Dialog(string title,Dictionary<string,Control> f){using var d=new Form{Text=title,Width=560,Height=Math.Min(780,130+f.Count*40),StartPosition=FormStartPosition.CenterParent,Font=new Font("맑은 고딕",9.5f)};var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=52,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(8)};var ok=new Button{Text="저장",DialogResult=DialogResult.OK,Width=90};var cancel=new Button{Text="취소",DialogResult=DialogResult.Cancel,Width=90};buttons.Controls.Add(ok);buttons.Controls.Add(cancel);var panel=FormPanel(f);panel.Dock=DockStyle.Fill;d.Controls.Add(panel);d.Controls.Add(buttons);d.AcceptButton=ok;d.CancelButton=cancel;return d.ShowDialog()==DialogResult.OK;}
    private static void Hide(DataGridView g){if(g.Columns.Contains("Id"))g.Columns["Id"].Visible=false;MoneyFormat(g);}private static void MoneyFormat(DataGridView g){foreach(DataGridViewColumn c in g.Columns)if(new[]{"단가","초기재고","매입수량","매출수량","재고","수량","공급가액","세액","추가","할인","합계","금액","매입매출","지급수금"}.Contains(c.Name))c.DefaultCellStyle.Format="N0";}
}
