using SObject = StardewValley.Object;
using StardewValley;
using StardewValley.GameData.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;

namespace AutomaticTodoList;

internal static class GameExtensions
{
    internal static ModConfig? Config { get; set; }

    /// <summary>Get the localized display name for any location, including building interiors.</summary>
    public static string GetLocationDisplayName(this GameLocation location)
    {
        string? name = location.GetDisplayName();
        if (!string.IsNullOrEmpty(name) && name != location.Name)
            return name;

        // for building interiors, GetDisplayName() may return internal name;
        // search parent buildings and use their display name instead
        foreach (var parentLocation in Game1.locations)
        {
            if (parentLocation?.buildings is null)
                continue;

            foreach (var building in parentLocation.buildings)
            {
                if (building?.indoors?.Value == location)
                    return GetBuildingDisplayName(building);
            }
        }

        return location.Name;
    }

    /// <summary>Get the localized display name for a building.</summary>
    public static string GetBuildingDisplayName(StardewValley.Buildings.Building building)
    {
        if (building.GetData() is BuildingData data && !string.IsNullOrEmpty(data.Name))
        {
            string parsed = TokenParser.ParseText(data.Name);
            if (!string.IsNullOrEmpty(parsed))
                return parsed;
        }

        string buildingType = building.buildingType.Value;
        if (!string.IsNullOrEmpty(buildingType))
        {
            string localized = Game1.content.LoadStringReturnNullIfNotFound(
                $"Strings/Buildings:{buildingType}_name"
            );
            if (!string.IsNullOrEmpty(localized))
                return localized;
        }

        return buildingType;
    }

    public static int GetNumberOfReadyMachinesExcludingBuildings(this GameLocation location)
    {
        int num = 0;
        foreach (StardewValley.Object obj in location.objects.Values)
        {
            if (obj.IsConsideredReadyMachineForComputer())
            {
                if (obj is ItemPedestal itemPedestal)
                {
                    // skip if this "machine" is a pedestal
                    continue;
                }

                num++;
            }
        }

        return num;
    }

    public static int GetTotalCropsReadyForHarvestExcludingForagables(this GameLocation location)
    {
        bool includeFlowers = Config?.IncludeFlowers ?? false;
        int num = 0;
        foreach (TerrainFeature value in location.terrainFeatures.Values)
        {
            if (
                value is HoeDirt hoeDirt && hoeDirt.readyForHarvest() && // existing checks
                hoeDirt.crop is not null && !hoeDirt.crop.forageCrop.Value // exclude foragables
            )
            {
                if (!includeFlowers && IsFlowerCrop(hoeDirt.crop))
                    continue;

                num++;
            }
        }

        return num;
    }

    private static bool IsFlowerCrop(Crop crop)
    {
        if (crop.indexOfHarvest.Value is null)
            return false;

        Item item = ItemRegistry.Create(crop.indexOfHarvest.Value);
        return item.Category == SObject.flowersCategory;
    }

    public static int GetTotalUnwateredCropsExcludingGinger(this GameLocation location)
    {
        int num = 0;
        foreach (TerrainFeature feature in location.terrainFeatures.Values)
        {
            if (feature is HoeDirt hoeDirt &&
                hoeDirt.crop is not null &&
                !hoeDirt.crop.dead.Value && // dead crops (rotten plants) can't grow, so never prompt to water them
                hoeDirt.needsWatering() &&
                !hoeDirt.isWatered() &&
                !hoeDirt.crop.IsGinger()
            )
            {
                num++;
            }
        }

        return num;
    }

    public static bool IsGinger(this Crop crop)
    {
        return crop is not null && crop.forageCrop.Value && crop.whichForageCrop.Value == "2";
    }

    public static bool IsInPassiveFestivalLocation(this Character character, string festivalID)
    {
        return festivalID switch
        {
            "NightMarket" => character.currentLocation is BeachNightMarket,
            "DesertFestival" => character.currentLocation is DesertFestival,
            "TroutDerby" => character.currentLocation is Forest,
            "SquidFest" => character.currentLocation is Beach,
            _ => false
        };
    }
}
