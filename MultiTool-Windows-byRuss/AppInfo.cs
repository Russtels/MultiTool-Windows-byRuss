// AppInfo.cs
using System.Text.Json.Serialization;

public class AppInfo
{
    // Las propiedades ahora son "nullable" (pueden ser nulas) para evitar advertencias.
    [JsonPropertyName("Category")]
    public string? Category { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("DownloadUrl")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("SilentArgs")]
    public string? SilentArgs { get; set; }
}