using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace JieipManager;

public sealed class MainForm : Form
{
    private sealed record VehicleChoice(Guid? Id, string Name);
    private readonly DataStore _store = new();
    private readonly DataGridView _owners = Grid(), _vehicles = Grid(), _charges = Grid(), _payments = Grid(), _works = Grid();
    private readonly Label _summary = new() { AutoSize = true, Font = new Font("맑은 고딕", 16, FontStyle.Bold), Padding = new Padding(12) };
    private readonly TextBox _month = new() { Width = 90, Text = DateTime.Today.ToString("yyyy-MM") };
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };

    public MainForm()
    {
        Text = $"지입관리 {UpdateService.CurrentVersion}";
        Width = 1280; Height = 820; StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("맑은 고딕", 9.5f);
        Controls.Add(_tabs);
        BuildDashboard(); BuildMaster(); BuildCharges(); BuildPayments(); BuildWorks(); BuildSettings();
        RefreshAll();
        FormClosing += (_, _) => _store.Save();
    }

    private static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false, BackgroundColor = Color.White
    };

    private static Button Btn(string text, EventHandler click)
    {
        var b = new Button { Text = text, AutoSize = true, Height = 34, Margin = new Padding(4) };
        b.Click += click; return b;
    }

    private static FlowLayoutPanel Bar(params Control[] controls)
    {
        var p = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(6), WrapContents = false };
        p.Controls.AddRange(controls); return p;
    }

    private void BuildDashboard()
    {
        var page = new TabPage("현황");
        page.Controls.Add(new Label
        {
            Dock = DockStyle.Fill, Text = "왼쪽 상단 탭에서 차주·차량을 등록한 뒤 월 부과를 생성하세요.\n\n" +
                "• 검사·보험 만료 30일 전부터 업무일정에서 확인할 수 있습니다.\n" +
                "• 모든 변경은 자동 저장되며 최근 60개 백업을 보관합니다.",
            Font = new Font("맑은 고딕", 12), Padding = new Padding(24), TextAlign = ContentAlignment.MiddleLeft
        });
        page.Controls.Add(_summary); _summary.Dock = DockStyle.Top;
        _tabs.TabPages.Add(page);
    }

    private void BuildMaster()
    {
        var page = new TabPage("차주·차량");
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 330 };
        split.Panel1.Controls.Add(_owners);
        split.Panel1.Controls.Add(Bar(Btn("차주 추가", (_, _) => EditOwner(null)), Btn("수정", (_, _) => EditOwner(SelectedOwner())), Btn("삭제", (_, _) => DeleteOwner())));
        split.Panel2.Controls.Add(_vehicles);
        split.Panel2.Controls.Add(Bar(Btn("차량 추가", (_, _) => EditVehicle(null)), Btn("수정", (_, _) => EditVehicle(SelectedVehicle())), Btn("삭제", (_, _) => DeleteVehicle())));
        page.Controls.Add(split); _tabs.TabPages.Add(page);
    }

    private void BuildCharges()
    {
        var page = new TabPage("부과·청구"); page.Controls.Add(_charges);
        var monthLabel = new Label { Text = "부과월", AutoSize = true, Padding = new Padding(8, 8, 0, 0) };
        page.Controls.Add(Bar(monthLabel, _month,
            Btn("월 부과 생성", (_, _) => GenerateCharges()), Btn("직접 추가", (_, _) => EditCharge(null)),
            Btn("수정", (_, _) => EditCharge(SelectedCharge())), Btn("삭제", (_, _) => DeleteCharge()),
            Btn("청구서 열기/인쇄", (_, _) => OpenInvoice()), Btn("CSV 내보내기", (_, _) => ExportCsv())));
        _tabs.TabPages.Add(page);
    }

    private void BuildPayments()
    {
        var page = new TabPage("수납"); page.Controls.Add(_payments);
        page.Controls.Add(Bar(Btn("선택 청구 수납", (_, _) => AddPayment()), Btn("수납 취소", (_, _) => DeletePayment())));
        _tabs.TabPages.Add(page);
    }

    private void BuildWorks()
    {
        var page = new TabPage("업무일정"); page.Controls.Add(_works);
        page.Controls.Add(Bar(Btn("업무 추가", (_, _) => EditWork(null)), Btn("수정", (_, _) => EditWork(SelectedWork())), Btn("완료", (_, _) => CompleteWork()), Btn("삭제", (_, _) => DeleteWork())));
        _tabs.TabPages.Add(page);
    }

    private void BuildSettings()
    {
        var page = new TabPage("설정");
        var s = _store.Data.Settings;
        var fields = new Dictionary<string, Control>
        {
            ["회사명"] = new TextBox { Text = s.CompanyName }, ["사업자번호"] = new TextBox { Text = s.BusinessNumber },
            ["대표자"] = new TextBox { Text = s.Representative }, ["전화"] = new TextBox { Text = s.Phone }, ["주소"] = new TextBox { Text = s.Address },
            ["기본 지입관리비"] = Money(s.ManagementFee), ["기본 공과금"] = Money(s.UtilityFee), ["기본 보험료"] = Money(s.InsuranceFee),
            ["기본 협회비"] = Money(s.AssociationFee), ["기본 기타"] = Money(s.EtcFee), ["납부기한(일)"] = Money(s.DueDay, 1, 28),
            ["외부 백업 폴더"] = new TextBox { Text = s.BackupFolder }
        };
        var panel = FormPanel(fields); panel.Dock = DockStyle.Top; panel.Height = 520; page.Controls.Add(panel);
        page.Controls.Add(Bar(Btn("설정 저장", (_, _) =>
        {
            s.CompanyName = FieldText(fields, "회사명"); s.BusinessNumber = FieldText(fields, "사업자번호"); s.Representative = FieldText(fields, "대표자");
            s.Phone = FieldText(fields, "전화"); s.Address = FieldText(fields, "주소"); s.ManagementFee = Num(fields, "기본 지입관리비");
            s.UtilityFee = Num(fields, "기본 공과금"); s.InsuranceFee = Num(fields, "기본 보험료"); s.AssociationFee = Num(fields, "기본 협회비");
            s.EtcFee = Num(fields, "기본 기타"); s.DueDay = Num(fields, "납부기한(일)"); s.BackupFolder = FieldText(fields, "외부 백업 폴더");
            SaveRefresh("설정을 저장했습니다.");
        }), Btn("데이터 폴더 열기", (_, _) => Process.Start(new ProcessStartInfo("explorer.exe", _store.BaseFolder) { UseShellExecute = true })),
            Btn("업데이트 확인", async (_, _) => await CheckForUpdatesAsync()),
            new Label { Text = $"현재 버전 {UpdateService.CurrentVersion}", AutoSize = true, Padding = new Padding(12, 9, 0, 0) }));
        _tabs.TabPages.Add(page);
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            UseWaitCursor = true;
            var release = await UpdateService.GetLatestReleaseAsync();
            if (!UpdateService.IsNewer(release.Version))
            {
                MessageBox.Show($"현재 최신 버전입니다.\n현재: {UpdateService.CurrentVersion}", "업데이트");
                return;
            }

            var notes = string.IsNullOrWhiteSpace(release.Notes) ? "변경 사항 설명이 없습니다." : release.Notes.Trim();
            if (notes.Length > 800) notes = notes[..800] + "…";
            var answer = MessageBox.Show(
                $"새 버전 {release.Version}이 있습니다.\n\n{notes}\n\n데이터를 백업하고 지금 업데이트할까요?",
                "지입관리 업데이트", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (answer != DialogResult.Yes) return;

            var backup = _store.CreateUpdateBackup();
            await UpdateService.DownloadAndRestartAsync(release);
            MessageBox.Show($"업데이트 파일을 받았습니다.\n백업: {backup}\n\n프로그램을 종료하고 새 버전으로 다시 실행합니다.", "업데이트");
            Application.Exit();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"업데이트를 확인하거나 설치하지 못했습니다.\n\n{ex.Message}", "업데이트 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private static NumericUpDown Money(int value, int min = 0, int max = 100_000_000) => new() { Minimum = min, Maximum = max, Value = Math.Clamp(value, min, max), ThousandsSeparator = true };
    private static string FieldText(Dictionary<string, Control> f, string key) => ((TextBox)f[key]).Text.Trim();
    private static int Num(Dictionary<string, Control> f, string key) => (int)((NumericUpDown)f[key]).Value;

    private static TableLayoutPanel FormPanel(Dictionary<string, Control> fields)
    {
        var p = new TableLayoutPanel { ColumnCount = 2, RowCount = fields.Count, Padding = new Padding(18), AutoScroll = true };
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145)); p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        int r = 0;
        foreach (var (name, control) in fields)
        {
            p.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            p.Controls.Add(new Label { Text = name, AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, 0, r);
            control.Dock = DockStyle.Fill; control.Margin = new Padding(3, 3, 3, 6); p.Controls.Add(control, 1, r++);
        }
        return p;
    }

    private static bool ShowEditor(string title, Dictionary<string, Control> fields)
    {
        using var f = new Form { Text = title, Width = 520, Height = Math.Min(760, 110 + fields.Count * 42), StartPosition = FormStartPosition.CenterParent, Font = new Font("맑은 고딕", 9.5f) };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        var ok = new Button { Text = "저장", DialogResult = DialogResult.OK, Width = 90 }; var cancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel, Width = 90 };
        buttons.Controls.Add(ok); buttons.Controls.Add(cancel); f.Controls.Add(FormPanel(fields)); f.Controls[^1].Dock = DockStyle.Fill; f.Controls.Add(buttons);
        f.AcceptButton = ok; f.CancelButton = cancel; return f.ShowDialog() == DialogResult.OK;
    }

    private Owner? SelectedOwner() => SelectedId(_owners) is Guid id ? _store.Data.Owners.FirstOrDefault(x => x.Id == id) : null;
    private Vehicle? SelectedVehicle() => SelectedId(_vehicles) is Guid id ? _store.Data.Vehicles.FirstOrDefault(x => x.Id == id) : null;
    private Charge? SelectedCharge() => SelectedId(_charges) is Guid id ? _store.Data.Charges.FirstOrDefault(x => x.Id == id) : null;
    private WorkItem? SelectedWork() => SelectedId(_works) is Guid id ? _store.Data.WorkItems.FirstOrDefault(x => x.Id == id) : null;
    private static Guid? SelectedId(DataGridView g) => g.CurrentRow?.Cells["Id"].Value is Guid id ? id : Guid.TryParse(g.CurrentRow?.Cells["Id"].Value?.ToString(), out var x) ? x : null;

    private void EditOwner(Owner? item)
    {
        var x = item ?? new Owner();
        var f = new Dictionary<string, Control> { ["성명/상호"] = new TextBox { Text = x.Name }, ["사업자번호"] = new TextBox { Text = x.BusinessNumber }, ["전화"] = new TextBox { Text = x.Phone }, ["주소"] = new TextBox { Text = x.Address }, ["계약일"] = new DateTimePicker { Value = x.JoinDate }, ["사용"] = new CheckBox { Checked = x.Active }, ["메모"] = new TextBox { Text = x.Memo, Multiline = true } };
        if (!ShowEditor("차주", f)) return;
        x.Name = FieldText(f, "성명/상호"); if (x.Name == "") { MessageBox.Show("성명을 입력하세요."); return; }
        x.BusinessNumber = FieldText(f, "사업자번호"); x.Phone = FieldText(f, "전화"); x.Address = FieldText(f, "주소"); x.JoinDate = ((DateTimePicker)f["계약일"]).Value.Date; x.Active = ((CheckBox)f["사용"]).Checked; x.Memo = FieldText(f, "메모");
        if (item is null) _store.Data.Owners.Add(x); SaveRefresh();
    }

    private void EditVehicle(Vehicle? item)
    {
        if (_store.Data.Owners.Count == 0) { MessageBox.Show("차주를 먼저 등록하세요."); return; }
        var x = item ?? new Vehicle(); var owners = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DataSource = _store.Data.Owners.ToList(), DisplayMember = "Name", ValueMember = "Id" };
        if (x.OwnerId != Guid.Empty) owners.SelectedValue = x.OwnerId;
        var f = new Dictionary<string, Control> { ["차주"] = owners, ["차량번호"] = new TextBox { Text = x.PlateNumber }, ["차종"] = new TextBox { Text = x.VehicleType }, ["모델"] = new TextBox { Text = x.Model }, ["연식"] = Money(x.Year, 1980, 2100), ["검사 만료"] = NullableDate(x.InspectionDue), ["보험 만료"] = NullableDate(x.InsuranceDue), ["사용"] = new CheckBox { Checked = x.Active }, ["메모"] = new TextBox { Text = x.Memo, Multiline = true } };
        if (!ShowEditor("차량", f)) return;
        x.OwnerId = (Guid)(owners.SelectedValue ?? Guid.Empty); x.PlateNumber = FieldText(f, "차량번호"); if (x.PlateNumber == "") { MessageBox.Show("차량번호를 입력하세요."); return; }
        x.VehicleType = FieldText(f, "차종"); x.Model = FieldText(f, "모델"); x.Year = Num(f, "연식"); x.InspectionDue = DateValue(f["검사 만료"]); x.InsuranceDue = DateValue(f["보험 만료"]); x.Active = ((CheckBox)f["사용"]).Checked; x.Memo = FieldText(f, "메모");
        if (item is null) _store.Data.Vehicles.Add(x); SaveRefresh();
    }

    private static DateTimePicker NullableDate(DateTime? value) => new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = value.HasValue, Value = value ?? DateTime.Today };
    private static DateTime? DateValue(Control c) => c is DateTimePicker d && d.Checked ? d.Value.Date : null;

    private void EditCharge(Charge? item)
    {
        if (_store.Data.Vehicles.Count == 0) { MessageBox.Show("차량을 먼저 등록하세요."); return; }
        var x = item ?? new Charge(); var vehicles = VehicleCombo(x.VehicleId);
        var f = new Dictionary<string, Control> { ["부과월"] = new TextBox { Text = x.BillingMonth }, ["차량"] = vehicles, ["지입관리비"] = Money(x.ManagementFee), ["공과금"] = Money(x.UtilityFee), ["보험료"] = Money(x.InsuranceFee), ["협회비"] = Money(x.AssociationFee), ["기타"] = Money(x.EtcFee), ["납부기한"] = new DateTimePicker { Value = x.DueDate }, ["메모"] = new TextBox { Text = x.Memo } };
        if (!ShowEditor("부과", f)) return; var v = _store.Data.Vehicles.First(z => z.Id == (Guid)vehicles.SelectedValue!);
        x.BillingMonth = FieldText(f, "부과월"); x.VehicleId = v.Id; x.OwnerId = v.OwnerId; x.ManagementFee = Num(f, "지입관리비"); x.UtilityFee = Num(f, "공과금"); x.InsuranceFee = Num(f, "보험료"); x.AssociationFee = Num(f, "협회비"); x.EtcFee = Num(f, "기타"); x.DueDate = ((DateTimePicker)f["납부기한"]).Value.Date; x.Memo = FieldText(f, "메모");
        if (item is null) _store.Data.Charges.Add(x); SaveRefresh();
    }

    private ComboBox VehicleCombo(Guid selected)
    {
        var list = _store.Data.Vehicles.Select(v => new { v.Id, Name = $"{v.PlateNumber} - {OwnerName(v.OwnerId)}" }).ToList();
        var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DataSource = list, DisplayMember = "Name", ValueMember = "Id" }; if (selected != Guid.Empty) c.SelectedValue = selected; return c;
    }

    private void GenerateCharges()
    {
        if (!DateTime.TryParse(_month.Text + "-01", out var baseDate)) { MessageBox.Show("부과월을 yyyy-MM 형식으로 입력하세요."); return; }
        var s = _store.Data.Settings; int count = 0;
        foreach (var v in _store.Data.Vehicles.Where(v => v.Active && _store.Data.Owners.Any(o => o.Id == v.OwnerId && o.Active)))
        {
            if (_store.Data.Charges.Any(c => c.BillingMonth == _month.Text && c.VehicleId == v.Id)) continue;
            _store.Data.Charges.Add(new Charge { BillingMonth = _month.Text, OwnerId = v.OwnerId, VehicleId = v.Id, ManagementFee = s.ManagementFee, UtilityFee = s.UtilityFee, InsuranceFee = s.InsuranceFee, AssociationFee = s.AssociationFee, EtcFee = s.EtcFee, DueDate = new DateTime(baseDate.Year, baseDate.Month, Math.Min(s.DueDay, DateTime.DaysInMonth(baseDate.Year, baseDate.Month))) }); count++;
        }
        SaveRefresh($"{count}건의 월 부과를 생성했습니다.");
    }

    private void AddPayment()
    {
        var c = SelectedCharge(); if (c is null) { MessageBox.Show("부과·청구 탭에서 청구 건을 선택하세요."); _tabs.SelectedIndex = 2; return; }
        int paid = _store.Data.Payments.Where(x => x.ChargeId == c.Id).Sum(x => x.Amount); int remain = c.Total - paid;
        var f = new Dictionary<string, Control> { ["수납일"] = new DateTimePicker { Value = DateTime.Today }, ["금액"] = Money(Math.Max(0, remain)), ["방법"] = new ComboBox { DataSource = new[] { "계좌이체", "현금", "카드", "기타" }, DropDownStyle = ComboBoxStyle.DropDownList }, ["메모"] = new TextBox() };
        if (!ShowEditor("수납", f)) return;
        _store.Data.Payments.Add(new Payment { ChargeId = c.Id, PaidAt = ((DateTimePicker)f["수납일"]).Value.Date, Amount = Num(f, "금액"), Method = ((ComboBox)f["방법"]).Text, Memo = FieldText(f, "메모") }); SaveRefresh();
    }

    private void EditWork(WorkItem? item)
    {
        var x = item ?? new WorkItem();
        var vehicleChoices = new List<VehicleChoice> { new(null, "공통") };
        vehicleChoices.AddRange(_store.Data.Vehicles.Select(v => new VehicleChoice(v.Id, $"{v.PlateNumber} - {OwnerName(v.OwnerId)}")));
        var vehicles = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DataSource = vehicleChoices, DisplayMember = "Name" };
        vehicles.SelectedItem = vehicleChoices.FirstOrDefault(v => v.Id == x.VehicleId) ?? vehicleChoices[0];
        var type = new ComboBox { DataSource = new[] { "검사", "보험", "보험처리", "대폐차", "세무지원", "기타" }, DropDownStyle = ComboBoxStyle.DropDownList, Text = x.Type };
        var status = new ComboBox { DataSource = new[] { "대기", "진행", "완료", "보류" }, DropDownStyle = ComboBoxStyle.DropDownList, Text = x.Status };
        var f = new Dictionary<string, Control> { ["구분"] = type, ["차량"] = vehicles, ["제목"] = new TextBox { Text = x.Title }, ["기한"] = new DateTimePicker { Value = x.DueDate }, ["상태"] = status, ["메모"] = new TextBox { Text = x.Memo, Multiline = true } };
        if (!ShowEditor("업무", f)) return; x.Type = type.Text; x.VehicleId = (vehicles.SelectedItem as VehicleChoice)?.Id; x.Title = FieldText(f, "제목"); x.DueDate = ((DateTimePicker)f["기한"]).Value.Date; x.Status = status.Text; x.Memo = FieldText(f, "메모"); if (item is null) _store.Data.WorkItems.Add(x); SaveRefresh();
    }

    private void OpenInvoice()
    {
        var c = SelectedCharge(); if (c is null) { MessageBox.Show("청구 건을 선택하세요."); return; }
        int paid = Paid(c.Id); var s = _store.Data.Settings; var owner = _store.Data.Owners.FirstOrDefault(x => x.Id == c.OwnerId); var vehicle = _store.Data.Vehicles.FirstOrDefault(x => x.Id == c.VehicleId);
        string Row(string a, int b) => $"<tr><td>{a}</td><td class='n'>{b:N0}원</td></tr>";
        var html = $$"""<!doctype html><meta charset='utf-8'><title>청구서</title><style>body{font-family:'Malgun Gothic';max-width:760px;margin:40px auto}h1{text-align:center}table{width:100%;border-collapse:collapse}td,th{border:1px solid #777;padding:10px}.n{text-align:right}.sign{margin-top:50px;text-align:right}</style><h1>지입 관리비 청구서</h1><p><b>{{s.CompanyName}}</b> / {{s.BusinessNumber}} / {{s.Phone}}</p><p>청구월: {{c.BillingMonth}} &nbsp; 납부기한: {{c.DueDate:yyyy-MM-dd}}</p><p>차주: {{owner?.Name}} &nbsp; 차량: {{vehicle?.PlateNumber}}</p><table><tr><th>항목</th><th>금액</th></tr>{{Row("지입관리비",c.ManagementFee)}}{{Row("공과금",c.UtilityFee)}}{{Row("보험료",c.InsuranceFee)}}{{Row("협회비",c.AssociationFee)}}{{Row("기타",c.EtcFee)}}<tr><th>합계</th><th class='n'>{{c.Total:N0}}원</th></tr><tr><td>수납/미수</td><td class='n'>{{paid:N0}}원 / {{c.Total-paid:N0}}원</td></tr></table><p>메모: {{System.Net.WebUtility.HtmlEncode(c.Memo)}}</p><div class='sign'>{{s.CompanyName}} 대표 {{s.Representative}}</div><script>window.print()</script>""";
        var path = Path.Combine(Path.GetTempPath(), $"지입청구서_{c.BillingMonth}_{vehicle?.PlateNumber}.html"); File.WriteAllText(path, html, Encoding.UTF8); Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private void ExportCsv()
    {
        using var d = new SaveFileDialog { Filter = "CSV 파일|*.csv", FileName = $"지입부과_{_month.Text}.csv" }; if (d.ShowDialog() != DialogResult.OK) return;
        var sb = new StringBuilder("부과월,차주,차량번호,지입관리비,공과금,보험료,협회비,기타,합계,수납,미수,납부기한\r\n");
        foreach (var c in _store.Data.Charges.Where(x => x.BillingMonth == _month.Text)) sb.AppendLine($"{c.BillingMonth},\"{OwnerName(c.OwnerId)}\",\"{Plate(c.VehicleId)}\",{c.ManagementFee},{c.UtilityFee},{c.InsuranceFee},{c.AssociationFee},{c.EtcFee},{c.Total},{Paid(c.Id)},{c.Total-Paid(c.Id)},{c.DueDate:yyyy-MM-dd}");
        File.WriteAllText(d.FileName, "\uFEFF" + sb, Encoding.UTF8); MessageBox.Show("내보냈습니다.");
    }

    private void DeleteOwner() { var x = SelectedOwner(); if (x is null || !Confirm("차주를 삭제할까요?")) return; if (_store.Data.Vehicles.Any(v => v.OwnerId == x.Id)) { MessageBox.Show("연결된 차량을 먼저 삭제하거나 다른 차주로 변경하세요."); return; } _store.Data.Owners.Remove(x); SaveRefresh(); }
    private void DeleteVehicle() { var x = SelectedVehicle(); if (x is null || !Confirm("차량을 삭제할까요?")) return; if (_store.Data.Charges.Any(c => c.VehicleId == x.Id)) { MessageBox.Show("청구 이력이 있어 삭제할 수 없습니다. 사용 여부를 해제하세요."); return; } _store.Data.Vehicles.Remove(x); SaveRefresh(); }
    private void DeleteCharge() { var x = SelectedCharge(); if (x is null || !Confirm("청구와 연결된 수납을 삭제할까요?")) return; _store.Data.Payments.RemoveAll(p => p.ChargeId == x.Id); _store.Data.Charges.Remove(x); SaveRefresh(); }
    private void DeletePayment() { if (SelectedId(_payments) is not Guid id || !Confirm("수납을 취소할까요?")) return; _store.Data.Payments.RemoveAll(x => x.Id == id); SaveRefresh(); }
    private void DeleteWork() { var x = SelectedWork(); if (x is null || !Confirm("업무를 삭제할까요?")) return; _store.Data.WorkItems.Remove(x); SaveRefresh(); }
    private void CompleteWork() { var x = SelectedWork(); if (x is null) return; x.Status = "완료"; SaveRefresh(); }
    private static bool Confirm(string text) => MessageBox.Show(text, "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    private string OwnerName(Guid id) => _store.Data.Owners.FirstOrDefault(x => x.Id == id)?.Name ?? "(미지정)";
    private string Plate(Guid id) => _store.Data.Vehicles.FirstOrDefault(x => x.Id == id)?.PlateNumber ?? "";
    private int Paid(Guid chargeId) => _store.Data.Payments.Where(x => x.ChargeId == chargeId).Sum(x => x.Amount);
    private void SaveRefresh(string? message = null) { _store.Save(); RefreshAll(); if (message is not null) MessageBox.Show(message); }

    private void RefreshAll()
    {
        _owners.DataSource = _store.Data.Owners.OrderBy(x => x.Name).Select(x => new { x.Id, 성명 = x.Name, 사업자번호 = x.BusinessNumber, 전화 = x.Phone, 계약일 = x.JoinDate.ToString("yyyy-MM-dd"), 상태 = x.Active ? "사용" : "종료", x.Memo }).ToList(); HideId(_owners);
        _vehicles.DataSource = _store.Data.Vehicles.OrderBy(x => x.PlateNumber).Select(x => new { x.Id, 차주 = OwnerName(x.OwnerId), 차량번호 = x.PlateNumber, 차종 = x.VehicleType, 모델 = x.Model, 연식 = x.Year, 검사만료 = x.InspectionDue?.ToString("yyyy-MM-dd"), 보험만료 = x.InsuranceDue?.ToString("yyyy-MM-dd"), 상태 = x.Active ? "사용" : "종료" }).ToList(); HideId(_vehicles);
        _charges.DataSource = _store.Data.Charges.OrderByDescending(x => x.BillingMonth).ThenBy(x => OwnerName(x.OwnerId)).Select(x => new { x.Id, 부과월 = x.BillingMonth, 차주 = OwnerName(x.OwnerId), 차량 = Plate(x.VehicleId), 관리비 = x.ManagementFee, 공과금 = x.UtilityFee, 보험료 = x.InsuranceFee, 협회비 = x.AssociationFee, 기타 = x.EtcFee, 합계 = x.Total, 수납 = Paid(x.Id), 미수 = x.Total - Paid(x.Id), 납부기한 = x.DueDate.ToString("yyyy-MM-dd") }).ToList(); HideId(_charges);
        _payments.DataSource = _store.Data.Payments.OrderByDescending(x => x.PaidAt).Select(x => { var c = _store.Data.Charges.FirstOrDefault(z => z.Id == x.ChargeId); return new { x.Id, 수납일 = x.PaidAt.ToString("yyyy-MM-dd"), 부과월 = c?.BillingMonth, 차주 = c is null ? "" : OwnerName(c.OwnerId), 차량 = c is null ? "" : Plate(c.VehicleId), 금액 = x.Amount, 방법 = x.Method, x.Memo }; }).ToList(); HideId(_payments);
        var autoWorks = _store.Data.Vehicles.SelectMany(v => new[] { v.InspectionDue is DateTime i ? new { Type="검사", Date=i, Vehicle=v } : null, v.InsuranceDue is DateTime n ? new { Type="보험", Date=n, Vehicle=v } : null }).Where(x => x is not null && x.Date <= DateTime.Today.AddDays(60)).Select(x => new { Id = Guid.Empty, 구분 = x!.Type, 차량 = x.Vehicle.PlateNumber, 제목 = $"{x.Type} 만료", 기한 = x.Date.ToString("yyyy-MM-dd"), 상태 = x.Date < DateTime.Today ? "기한초과" : "예정", 메모 = "차량정보 자동생성" });
        var manual = _store.Data.WorkItems.Select(x => new { x.Id, 구분 = x.Type, 차량 = x.VehicleId is Guid id ? Plate(id) : "공통", 제목 = x.Title, 기한 = x.DueDate.ToString("yyyy-MM-dd"), 상태 = x.Status, 메모 = x.Memo });
        _works.DataSource = autoWorks.Concat(manual).OrderBy(x => x.기한).ToList(); HideId(_works);
        int total = _store.Data.Charges.Sum(x => x.Total), paid = _store.Data.Payments.Sum(x => x.Amount), overdue = _store.Data.Charges.Where(x => x.DueDate < DateTime.Today).Sum(x => Math.Max(0, x.Total - Paid(x.Id)));
        _summary.Text = $"운영 차주 {_store.Data.Owners.Count(x => x.Active)}명   차량 {_store.Data.Vehicles.Count(x => x.Active)}대   총 청구 {total:N0}원   수납 {paid:N0}원   미수 {total-paid:N0}원   연체 {overdue:N0}원";
    }

    private static void HideId(DataGridView g) { if (g.Columns.Contains("Id")) g.Columns["Id"].Visible = false; foreach (DataGridViewColumn c in g.Columns) if (new[] { "관리비", "공과금", "보험료", "협회비", "기타", "합계", "수납", "미수", "금액" }.Contains(c.Name)) c.DefaultCellStyle.Format = "N0"; }
}
