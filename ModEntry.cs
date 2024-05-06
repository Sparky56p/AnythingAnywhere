﻿using HarmonyLib;
using StardewValley;
using StardewValley.GameData.Buildings;
using StardewValley.TokenizableStrings;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using AnythingAnywhere.Framework.UI;
using AnythingAnywhere.Framework.Managers;
using AnythingAnywhere.Framework.Interfaces;
using AnythingAnywhere.Framework.Patches.Menus;
using AnythingAnywhere.Framework.Patches.Locations;
using AnythingAnywhere.Framework.Patches.GameLocations;
using AnythingAnywhere.Framework.Patches.StandardObjects;
using AnythingAnywhere.Framework.Patches.TerrainFeatures;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;

namespace AnythingAnywhere
{
    public class ModEntry : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static ICustomBushApi customBushApi;
        internal static ModConfig modConfig;
        internal static Multiplayer multiplayer;

        // Managers
        internal static ApiManager apiManager;

        public override void Entry(IModHelper helper)
        {
            // Setup i18n
            I18n.Init(helper.Translation);

            // Setup the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            modConfig = Helper.ReadConfig<ModConfig>();
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

            // Setup the manager
            apiManager = new ApiManager();

            // Load the Harmony patches
            var harmony = new Harmony(this.ModManifest.UniqueID);

            // Apply GameLocation patches
            new GameLocationPatch(harmony).Apply();

            // Apply Location patches
            new FarmHousePatch(harmony).Apply();

            // Apply Menu patches
            new CarpenterMenuPatch(harmony).Apply();
            new AnimalQueryMenuPatch(harmony).Apply();

            // Apply StandardObject patches
            new CaskPatch(harmony).Apply();
            new FurniturePatch(harmony).Apply();
            new MiniJukeboxPatch(harmony).Apply();
            new ObjectPatch(harmony).Apply();

            // Apply TerrainFeature patches
            new FruitTreePatch(harmony).Apply();
            new TreePatch(harmony).Apply();
            new HoeDirtPatch(harmony).Apply();


            // Add debug commands
            helper.ConsoleCommands.Add("aa_remove_objects", "Removes all objects of a specified ID at a specified location.\n\nUsage: aa_remove_objects [LOCATION] [OBJECT_ID]", this.DebugRemoveObjects);
            helper.ConsoleCommands.Add("aa_remove_furniture", "Removes all furniture of a specified ID at a specified location.\n\nUsage: aa_remove_furniture [LOCATION] [FURNITURE_ID]", this.DebugRemoveFurniture);

            // Hook into GameLoop events
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

            // Hook into Input events
            helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;

            // Hook into Content events
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            if (Helper.ModRegistry.IsLoaded("furyx639.CustomBush"))
            {
                customBushApi = apiManager.GetApi<ICustomBushApi>("furyx639.CustomBush");
            }

            if (Helper.ModRegistry.IsLoaded("PeacefulEnd.MultipleMiniObelisks"))
            {
                modConfig.MultipleMiniObelisks = true;
            }

            if (Helper.ModRegistry.IsLoaded("mouahrara.RelocateFarmAnimals"))
            {
                modConfig.EnableAnimalRelocate = false;
            }

            var configApi = apiManager.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu", false);
            if (Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu") && configApi != null)
            {
                configApi.Register(ModManifest, () => modConfig = new ModConfig(), () => Helper.WriteConfig(modConfig));

                // Register the main page
                configApi.AddPageLink(ModManifest, "PlacingPage", () => String.Concat("> ", I18n.Config_AnythingAnywhere_Placing_Title()));
                configApi.AddPageLink(ModManifest, "BuildingsPage", () => String.Concat("> ", I18n.Config_AnythingAnywhere_Building_Title()));
                configApi.AddPageLink(ModManifest, "FarmingPage", () => String.Concat("> ", I18n.Config_AnythingAnywhere_Farming_Title()));
                configApi.AddPageLink(ModManifest, "OtherPage", () => String.Concat("> ", I18n.Config_AnythingAnywhere_Other_Title()));

                // Register the placing settings
                configApi.AddPage(ModManifest, "PlacingPage", I18n.Config_AnythingAnywhere_Placing_Title);
                configApi.AddSectionTitle(ModManifest, I18n.Config_AnythingAnywhere_Placing_Title);
                AddOption(configApi, nameof(ModConfig.EnablePlacing));
                AddOption(configApi, nameof(ModConfig.EnableFreePlace));
                AddOption(configApi, nameof(ModConfig.EnableWallFurnitureIndoors));
                AddOption(configApi, nameof(ModConfig.EnableRugRemovalBypass));

                // Register the build settings
                configApi.AddPage(ModManifest, "BuildingsPage", I18n.Config_AnythingAnywhere_Building_Title);
                configApi.AddSectionTitle(ModManifest, I18n.Config_AnythingAnywhere_Building_Title);
                AddOption(configApi, nameof(ModConfig.EnableBuilding));
                AddOption(configApi, nameof(ModConfig.EnableBuildAnywhere));
                AddOption(configApi, nameof(ModConfig.EnableInstantBuild));
                AddOption(configApi, nameof(ModConfig.BuildMenu));
                AddOption(configApi, nameof(ModConfig.WizardBuildMenu));
                AddOption(configApi, nameof(ModConfig.BuildModifier));
                AddOption(configApi, nameof(ModConfig.EnableGreenhouse));
                AddOption(configApi, nameof(ModConfig.RemoveBuildConditions));
                AddOption(configApi, nameof(ModConfig.EnableBuildingIndoors));
                AddOption(configApi, nameof(ModConfig.BypassMagicInk));

                // Register the farming settings
                configApi.AddPage(ModManifest, "FarmingPage", I18n.Config_AnythingAnywhere_Farming_Title);
                configApi.AddSectionTitle(ModManifest, I18n.Config_AnythingAnywhere_Farming_Title);
                AddOption(configApi, nameof(ModConfig.EnablePlanting));
                AddOption(configApi, nameof(ModConfig.EnableDiggingAll));
                AddOption(configApi, nameof(ModConfig.EnableFruitTreeTweaks));
                AddOption(configApi, nameof(ModConfig.EnableWildTreeTweaks));

                // Register the other settings
                configApi.AddPage(ModManifest, "OtherPage", I18n.Config_AnythingAnywhere_Other_Title);
                configApi.AddSectionTitle(ModManifest, I18n.Config_AnythingAnywhere_Other_Title);
                AddOption(configApi, nameof(ModConfig.EnableAnimalRelocate));
                AddOption(configApi, nameof(ModConfig.EnableCaskFunctionality));
                AddOption(configApi, nameof(ModConfig.EnableJukeboxFunctionality));
                AddOption(configApi, nameof(ModConfig.EnableGoldClockAnywhere));
                AddOption(configApi, nameof(ModConfig.MultipleMiniObelisks));
                AddOption(configApi, nameof(ModConfig.EnableCabinsAnywhere));
            }
        }

        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsWorldReady || !modConfig.EnableBuilding)
                return;

            if (modConfig.BuildMenu.JustPressed() && modConfig.EnableBuilding)
                HandleBuildButtonPress("Robin");

            if (modConfig.WizardBuildMenu.JustPressed() && modConfig.EnableBuilding)
                HandleBuildButtonPress("Wizard");
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("Data/Buildings"))
            {
                e.Edit(
                    asset =>
                    {
                        var data = asset.AsDictionary<string, BuildingData>().Data;
                        foreach (var buildingDataKey in data.Keys.ToList()) 
                        {
                            data[buildingDataKey] = ModifyBuildingData(data[buildingDataKey], modConfig.EnableInstantBuild, modConfig.EnableGreenhouse, modConfig.RemoveBuildConditions);
                        }
                    }, AssetEditPriority.Late);

                return;
            }
        }
        private static BuildingData ModifyBuildingData(BuildingData data, bool enableInstantBuild, bool enableGreenhouse, bool removeBuildConditions)
        {
            if (enableGreenhouse && IsGreenhouse(data))
                SetGreenhouseAttributes(data);

            if (enableInstantBuild)
                SetInstantBuildAttributes(data);

            if (removeBuildConditions)
                RemoveBuildConditions(data);

            return data;
        }

        private static bool IsGreenhouse(BuildingData data)
        {
            return TokenParser.ParseText(data.Name) == Game1.content.LoadString("Strings\\Buildings:Greenhouse_Name");
        }

        private static void SetGreenhouseAttributes(BuildingData data)
        {
            // Define greenhouse materials
            List<BuildingMaterial> greenhouseMaterials = new List<BuildingMaterial>
            {
                new BuildingMaterial { ItemId = "(O)709", Amount = 100 },
                new BuildingMaterial { ItemId = "(O)338", Amount = 20 },
                new BuildingMaterial { ItemId = "(O)337", Amount = 10 },
            };

            // Set greenhouse attributes
            data.Builder = Game1.builder_robin;
            data.BuildCost = 150000;
            data.BuildDays = 3;
            data.BuildMaterials = greenhouseMaterials;
            data.BuildCondition = "PLAYER_HAS_MAIL Host ccPantry";
        }

        private static void SetInstantBuildAttributes(BuildingData data)
        {
            data.MagicalConstruction = true;
            data.BuildCost = 0;
            data.BuildDays = 0;
            data.BuildMaterials = [];
        }

        private static void RemoveBuildConditions(BuildingData data)
        {
            data.BuildCondition = "";
        }

        private void HandleBuildButtonPress(string builder)
        {
            if (Context.IsPlayerFree && Game1.activeClickableMenu == null)
            {
                modHelper.GameContent.InvalidateCache("Data/Buildings");
                ActivateBuildAnywhereMenu(builder);
            }
            else if (Game1.activeClickableMenu is BuildAnywhereMenu)
            {
                Game1.displayFarmer = true;
                ((BuildAnywhereMenu)Game1.activeClickableMenu).returnToCarpentryMenu();
                ((BuildAnywhereMenu)Game1.activeClickableMenu).exitThisMenu();
            }
        }

        private void ActivateBuildAnywhereMenu(string builder)
        {
            if (!Game1.currentLocation.IsOutdoors && !modConfig.EnableBuildingIndoors)
            {
                Game1.addHUDMessage(new HUDMessage(I18n.Message_AnythingAnywhere_NoBuildingIndoors(), HUDMessage.error_type) { timeLeft = HUDMessage.defaultTime });
                return;
            }
            bool magicInkCheck = !(Game1.getFarmer(Game1.player.UniqueMultiplayerID).hasMagicInk || modConfig.BypassMagicInk);
            if (builder == "Wizard" && magicInkCheck && !modConfig.EnableInstantBuild)
            {
                Game1.addHUDMessage(new HUDMessage(I18n.Message_AnythingAnywhere_NoMagicInk(), HUDMessage.error_type) { timeLeft = HUDMessage.defaultTime });
                return;
            }

            Game1.activeClickableMenu = new BuildAnywhereMenu(builder, Game1.player.currentLocation);
        }

        private void DebugRemoveFurniture(string command, string[] args)
        {
            if (args.Length <= 1)
            {
                Monitor.Log($"Missing required arguments: [LOCATION] [FURNITURE_ID]", LogLevel.Warn);
                return;
            }

            // check context
            if (!Context.IsWorldReady)
            {
                monitor.Log("You need to load a save to use this command.", LogLevel.Error);
                return;
            }

            // get target location
            var location = Game1.locations.FirstOrDefault(p => p.Name != null && p.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));
            if (location == null && args[0] == "current")
            {
                location = Game1.currentLocation;
            }
            if (location == null)
            {
                string[] locationNames = (from loc in Game1.locations where !string.IsNullOrWhiteSpace(loc.Name) orderby loc.Name select loc.Name).ToArray();
                monitor.Log($"Could not find a location with that name. Must be one of [{string.Join(", ", locationNames)}].", LogLevel.Error);
                return;
            }

            // remove objects
            int removed = 0;
            foreach (var pair in location.furniture.ToArray())
            {
                if (pair.QualifiedItemId == args[1])
                {
                    location.furniture.Remove(pair);
                    removed++;
                }
            }

            monitor.Log($"Command removed {removed} furniture objects at {location.NameOrUniqueName}", LogLevel.Info);
            return;
        }

        private void DebugRemoveObjects(string command, string[] args)
        {
            if (args.Length <= 1)
            {
                Monitor.Log($"Missing required arguments: [LOCATION] [OBJECT_ID]", LogLevel.Warn);
                return;
            }

            // check context
            if (!Context.IsWorldReady)
            {
                monitor.Log("You need to load a save to use this command.", LogLevel.Error);
                return;
            }

            // get target location
            var location = Game1.locations.FirstOrDefault(p => p.Name != null && p.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));
            if (location == null && args[0] == "current")
            {
                location = Game1.currentLocation;
            }
            if (location == null)
            {
                string[] locationNames = (from loc in Game1.locations where !string.IsNullOrWhiteSpace(loc.Name) orderby loc.Name select loc.Name).ToArray();
                monitor.Log($"Could not find a location with that name. Must be one of [{string.Join(", ", locationNames)}].", LogLevel.Error);
                return;
            }

            // remove objects
            int removed = 0;
            foreach ((Vector2 tile, var obj) in location.Objects.Pairs.ToArray())
            {
                if (obj.QualifiedItemId == args[1])
                {
                    location.Objects.Remove(tile);
                    removed++;
                }
            }

            monitor.Log($"Command removed {removed} objects at {location.NameOrUniqueName}", LogLevel.Info);
            return;
        }

        private void AddOption(IGenericModConfigMenuApi configApi, string name)
        {
            PropertyInfo propertyInfo = typeof(ModConfig).GetProperty(name);
            if (propertyInfo == null)
            {
                Monitor.Log($"Error: Property '{name}' not found in ModConfig.", LogLevel.Error);
                return;
            }

            Func<string> getName = () => I18n.GetByKey($"Config.{typeof(ModEntry).Namespace}.{name}.Name");
            Func<string> getDescription = () => I18n.GetByKey($"Config.{typeof(ModEntry).Namespace}.{name}.Description");

            if (getName == null || getDescription == null)
            {
                Monitor.Log($"Error: Localization keys for '{name}' not found.", LogLevel.Error);
                return;
            }

            var getterMethod = propertyInfo.GetGetMethod();
            var setterMethod = propertyInfo.GetSetMethod();

            if (getterMethod == null || setterMethod == null)
            {
                Monitor.Log($"Error: The get/set methods are null for property '{name}'.", LogLevel.Error);
                return;
            }

            var getter = Delegate.CreateDelegate(typeof(Func<>).MakeGenericType(propertyInfo.PropertyType), modConfig, getterMethod);
            var setter = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(propertyInfo.PropertyType), modConfig, setterMethod);

            switch (propertyInfo.PropertyType.Name)
            {
                case nameof(Boolean):
                    configApi.AddBoolOption(ModManifest, (Func<bool>)getter, (Action<bool>)setter, getName, getDescription);
                    break;
                case nameof(Int32):
                    configApi.AddNumberOption(ModManifest, (Func<int>)getter, (Action<int>)setter, getName, getDescription);
                    break;
                case nameof(Single):
                    configApi.AddNumberOption(ModManifest, (Func<float>)getter, (Action<float>)setter, getName, getDescription);
                    break;
                case nameof(String):
                    configApi.AddTextOption(ModManifest, (Func<string>)getter, (Action<string>)setter, getName, getDescription);
                    break;
                case nameof(SButton):
                    configApi.AddKeybind(ModManifest, (Func<SButton>)getter, (Action<SButton>)setter, getName, getDescription);
                    break;
                case nameof(KeybindList):
                    configApi.AddKeybindList(ModManifest, (Func<KeybindList>)getter, (Action<KeybindList>)setter, getName, getDescription);
                    break;
                default:
                    Monitor.Log($"Error: Unsupported property type '{propertyInfo.PropertyType.Name}' for '{name}'.", LogLevel.Error);
                    break;
            }
        }
    }
}