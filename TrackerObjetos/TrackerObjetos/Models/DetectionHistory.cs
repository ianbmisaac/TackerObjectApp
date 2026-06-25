using SQLite;

namespace TrackerObjetos.Models;

[Table("DetectionHistory")]
public class DetectionHistory
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Titulo { get; set; } = "";

    public string Descripcion { get; set; } = "";

    public byte[]? Thumbnail { get; set; }

    public string? WebTitle { get; set; }

    public string? WebExtract { get; set; }

    public DateTime DetectedAt { get; set; }
}
