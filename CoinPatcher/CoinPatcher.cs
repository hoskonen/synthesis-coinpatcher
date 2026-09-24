using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace CoinPatcher;

public static class CoinPatcher
{
    private sealed record ResolvedCoin(
        SupportedCoinRecord Metadata,
        IMiscItemGetter Winner);

    private sealed record ResolvedCategory(
        SupportedCoinPlugin Plugin,
        SupportedCoinCategory Metadata,
        float TargetWeight,
        string? TargetName,
        IReadOnlyList<ResolvedCoin> Coins);

    private sealed record CoinChange(
        ResolvedCoin Coin,
        bool ChangeName,
        bool ChangeWeight);

    public static void Run(
        IPatcherState<ISkyrimMod, ISkyrimModGetter> state,
        Settings settings,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(output);

        int coinsExamined = 0;
        int coinsPatched = 0;
        int alreadyCorrect = 0;
        int failures = 0;
        var resolvedCategories = new List<ResolvedCategory>();

        foreach (SupportedCoinPlugin plugin in SupportedCoinCatalog.Plugins)
        {
            bool isInstalled = state.LoadOrder.ListedOrder.Any(
                listing =>
                    listing.ModKey == plugin.ModKey &&
                    listing.Mod is not null);

            if (!plugin.Required && !isInstalled)
            {
                continue;
            }

            ValidateSettings(plugin, settings);

            foreach (SupportedCoinCategory category in plugin.Categories)
            {
                var resolvedCoins = new List<ResolvedCoin>();
                var categoryErrors = new List<string>();

                foreach (SupportedCoinRecord coin in category.Records)
                {
                    coinsExamined++;
                    var formKey = coin.GetFormKey(plugin.ModKey);

                    if (!state.LinkCache.TryResolve<IMiscItemGetter>(
                            formKey,
                            out var winningCoin,
                            ResolveTarget.Winner))
                    {
                        failures++;
                        categoryErrors.Add(
                            $"Could not resolve MISC {FormatRecord(plugin, coin)} " +
                            $"(diagnostic EditorID '{coin.EditorId}') as a " +
                            "winning record.");
                        continue;
                    }

                    if (winningCoin.IsDeleted)
                    {
                        failures++;
                        categoryErrors.Add(
                            $"Winning MISC {FormatRecord(plugin, coin)} is " +
                            $"deleted (diagnostic EditorID '{coin.EditorId}').");
                        continue;
                    }

                    resolvedCoins.Add(new ResolvedCoin(coin, winningCoin));
                }

                if (categoryErrors.Count != 0)
                {
                    output.WriteLine(category.DisplayName);
                    foreach (string error in categoryErrors)
                    {
                        output.WriteLine($"  ERROR: {error}");
                    }

                    output.WriteLine();
                    continue;
                }

                resolvedCategories.Add(new ResolvedCategory(
                    plugin,
                    category,
                    category.SelectWeight(settings),
                    category.SelectName?.Invoke(settings),
                    resolvedCoins));
            }
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
                "Coin Patcher could not resolve every known coin for an " +
                "installed supported plugin. No patch was produced.");
        }

        foreach (ResolvedCategory category in resolvedCategories)
        {
            IReadOnlyList<CoinChange> changes = category.Coins
                .Select(coin => new CoinChange(
                    coin,
                    ChangeName: category.TargetName is not null &&
                        !string.Equals(
                            coin.Winner.Name?.String,
                            category.TargetName,
                            StringComparison.Ordinal),
                    ChangeWeight: coin.Winner.Weight != category.TargetWeight))
                .ToArray();
            CoinChange[] changedCoins = changes
                .Where(change => change.ChangeName || change.ChangeWeight)
                .ToArray();

            alreadyCorrect += changes.Count - changedCoins.Length;
            output.WriteLine(category.Metadata.DisplayName);
            output.WriteLine($"  Records: {category.Coins.Count}");

            if (changedCoins.Length == 0)
            {
                output.WriteLine("  Already correct");
                output.WriteLine();
                continue;
            }

            foreach (CoinChange change in changedCoins)
            {
                MiscItem overrideCoin =
                    state.PatchMod.MiscItems.GetOrAddAsOverride(
                        change.Coin.Winner);

                if (change.ChangeName)
                {
                    overrideCoin.Name = category.TargetName;
                }

                if (change.ChangeWeight)
                {
                    overrideCoin.Weight = category.TargetWeight;
                }
            }

            WriteChanges(output, category, changedCoins);
            output.WriteLine();
            coinsPatched += changedCoins.Length;
        }

        WriteSummary(
            output,
            coinsExamined,
            coinsPatched,
            alreadyCorrect,
            failures);
    }

    private static void ValidateSettings(
        SupportedCoinPlugin plugin,
        Settings settings)
    {
        foreach (SupportedCoinCategory category in plugin.Categories)
        {
            if (category.SelectWeight(settings) < 0)
            {
                throw new ValidationException(
                    $"{category.DisplayName} weight must not be negative.");
            }
        }
    }

    private static void WriteChanges(
        TextWriter output,
        ResolvedCategory category,
        IReadOnlyList<CoinChange> changes)
    {
        CoinChange[] nameChanges = changes
            .Where(change => change.ChangeName)
            .ToArray();

        foreach (CoinChange change in nameChanges)
        {
            string prefix = nameChanges.Length == 1
                ? "  Name:   "
                : $"  {change.Coin.Metadata.EditorId} Name: ";
            output.WriteLine(
                $"{prefix}{FormatName(change.Coin.Winner.Name?.String)} -> " +
                $"{FormatName(category.TargetName)}");
        }

        CoinChange[] weightChanges = changes
            .Where(change => change.ChangeWeight)
            .ToArray();

        if (weightChanges.Length == 0)
        {
            return;
        }

        bool originalWeightsDiffer = category.Coins
            .Select(coin => coin.Winner.Weight)
            .Distinct()
            .Skip(1)
            .Any();

        if (!originalWeightsDiffer)
        {
            output.WriteLine(
                $"  Weight: {FormatWeight(weightChanges[0].Coin.Winner.Weight)} " +
                $"-> {FormatWeight(category.TargetWeight)}");
            return;
        }

        foreach (CoinChange change in weightChanges)
        {
            output.WriteLine(
                $"  {change.Coin.Metadata.EditorId} Weight: " +
                $"{FormatWeight(change.Coin.Winner.Weight)} -> " +
                $"{FormatWeight(category.TargetWeight)}");
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

    private static string FormatRecord(
        SupportedCoinPlugin plugin,
        SupportedCoinRecord coin) =>
        $"{plugin.ModKey.FileName.String} | {coin.LocalFormId:X8}";

    private static string FormatWeight(float weight) =>
        weight.ToString("0.00######", CultureInfo.InvariantCulture);
}
