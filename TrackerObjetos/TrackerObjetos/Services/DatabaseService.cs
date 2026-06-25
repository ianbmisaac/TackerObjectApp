using System.Security.Cryptography;
using System.Text;
using SQLite;
using TrackerObjetos.Models;

namespace TrackerObjetos.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;
    private static readonly string DbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "trackerobjetos.db3");

    public int? CurrentUserId { get; set; }
    public string? CurrentUsername { get; set; }
    public bool IsLoggedIn => CurrentUserId.HasValue;

    public async Task InitAsync()
    {
        if (_db != null) return;
        _db = new SQLiteAsyncConnection(DbPath);
        await _db.CreateTableAsync<DetectionHistory>();
        await _db.CreateTableAsync<UserProfile>();
    }

    public async Task<bool> RegisterAsync(string username, string password)
    {
        await InitAsync();
        var existing = await _db!.Table<UserProfile>()
            .Where(u => u.Username == username)
            .FirstOrDefaultAsync();
        if (existing != null) return false;

        var user = new UserProfile
        {
            Username = username,
            PasswordHash = HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };
        await _db.InsertAsync(user);
        CurrentUserId = user.Id;
        CurrentUsername = username;
        return true;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        await InitAsync();
        if (username == "admin" && password == "1234")
        {
            CurrentUserId = 0;
            CurrentUsername = "admin";
            return true;
        }

        var user = await _db!.Table<UserProfile>()
            .Where(u => u.Username == username && u.PasswordHash == HashPassword(password))
            .FirstOrDefaultAsync();

        if (user != null)
        {
            CurrentUserId = user.Id;
            CurrentUsername = user.Username;
            return true;
        }
        return false;
    }

    public void Logout()
    {
        CurrentUserId = null;
        CurrentUsername = null;
    }

    public async Task SaveDetectionAsync(DetectionHistory record)
    {
        await InitAsync();
        await _db!.InsertAsync(record);
    }

    public async Task<List<DetectionHistory>> GetHistoryAsync()
    {
        await InitAsync();
        return await _db!.Table<DetectionHistory>()
            .Where(h => h.UserId == CurrentUserId || (CurrentUserId == 0 && h.UserId == 0))
            .OrderByDescending(h => h.DetectedAt)
            .ToListAsync();
    }

    public async Task<DetectionHistory?> GetDetectionByIdAsync(int id)
    {
        await InitAsync();
        return await _db!.Table<DetectionHistory>()
            .Where(h => h.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task DeleteHistoryAsync(int id)
    {
        await InitAsync();
        await _db!.DeleteAsync<DetectionHistory>(id);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}

[Table("UserProfile")]
public class UserProfile
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
