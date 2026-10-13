namespace EkadashiEngine.Core.Astronomy;

/// <summary>A complete half-open lunar day, with UTC boundaries.</summary>
public sealed record TithiInterval(int TithiNumber, DateTimeOffset StartUtc, DateTimeOffset EndUtc);
