using System.Text.Json.Serialization;

namespace IHateDPI.Launcher.Models;

public enum EngineType
{
    IHateDPI,
    GoodbyeDPI
}

public sealed class AppConfig
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EngineType SelectedEngine { get; set; } = EngineType.IHateDPI;

    [JsonPropertyName("selectedGoodbyeDpiScript")]
    public string? SelectedGoodbyeDpiScript { get; set; }
}
