using System.Collections.Immutable;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;

namespace CoinPatcher;

public sealed record SupportedCoinRecord(
    uint LocalFormId,
    string EditorId)
{
    public FormKey GetFormKey(ModKey modKey) =>
        modKey.MakeFormKey(LocalFormId);
}

public sealed record SupportedCoinCategory(
    string DisplayName,
    ImmutableArray<SupportedCoinRecord> Records,
    Func<Settings, float> SelectWeight,
    Func<Settings, string>? SelectName = null);

public sealed record SupportedCoinPlugin(
    string DisplayName,
    ModKey ModKey,
    bool Required,
    ImmutableArray<SupportedCoinCategory> Categories);

public static class SupportedCoinCatalog
{
    public static readonly SupportedCoinPlugin Skyrim = new(
        "Skyrim",
        ModKey.FromFileName("Skyrim.esm"),
        Required: true,
        [
            new SupportedCoinCategory(
                "Vanilla Coin",
                [new SupportedCoinRecord(0x0000000F, "Gold001")],
                settings => settings.VanillaCoin.Weight,
                settings => settings.VanillaCoin.Name),
        ]);

    public static readonly SupportedCoinPlugin Coin = new(
        "C.O.I.N.",
        ModKey.FromFileName("C.O.I.N.esp"),
        Required: false,
        [
            new SupportedCoinCategory(
                "Ancient Nord Drakr",
                [
                    new SupportedCoinRecord(0x00DE5012, "DES_DrakrDragon"),
                    new SupportedCoinRecord(0x00DE5013, "DES_DrakrMoth"),
                    new SupportedCoinRecord(0x00DE5014, "DES_DrakrOwl"),
                    new SupportedCoinRecord(0x00DE5015, "DES_DrakrWhale"),
                ],
                settings => settings.Coin.AncientNordDrakrWeight),
            new SupportedCoinCategory(
                "Ancient Falmer Mallari",
                [new SupportedCoinRecord(0x00DE5020, "DES_Mallari")],
                settings => settings.Coin.AncientFalmerMallariWeight),
            new SupportedCoinCategory(
                "Ayleid Mala",
                [new SupportedCoinRecord(0x00DE5019, "DES_Mala")],
                settings => settings.Coin.AyleidMalaWeight),
            new SupportedCoinCategory(
                "Dwarven Nchuark",
                [new SupportedCoinRecord(0x00DE5022, "DES_Nchuark")],
                settings => settings.Coin.DwarvenNchuarkWeight),
            new SupportedCoinCategory(
                "Gibber",
                [
                    new SupportedCoinRecord(0x00DE5017, "DES_GibberBack"),
                    new SupportedCoinRecord(0x00DE5018, "DES_GibberFront"),
                ],
                settings => settings.Coin.GibberWeight),
        ]);

    public static readonly ImmutableArray<SupportedCoinPlugin> Plugins =
        [Skyrim, Coin];
}
