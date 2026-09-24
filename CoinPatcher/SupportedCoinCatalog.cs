using System.Collections.Immutable;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;

namespace CoinPatcher;

public sealed record SupportedCoin(
    string DisplayName,
    ModKey ModKey,
    uint LocalFormId,
    string EditorId,
    Func<Settings, CoinSettings> SelectSettings)
{
    public FormKey FormKey => ModKey.MakeFormKey(LocalFormId);
}

public static class SupportedCoinCatalog
{
    public static readonly SupportedCoin VanillaGold = new(
        "Vanilla Gold",
        ModKey.FromFileName("Skyrim.esm"),
        0x0000000F,
        "Gold001",
        settings => settings.VanillaCoin);

    public static readonly ImmutableArray<SupportedCoin> Coins =
        [VanillaGold];
}
