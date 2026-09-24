using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.Settings;

namespace CoinPatcher;

public static class CoinPatcher
{
    public static void Run(
        IPatcherState<ISkyrimMod, ISkyrimModGetter> state,
        Settings settings,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(output);

        ValidateSettings(settings);

        int coinsExamined = 0;
        int coinsPatched = 0;
        int alreadyCorrect = 0;
        int failures = 0;
        var resolvedCoins =
            new List<(
                SupportedCoin Metadata,
                IMiscItemGetter Winner,
                string CategoryDisplayName)>();

        foreach (SupportedCoin coin in SupportedCoinCatalog.Coins)
        {
            coinsExamined++;
            CoinSettings coinSettings = coin.SelectSettings(settings);
            string categoryDisplayName = GetCategoryDisplayName(
                settings,
                coinSettings,
                coin.DisplayName);

            if (!state.LinkCache.TryResolve<IMiscItemGetter>(
                    coin.FormKey,
                    out var winningCoin,
                    ResolveTarget.Winner))
            {
                failures++;
                output.WriteLine(categoryDisplayName);
                output.WriteLine(
                    $"  ERROR: Could not resolve MISC {FormatRecord(coin)} " +
                    $"(local FormID {coin.LocalFormId:X8}, diagnostic EditorID " +
                    $"'{coin.EditorId}') as a winning record.");
                output.WriteLine();
                continue;
            }

            if (winningCoin.IsDeleted)
            {
                failures++;
                output.WriteLine(categoryDisplayName);
                output.WriteLine(
                    $"  ERROR: Winning MISC {FormatRecord(coin)} is deleted " +
                    $"(diagnostic EditorID '{coin.EditorId}').");
                output.WriteLine();
                continue;
            }

            resolvedCoins.Add((coin, winningCoin, categoryDisplayName));
        }

        if (failures != 0)
        {
            WriteSummary(
                output,
                coinsExamined,
                coinsPatched,
                alreadyCorrect,
                failures);
            throw new InvalidOperationException(
                "Coin Patcher could not resolve every known coin. " +
                "No patch was produced.");
        }

        foreach ((
                     SupportedCoin metadata,
                     IMiscItemGetter winningCoin,
                     string categoryDisplayName) in
                 resolvedCoins)
        {
            CoinSettings coinSettings = metadata.SelectSettings(settings);
            string? winningName = winningCoin.Name?.String;
            bool changeName = !string.Equals(
                winningName,
                coinSettings.Name,
                StringComparison.Ordinal);
            bool changeWeight = winningCoin.Weight != coinSettings.Weight;

            if (!changeName && !changeWeight)
            {
                alreadyCorrect++;
                output.WriteLine(categoryDisplayName);
                output.WriteLine("  Already correct");
                output.WriteLine();
                continue;
            }

            MiscItem overrideCoin =
                state.PatchMod.MiscItems.GetOrAddAsOverride(winningCoin);

            output.WriteLine(categoryDisplayName);

            if (changeName)
            {
                overrideCoin.Name = coinSettings.Name;
                output.WriteLine(
                    $"  Name:   {FormatName(winningName)} -> " +
                    $"{FormatName(coinSettings.Name)}");
            }

            if (changeWeight)
            {
                overrideCoin.Weight = coinSettings.Weight;
                output.WriteLine(
                    $"  Weight: {FormatWeight(winningCoin.Weight)} -> " +
                    $"{FormatWeight(coinSettings.Weight)}");
            }

            output.WriteLine();
            coinsPatched++;
        }

        WriteSummary(
            output,
            coinsExamined,
            coinsPatched,
            alreadyCorrect,
            failures);
    }

    private static void ValidateSettings(Settings settings)
    {
        foreach (SupportedCoin coin in SupportedCoinCatalog.Coins)
        {
            CoinSettings coinSettings = coin.SelectSettings(settings);
            if (coinSettings.Weight < 0)
            {
                throw new ValidationException(
                    $"{coin.DisplayName} weight must not be negative.");
            }
        }
    }

    private static void WriteSummary(
        TextWriter output,
        int coinsExamined,
        int coinsPatched,
        int alreadyCorrect,
        int failures)
    {
        output.WriteLine($"""
            Summary
              Coins examined: {coinsExamined}
              Coins patched: {coinsPatched}
              Already correct: {alreadyCorrect}
              Failures: {failures}
            """);
    }

    private static string FormatName(string? name) =>
        name is null ? "<none>" : name;

    private static string FormatRecord(SupportedCoin coin) =>
        $"{coin.ModKey.FileName.String} | {coin.LocalFormId:X8}";

    private static string GetCategoryDisplayName(
        Settings settings,
        CoinSettings coinSettings,
        string fallback)
    {
        var settingsProperty = typeof(Settings)
            .GetProperties()
            .FirstOrDefault(property =>
                property.PropertyType == typeof(CoinSettings) &&
                ReferenceEquals(property.GetValue(settings), coinSettings));
        var displayNameAttribute = settingsProperty?.CustomAttributes
            .FirstOrDefault(attribute =>
                attribute.AttributeType == typeof(SynthesisSettingName));

        return displayNameAttribute?.ConstructorArguments.FirstOrDefault().Value
            as string ?? fallback;
    }

    private static string FormatWeight(float weight) =>
        weight.ToString("0.00######", CultureInfo.InvariantCulture);
}
