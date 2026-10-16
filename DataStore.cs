using System.Text.Json;

namespace JieipManager;

public sealed class DataStore
{
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };
    public string BaseFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JieipManager");
    public string DataFile => Path.Combine(BaseFolder, "data.json");
    public AppData Data { get; private set; } = new();

    public DataStore() => Load();

    public void Load()
    {
        Directory.CreateDirectory(BaseFolder);
        try
        {
            if (File.Exists(DataFile))
                Data = JsonSerializer.Deserialize<AppData>(File.ReadAllText(DataFile), _json) ?? new();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"데이터를 읽지 못했습니다. 새 데이터로 시작합니다.\n{ex.Message}", "데이터 오류");
            Data = new();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(BaseFolder);
        if (File.Exists(DataFile))
        {
            var backupDir = Path.Combine(BaseFolder, "backup");
            Directory.CreateDirectory(backupDir);
            File.Copy(DataFile, Path.Combine(backupDir, $"data_{DateTime.Now:yyyyMMdd_HHmmss}.json"), true);
            foreach (var old in new DirectoryInfo(backupDir).GetFiles("data_*.json").OrderByDescending(x => x.CreationTime).Skip(60)) old.Delete();
        }
        File.WriteAllText(DataFile, JsonSerializer.Serialize(Data, _json));
        if (!string.IsNullOrWhiteSpace(Data.Settings.BackupFolder))
        {
            try
            {
                Directory.CreateDirectory(Data.Settings.BackupFolder);
                File.Copy(DataFile, Path.Combine(Data.Settings.BackupFolder, $"지입관리_{DateTime.Today:yyyyMMdd}.json"), true);
            }
            catch { /* 외부 백업 실패가 업무 저장을 막지 않도록 한다. */ }
        }
    }

    public string CreateUpdateBackup()
    {
        Save();
        var backupDir = Path.Combine(BaseFolder, "backup");
        Directory.CreateDirectory(backupDir);
        var path = Path.Combine(backupDir, $"업데이트전_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        File.Copy(DataFile, path, true);
        return path;
    }
}
