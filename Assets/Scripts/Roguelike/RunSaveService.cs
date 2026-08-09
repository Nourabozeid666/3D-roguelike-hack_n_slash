using System.IO;
using UnityEngine;

/// <summary>
/// Plain-C# persistence for a run save: one JSON file under Application.persistentDataPath
/// (default file name run_save_v1.json). Corrupt or invalid saves are treated as "no save" — they
/// never crash and never fabricate progress — and the bad file is removed so later HasSave()/
/// Continue checks are not repeatedly poisoned. Opening the game NEVER writes: writes happen only at
/// the explicit save points (every floor start, via RunBootstrap) and at New Run (Delete). No
/// singleton, no EventBus; construct one and inject a file path (tests pass a temp path).
/// </summary>
public class RunSaveService
{
    const string FileName = "run_save_v1.json";

    readonly string filePath;

    public RunSaveService() : this(Path.Combine(Application.persistentDataPath, FileName)) { }

    public RunSaveService(string filePath)
    {
        this.filePath = filePath;
    }

    public bool HasSave() => File.Exists(filePath);

    public bool Save(SaveData data)
    {
        try
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, JsonUtility.ToJson(data, true));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[RunSaveService] Save failed: " + e.Message);
            return false;
        }
    }

    /// <summary>Load and validate the save. Returns false (never throws) when the file is missing,
    /// corrupt, or holds inconsistent progress; the bad file is removed in the corrupt case.</summary>
    public bool TryLoad(out SaveData data)
    {
        data = null;
        if (!HasSave()) return false;
        try
        {
            string raw = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(raw)) return RejectCorrupt();
            SaveData parsed = JsonUtility.FromJson<SaveData>(raw);
            if (!IsValid(parsed)) return RejectCorrupt();
            data = parsed;
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[RunSaveService] Corrupt save rejected (" + e.Message + ")");
            return RejectCorrupt();
        }
    }

    public void Delete()
    {
        try
        {
            if (HasSave()) File.Delete(filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[RunSaveService] Delete failed: " + e.Message);
        }
    }

    bool RejectCorrupt()
    {
        Delete();
        return false;
    }

    static bool IsValid(SaveData d)
    {
        if (d == null) return false;
        if (d.version != SaveData.CurrentVersion) return false;
        if (d.floor < 1) return false;
        if (d.clearedRooms < 0 || d.clearedRooms >= d.floor) return false;
        if (d.enemyBudget <= 0f) return false;
        if (d.enemyBudgetGrowth <= 0f) return false;
        if (d.enemyStatGrowth <= 0f) return false;
        return true;
    }
}
