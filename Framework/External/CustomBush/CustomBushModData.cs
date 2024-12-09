using StardewValley.TerrainFeatures;
using System.Linq;

namespace AnythingAnywhere.Framework.External.CustomBush
{
    public static class CustomBushModData
    {
        private const string ModId = "furyx639.CustomBush";
        private const string ModDataId = ModId + "/Id";

        public static Bush AddBushModData(Bush bush, SObject __instance)
        {
            if (ModEntry.CustomBushApi == null || !ModEntry.ModHelper.ModRegistry.IsLoaded("furyx639.CustomBush"))
            {
                return bush;
            }

            System.Collections.Generic.IEnumerable<(string Id, ICustomBush Data)> customBushData = ModEntry.CustomBushApi.GetData();
            if (customBushData.All(item => item.Id != __instance.QualifiedItemId))
            {
                return bush;
            }

            bush.modData[ModDataId] = __instance.QualifiedItemId;
            bush.setUpSourceRect();

            return bush;
        }
    }
}