using System.Text.Json;

namespace JieipManager;

public sealed class DataStore
{
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };
    public string BaseFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JieipManager");
    public string DataFile => Path.Combine(BaseFolder, "ledger-v2.json");
    public AppData Data { get; private set; } = new();
    public string LastBackupStatus { get; private set; } = "외부 백업 미설정";

    public DataStore() => Load();
    public void Replace(AppData data) { Data = data; Save(); }

    public void Load()
    {
        Directory.CreateDirectory(BaseFolder);
        if (!File.Exists(DataFile)) return;
        try { Data = JsonSerializer.Deserialize<AppData>(File.ReadAllText(DataFile), _json) ?? throw new InvalidDataException("빈 데이터입니다."); }
        catch (Exception ex)
        {
            var recovered = LatestValidBackup();
            if (recovered is not null)
            {
                Data = JsonSerializer.Deserialize<AppData>(File.ReadAllText(recovered), _json)!;
                MessageBox.Show($"데이터 파일 손상을 발견해 최신 백업으로 복구했습니다.\n{recovered}", "자동 복구");
            }
            else
            {
                MessageBox.Show($"데이터 파일을 읽지 못했습니다. 원본 보호를 위해 저장을 중단합니다.\n{ex.Message}", "데이터 오류");
                throw;
            }
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(BaseFolder);
        var json = JsonSerializer.Serialize(Data, _json); var temp = DataFile + ".tmp";
        File.WriteAllText(temp, json);
        _ = JsonSerializer.Deserialize<AppData>(File.ReadAllText(temp), _json) ?? throw new InvalidDataException("저장 검증 실패");
        if (File.Exists(DataFile)) { File.Copy(DataFile, BackupPath("data"), true); File.Move(temp, DataFile, true); CleanupBackups(); }
        else File.Move(temp, DataFile, true);
        SaveExternal();
    }

    public string CreateUpdateBackup() { Save(); var p=BackupPath("업데이트전"); File.Copy(DataFile,p,true); return p; }
    public string CreateImportBackup() { if(!File.Exists(DataFile)) return "기존 데이터 없음"; var p=BackupPath("엑셀가져오기전"); File.Copy(DataFile,p,true); return p; }
    private string BackupPath(string prefix) { var d=Path.Combine(BaseFolder,"backup"); Directory.CreateDirectory(d); return Path.Combine(d,$"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json"); }
    private string? LatestValidBackup()
    {
        var d=Path.Combine(BaseFolder,"backup"); if(!Directory.Exists(d)) return null;
        foreach(var f in new DirectoryInfo(d).GetFiles("*.json").OrderByDescending(x=>x.LastWriteTime)) try { if(JsonSerializer.Deserialize<AppData>(File.ReadAllText(f.FullName),_json) is not null)return f.FullName; } catch { }
        return null;
    }
    private void CleanupBackups() { var d=Path.Combine(BaseFolder,"backup"); foreach(var f in new DirectoryInfo(d).GetFiles("*.json").OrderByDescending(x=>x.CreationTime).Skip(100)) f.Delete(); }
    private void SaveExternal()
    {
        if(string.IsNullOrWhiteSpace(Data.Settings.BackupFolder)){LastBackupStatus="외부 백업 미설정";return;}
        try { Directory.CreateDirectory(Data.Settings.BackupFolder); File.Copy(DataFile,Path.Combine(Data.Settings.BackupFolder,$"지입관리_{DateTime.Now:yyyyMMdd_HHmmss}.json"),true); LastBackupStatus=$"외부 백업 성공 {DateTime.Now:yyyy-MM-dd HH:mm}"; }
        catch(Exception ex){LastBackupStatus=$"외부 백업 실패: {ex.Message}";}
    }
}
