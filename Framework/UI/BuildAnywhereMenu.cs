using StardewValley;
using StardewValley.GameData.Buildings;
using StardewValley.Menus;

namespace AnythingAnywhere.Framework.UI
{
    internal sealed class BuildAnywhereMenu(string builder, GameLocation? targetLocation = null) : CarpenterMenu(builder, targetLocation)
    {
        // Prevents Better Juminos from spamming errors
#pragma warning disable CS0649
        public bool magicalConstruction;

        // Check if a building is valid for a location
        public override bool IsValidBuildingForLocation(string typeId, BuildingData data, GameLocation targetLocation)
        {
            return typeId != "Cabin" || TargetLocation.Name == "Farm" || !Game1.IsMultiplayer;
        }
    }
}