﻿using AnythingAnywhere.Framework;
using AnythingAnywhere.Framework.External.CustomBush;
using AnythingAnywhere.Framework.Patches;
using Common.Managers;
using Common.Utilities;
using Common.Utilities.Options;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnythingAnywhere
{
    public class ModEntry : Mod
    {
        public static IModHelper ModHelper { get; private set; } = null!;
        public static IMonitor ModMonitor { get; private set; } = null!;
        public static ModConfig Config { get; private set; } = null!;
        public static Multiplayer? Multiplayer { get; private set; }
        public static ICustomBushApi? CustomBushApi { get; private set; }

        public override void Entry(IModHelper helper)
        {
            // Setup the monitor, helper, config and multiplayer
            ModMonitor = Monitor;
            ModHelper = helper;
            Config = Helper.ReadConfig<ModConfig>();
            Multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

            // Initialize ConfigManager
            ConfigManager.Init(ModManifest, Config, ModHelper, ModMonitor, true);

            // Harmony Patches
            new BuildingPatches().Apply();
            new FarmingPatches().Apply();
            new PlacementPatches().Apply();
            new CabinAndHousePatches().Apply();
            new MiscPatches().Apply();

            // Add debug commands
            helper.ConsoleCommands.Add("aa_remove_objects", "Removes all objects of a specified ID at a specified location.\n\nUsage: aa_remove_objects [LOCATION] [OBJECT_ID]", this.DebugRemoveObjects);
            helper.ConsoleCommands.Add("aa_remove_furniture", "Removes all furniture of a specified ID at a specified location.\n\nUsage: aa_remove_furniture [LOCATION] [FURNITURE_ID]", this.DebugRemoveFurniture);
            helper.ConsoleCommands.Add("aa_active", "Lists all active locations.\n\nUsage: aa_active", this.DebugListActiveLocations);

            // Hook into Game events
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += EventHandlers.OnSaveLoaded;
            helper.Events.World.BuildingListChanged += EventHandlers.OnBuildingListChanged;
            helper.Events.Input.ButtonsChanged += EventHandlers.OnButtonsChanged;
            helper.Events.Content.AssetRequested += EventHandlers.OnAssetRequested;
            helper.Events.GameLoop.UpdateTicked += EventHandlers.OnUpdateTicked;
            helper.Events.Player.Warped += EventHandlers.OnWarped;
            helper.Events.GameLoop.DayEnding += EventHandlers.OnDayEnding;

            // Hook into Custom events
            ButtonOptions.Click += EventHandlers.OnClick;
            ConfigUtility.ConfigChanged += EventHandlers.OnConfigChanged;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            if (Helper.ModRegistry.IsLoaded("furyx639.CustomBush"))
            {
                CustomBushApi = ApiRegistry.GetApi<ICustomBushApi>("furyx639.CustomBush");
            }

            if (Helper.ModRegistry.IsLoaded("PeacefulEnd.MultipleMiniObelisks"))
            {
                Config.MultipleMiniObelisks = true;
                ConfigUtility.SkipConfig(nameof(ModConfig.MultipleMiniObelisks));
            }

            if (Helper.ModRegistry.IsLoaded("Pathoschild.CropsAnytimeAnywhere"))
            {
                Config.EnablePlanting = false;
                ConfigUtility.SkipConfig(nameof(ModConfig.EnablePlanting));
            }

            if (!Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu")) return;

            // Register the main page
            ConfigManager.AddPageLink("Placing");
            ConfigManager.AddPageLink("Building");
            ConfigManager.AddPageLink("Farming");
            ConfigManager.AddPageLink("House");
            ConfigManager.AddPageLink("Misc");

            // Register the placing settings
            ConfigManager.AddPage("Placing");
            ConfigManager.AddButtonOption("Placing", "ResetPage", fieldId: "Placing");
            ConfigManager.AddHorizontalSeparator();
            ConfigManager.AddOption(nameof(ModConfig.EnablePlacing));
            ConfigManager.AddOption(nameof(ModConfig.EnableFreePlace));
            ConfigManager.AddOption(nameof(ModConfig.EnableRugRemovalBypass));

            // Register the build settings
            ConfigManager.AddPage("Building");
            ConfigManager.AddButtonOption("Building", "ResetPage", fieldId: "Building");
            ConfigManager.AddHorizontalSeparator();
            ConfigManager.AddOption(nameof(ModConfig.EnableBuilding));
            ConfigManager.AddOption(nameof(ModConfig.EnableBuildAnywhere));
            ConfigManager.AddOption(nameof(ModConfig.EnableInstantBuild));
            ConfigManager.AddOption(nameof(ModConfig.EnableFreeBuild));
            ConfigManager.AddOption(nameof(ModConfig.BuildMenu));
            ConfigManager.AddOption(nameof(ModConfig.WizardBuildMenu));
            ConfigManager.AddOption(nameof(ModConfig.BuildModifier));
            ConfigManager.AddOption(nameof(ModConfig.EnableGreenhouse));
            ConfigManager.AddOption(nameof(ModConfig.RemoveBuildConditions));
            ConfigManager.AddOption(nameof(ModConfig.EnableBuildingIndoors));
            ConfigManager.AddOption(nameof(ModConfig.BypassMagicInk));
            ConfigManager.AddHorizontalSeparator();
            ConfigManager.AddButtonOption("BlacklistedLocations", renderLeft: true, fieldId: "BlacklistCurrentLocation", afterReset: afterReset);

            // Register the farming settings
            ConfigManager.AddPage("Farming");
            ConfigManager.AddButtonOption("Farming", "ResetPage", fieldId: "Farming");
            ConfigManager.AddHorizontalSeparator();
            ConfigManager.AddOption(nameof(ModConfig.EnablePlanting));
            ConfigManager.AddOption(nameof(ModConfig.DisableSeasonRestrictions));
            ConfigManager.AddOption(nameof(ModConfig.EnableDiggingAll));
            ConfigManager.AddOption(nameof(ModConfig.EnableFruitTreeTweaks));
            ConfigManager.AddOption(nameof(ModConfig.EnableWildTreeTweaks));

            // Register the Cabin and Farmhouse settings
            ConfigManager.AddPage("House");
            ConfigManager.AddButtonOption("House", "ResetPage", fieldId: "House");
            ConfigManager.AddHorizontalSeparator();
            ConfigManager.AddOption(nameof(ModConfig.DisableHardCodedWarp));
            ConfigManager.AddOption(nameof(ModConfig.UpgradeCabins));
            ConfigManager.AddOption(nameof(ModConfig.RenovateCabins));
            ConfigManager.AddOption(nameof(ModConfig.EnableFreeHouseUpgrade));
            ConfigManager.AddOption(nameof(ModConfig.EnableFreeRenovations));

            // Register the Misc settings
            ConfigManager.AddPage("Misc");
            ConfigManager.AddButtonOption("Misc", "ResetPage", fieldId: "Misc");
            ConfigManager.AddHorizontalSeparator();
            ConfigManager.AddOption(nameof(ModConfig.EnableCaskFunctionality));
            ConfigManager.AddOption(nameof(ModConfig.EnableFreeCommunityUpgrade));
            ConfigManager.AddOption(nameof(ModConfig.EnableJukeboxFunctionality));
            ConfigManager.AddOption(nameof(ModConfig.EnableGoldClockAnywhere));
            ConfigManager.AddOption(nameof(ModConfig.MultipleMiniObelisks));

            
        }

        private static readonly Action afterReset = () => EventHandlers.ResetBlacklist();

        private void DebugRemoveFurniture(string command, string[] args)
        {
            if (args.Length <= 1)
            {
                Monitor.Log("Missing required arguments: [LOCATION] [FURNITURE_ID]", LogLevel.Warn);
                return;
            }

            // check context
            if (!Context.IsWorldReady)
            {
                ModMonitor.Log("You need to load a save to use this command.", LogLevel.Error);
                return;
            }

            // get target location
            var location = Game1.locations.FirstOrDefault(p => p.Name?.Equals(args[0], StringComparison.OrdinalIgnoreCase) == true);
            if (location == null && args[0] == "current")
            {
                location = Game1.currentLocation;
            }
            if (location == null)
            {
                string[] locationNames = (from loc in Game1.locations where !string.IsNullOrWhiteSpace(loc.Name) orderby loc.Name select loc.Name).ToArray();
                ModMonitor.Log($"Could not find a location with that name. Must be one of [{string.Join(", ", locationNames)}].", LogLevel.Error);
                return;
            }

            // remove objects
            int removed = 0;
            foreach (var pair in location.furniture.ToArray())
            {
                if (pair.QualifiedItemId != args[1]) continue;
                location.furniture.Remove(pair);
                removed++;
            }

            ModMonitor.Log($"Command removed {removed} furniture objects at {location.NameOrUniqueName}", LogLevel.Info);
        }

        private void DebugRemoveObjects(string command, string[] args)
        {
            if (args.Length <= 1)
            {
                Monitor.Log("Missing required arguments: [LOCATION] [OBJECT_ID]", LogLevel.Warn);
                return;
            }

            // check context
            if (!Context.IsWorldReady)
            {
                ModMonitor.Log("You need to load a save to use this command.", LogLevel.Error);
                return;
            }

            // get target location
            var location = Game1.locations.FirstOrDefault(p => p.Name?.Equals(args[0], StringComparison.OrdinalIgnoreCase) == true);
            if (location == null && args[0] == "current")
            {
                location = Game1.currentLocation;
            }
            if (location == null)
            {
                string[] locationNames = (from loc in Game1.locations where !string.IsNullOrWhiteSpace(loc.Name) orderby loc.Name select loc.Name).ToArray();
                ModMonitor.Log($"Could not find a location with that name. Must be one of [{string.Join(", ", locationNames)}].", LogLevel.Error);
                return;
            }

            // remove objects
            int removed = 0;
            foreach ((Vector2 tile, var obj) in location.Objects.Pairs.ToArray())
            {
                if (obj.QualifiedItemId != args[1]) continue;
                location.Objects.Remove(tile);
                removed++;
            }

            ModMonitor.Log($"Command removed {removed} objects at {location.NameOrUniqueName}", LogLevel.Info);
        }

        private void DebugListActiveLocations(string command, string[] args)
        {
            if (args.Length > 0)
            {
                Monitor.Log("This command does not take any arguments", LogLevel.Warn);
                return;
            }

            if (!Context.IsWorldReady)
            {
                ModMonitor.Log("You need to load a save to use this command.", LogLevel.Error);
                return;
            }

            List<string> activeLocations = [];
            activeLocations.AddRange(from location in Game1.locations where location.isAlwaysActive.Value select location.Name);

            // Print out the comma-separated list of active locations
            string activeLocationsStr = string.Join(", ", activeLocations);
            ModMonitor.Log($"Active locations: {activeLocationsStr}", LogLevel.Info);
        }
    }
}