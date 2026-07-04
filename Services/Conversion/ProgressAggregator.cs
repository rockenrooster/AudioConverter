namespace AudioConverter;

internal static class ProgressAggregator
{
    public static double GetInFlightWeight(
        IEnumerable<KeyValuePair<int, double>> fileProgress,
        IReadOnlyDictionary<int, double> progressWeights)
    {
        double inFlightWeight = 0;
        foreach (var progress in fileProgress)
        {
            double weight = progressWeights.TryGetValue(progress.Key, out var value) ? value : 1;
            inFlightWeight += Math.Max(1, weight) * Math.Clamp(progress.Value, 0, 1);
        }

        return inFlightWeight;
    }

    public static int GetPercentage(
        double totalWeight,
        double finishedWeight,
        double inFlightWeight)
    {
        if (totalWeight <= 0)
            return 0;

        double completed = Math.Min(totalWeight, Math.Max(0, finishedWeight) + Math.Max(0, inFlightWeight));
        return Math.Clamp((int)Math.Round(completed * 100.0 / totalWeight), 0, 100);
    }
}
