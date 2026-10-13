namespace EkadashiEngine.Core.Astronomy;

public enum SunriseStatus
{
    Available,
    NoSunriseOnLocalDate,
    SkippedLocalDate
}

public sealed record SunriseResult(
    DateOnly LocalDate,
    SunriseDefinition Definition,
    SunriseStatus Status,
    DateTimeOffset? SunriseUtc,
    DateTimeOffset? SunriseLocal);
