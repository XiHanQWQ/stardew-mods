using SObject = StardewValley.Object;
using StardewValley;
using StardewValley.GameData.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AutomaticTodoList;

internal static class GameExtensions
{
    internal static ModConfig? Config { get; set; }

    // Cache for location display names to avoid repeated building searches
    private static readonly ConditionalWeakTable<GameLocation, string> LocationDisplayNameCache = new();

    // Map of passive festival IDs to their valid location type names.
    // Using type names instead of exact types allows for custom locations that inherit from these.
    private static readonly Dictionary<string, HashSet<string>> PassiveFestivalLocationTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NightMarket"] = new HashSet<string> { nameof(BeachNightMarket) },
        ["DesertFestival"] = new HashSet<string> { nameof(DesertFestival) },
        ["TroutDerby"] = new HashSet<string> { nameof(Forest) },
        ["SquidFest"] = new HashSet<string> { nameof(Beach) },
    };

    /// <summary>Get the localized display name for any location, including building interiors.</summary>
    public static string GetLocationDisplayName(this GameLocation location)
    {
        if (LocationDisplayNameCache.TryGetValue(location, out string? cachedName))
            return cachedName;

        string? name = location.GetDisplayName();
        string result;

        if (!string.IsNullOrEmpty(name) && name != location.Name)
        {
            result = name;
        }
        else
        {
            // for building interiors, GetDisplayName() may return internal name;
            // search parent buildings and use their display name instead
            result = location.Name;
            foreach (var parentLocation in Game1.locations)
            {
                if (parentLocation?.buildings is null)
                    continue;

                foreach (var building in parentLocation.buildings)
                {
                    if (building?.indoors?.Value == location)
                    {
                        result = GetBuildingDisplayName(building);
                        break;
                    }
                }
            }
        }

        LocationDisplayNameCache.Add(location, result);
        return result;
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
        if (character.currentLocation is null)
            return false;

        if (!PassiveFestivalLocationTypes.TryGetValue(festivalID, out var validTypes))
            return false;

        string locationTypeName = character.currentLocation.GetType().Name;
        return validTypes.Contains(locationTypeName);
    }
}
