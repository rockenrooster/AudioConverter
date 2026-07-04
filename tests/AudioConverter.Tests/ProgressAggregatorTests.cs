using AudioConverter;

namespace AudioConverter.Tests;

public sealed class ProgressAggregatorTests
{
    [Fact]
    public void InFlightProgressUsesWeights()
    {
        var progress = new Dictionary<int, double> { [1] = 0.5 };
        var weights = new Dictionary<int, double> { [1] = 40 };

        double inFlight = ProgressAggregator.GetInFlightWeight(progress, weights);

        Assert.Equal(20, inFlight);
        Assert.Equal(20, ProgressAggregator.GetPercentage(100, 0, inFlight));
    }

    [Fact]
    public void PercentageIncludesFinishedWeightAndCapsAtComplete()
    {
        Assert.Equal(75, ProgressAggregator.GetPercentage(100, 50, 25));
        Assert.Equal(100, ProgressAggregator.GetPercentage(100, 90, 50));
    }

    [Fact]
    public void UnknownTotalReportsZero()
    {
        Assert.Equal(0, ProgressAggregator.GetPercentage(0, 10, 10));
    }
}
