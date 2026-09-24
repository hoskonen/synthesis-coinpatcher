using System.ComponentModel.DataAnnotations;
using Mutagen.Bethesda.Synthesis.Settings;

namespace CoinPatcher;

public sealed class Settings
{
    [SynthesisSettingName("Vanilla Coin")]
    public CoinSettings VanillaCoin { get; set; } = new();
}

public sealed class CoinSettings
{
    public string Name { get; set; } = "Gold";

    [Range(0.0, float.MaxValue)]
    public float Weight { get; set; } = 0.01f;
}
