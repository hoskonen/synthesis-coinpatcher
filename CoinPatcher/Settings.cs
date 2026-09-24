using System.ComponentModel.DataAnnotations;
using Mutagen.Bethesda.Synthesis.Settings;

namespace CoinPatcher;

public sealed class Settings
{
    [SynthesisSettingName("Vanilla Coin")]
    public VanillaCoinSettings VanillaCoin { get; set; } = new();

    [SynthesisSettingName("C.O.I.N.")]
    public CoinModSettings Coin { get; set; } = new();
}

public sealed class VanillaCoinSettings
{
    public string Name { get; set; } = "Gold";

    [Range(0.0, float.MaxValue)]
    public float Weight { get; set; } = 0.01f;
}

public sealed class CoinModSettings
{
    [SynthesisSettingName("Ancient Nord Drakr Weight")]
    [Range(0.0, float.MaxValue)]
    public float AncientNordDrakrWeight { get; set; } = 0.03f;

    [SynthesisSettingName("Ancient Falmer Mallari Weight")]
    [Range(0.0, float.MaxValue)]
    public float AncientFalmerMallariWeight { get; set; } = 0.02f;

    [SynthesisSettingName("Ayleid Mala Weight")]
    [Range(0.0, float.MaxValue)]
    public float AyleidMalaWeight { get; set; } = 0.02f;

    [SynthesisSettingName("Dwarven Nchuark Weight")]
    [Range(0.0, float.MaxValue)]
    public float DwarvenNchuarkWeight { get; set; } = 0.03f;

    [SynthesisSettingName("Gibber Weight")]
    [Range(0.0, float.MaxValue)]
    public float GibberWeight { get; set; } = 0.01f;
}
