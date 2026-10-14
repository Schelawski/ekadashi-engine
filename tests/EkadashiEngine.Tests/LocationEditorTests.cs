using EkadashiEngine.Core;
using EkadashiEngine.Desktop.ViewModels;

namespace EkadashiEngine.Tests;

public sealed class LocationEditorTests
{
    [Fact]
    public void EditorPreservesCoordinatesAndIanaZoneAcrossCatalogLoads()
    {
        var editor = new LocationEditorViewModel();
        foreach (var location in LocationCatalog.DefaultLocations)
        {
            editor.Load(location);
            Assert.True(editor.IsValid);
            Assert.Equal(location, editor.Location);
        }
        editor.DisplayName = "  Own location  "; editor.Country = " XX ";
        editor.Latitude = -13.8333m; editor.Longitude = -171.75m; editor.TimeZoneId = " Pacific/Apia ";
        Assert.Equal(new Location("Own location", "XX", -13.8333, -171.75, "Pacific/Apia"), editor.Location);
    }

    [Theory]
    [InlineData("missingLatitude")]
    [InlineData("invalidLatitude")]
    [InlineData("invalidLongitude")]
    [InlineData("windowsZone")]
    [InlineData("unknownZone")]
    [InlineData("missingName")]
    [InlineData("missingCountry")]
    public void InvalidDraftDoesNotConstructACalculableLocation(string scenario)
    {
        var editor = new LocationEditorViewModel();
        switch (scenario)
        {
            case "missingLatitude": editor.Latitude = null; break;
            case "invalidLatitude": editor.Latitude = 91; break;
            case "invalidLongitude": editor.Longitude = -181; break;
            case "windowsZone": editor.TimeZoneId = "W. Europe Standard Time"; break;
            case "unknownZone": editor.TimeZoneId = "Unknown/Zone"; break;
            case "missingName": editor.DisplayName = " "; break;
            case "missingCountry": editor.Country = ""; break;
        }
        Assert.False(editor.IsValid);
        Assert.Null(editor.Location);
        Assert.NotEmpty(editor.ValidationMessage);
    }
}
