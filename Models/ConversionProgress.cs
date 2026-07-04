namespace AudioConverter;

internal sealed record ConversionProgress(
    string InputPath,
    double CurrentSeconds,
    double? TotalSeconds,
    double Fraction);
