using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley.Monsters;
using StardewValley;
using SpaceShared.APIs;
using System;
using System.IO;
//using ProducerFrameworkMod.Controllers;
//using ProducerFrameworkMod.ContentPack;
using ProducerFrameworkMod.Api;
using HarmonyLib;

// REMEMBER THAT THERE'S TWO ADDITIONAL PARTS OF THIS MOD [MFM] and [CP].
// REMEMBER TO ALWAYS DELETE THE PMF.dll from the file.


namespace HotChocolateCoffeeAlternative
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        /*********
        ** Fields
        *********/
        /// <summary>The Json Assets mod API.</summary>
        private IJsonAssetsApi JsonAssets;

        /// <summary>The Wear More Rings mod API.</summary>
        private IMoreRingsApi WearMoreRings;

        private IProducerFrameworkModApi PMFApi;


        /// <summary>The item ID for the Ring of Wide Nets.</summary>
        public string RingHotCocoa => this.JsonAssets.GetObjectId("Hot Cocoa Ring");
        public string HotChoc => this.JsonAssets.GetObjectId("Hot Chocolate");
        public string HotGoo => this.JsonAssets.GetObjectId("Hot Chocolate Goo");
        public string CocoaBean => this.JsonAssets.GetObjectId("Cocoa Bean");


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.World.LocationListChanged += World_LocationListChanged;
            helper.Events.World.NpcListChanged += World_NpcListChanged;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
        }
        internal class ObjectPatches
        {
            private static IMonitor Monitor;

            // call this method from your Entry class
            internal static void Initialize(IMonitor monitor)
            {
                Monitor = monitor;
            }

            // patches need to be static!
            internal static bool performObjectDropInAction_Prefix(StardewValley.Object __instance, Item dropInItem, bool probe, Farmer who, ref bool __result)
            {
                try
                {
                    if (__instance.isTemporarilyInvisible || !(dropInItem is StardewValley.Object))
                    {
                        __result = false;
                        return false;
                    }
                    StardewValley.Object object1 = dropInItem as StardewValley.Object;
                    if (__instance.bigCraftable.Value && !probe && object1 != null && __instance.heldObject.Value == null)
                        __instance.scale.X = 5f;
                    if (probe && __instance.MinutesUntilReady > 0)
                    {
                        __result = false;
                        return false;
                    }
                    if (__instance.name.Equals("Seed Maker"))
                    {
                        if (object1 != null && object1.name.Equals("Cocoa Bean"))
                        {
                            __result = false;
                            return false; // don't run original logic
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Failed in {nameof(performObjectDropInAction_Prefix)}:\n{ex}", LogLevel.Error);
                    return true; // run original logic
                }
            }
        }
        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            this.PMFApi.AddContentPack(Path.Combine(Helper.DirectoryPath, "assets", "PMF"));
        }

        /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // get Wear More Rings API if present
            this.WearMoreRings = this.Helper.ModRegistry.GetApi<IMoreRingsApi>("bcmpinc.WearMoreRings");

            this.PMFApi = this.Helper.ModRegistry.GetApi<IProducerFrameworkModApi>("Digus.ProducerFrameworkMod");

            // register rings with Json Assets
            this.JsonAssets = this.Helper.ModRegistry.GetApi<IJsonAssetsApi>("spacechase0.JsonAssets");
            if (this.JsonAssets != null)
            {
                this.JsonAssets.LoadAssets(Path.Combine(this.Helper.DirectoryPath, "assets", "json-assets"), this.Helper.Translation);
                this.Monitor.Log("HCCA json-assets loaded.", LogLevel.Info);
            }
            else
                this.Monitor.Log("Couldn't get the Json Assets API, so the new items won't be available.", LogLevel.Error);

            var harmony = new Harmony(this.ModManifest.UniqueID);

            ObjectPatches.Initialize(this.Monitor);

            // example patch, you'll need to edit this for your patch
            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.performObjectDropInAction)),
               prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.performObjectDropInAction_Prefix))
            );


        }
        private void World_NpcListChanged(object sender, NpcListChangedEventArgs e)
        {
            if (this.HasRingEquipped(this.RingHotCocoa))
            {
                foreach (NPC m in e.Added)
                {
                    if (m is Monster)
                    {
                        Monster mon = m as Monster;
                        if (Game1.random.NextDouble() < 0.25)
                        {
                            mon.objectsToDrop.Add(this.HotChoc);
                        }
                        else if (Game1.random.NextDouble() < 0.1 * CountRingsEquipped(this.RingHotCocoa))
                        {
                            mon.objectsToDrop.Add(this.HotGoo);
                        }
                    }
                }
            }
            foreach (NPC m in e.Added)
            {
                if (m is Monster)
                {
                    Monster mon = m as Monster;
                    if (mon is DustSpirit)
                    {
                        if (Game1.random.NextDouble() < 0.01)
                        {
                            mon.objectsToDrop.Add(this.CocoaBean);
                        }

                    }
                }
            }
        }

        private void World_LocationListChanged(object sender, LocationListChangedEventArgs e)
        {
            if (this.HasRingEquipped(this.RingHotCocoa))
            {

                foreach (GameLocation l in e.Added)
                {
                    foreach (NPC m in l.characters)
                    {
                        if (m is Monster)
                        {
                            Monster mon = m as Monster;
                            if (Game1.random.NextDouble() < 0.25)
                            {
                                mon.objectsToDrop.Add(this.HotChoc);
                            }
                            else if (Game1.random.NextDouble() < 0.1 * CountRingsEquipped(this.RingHotCocoa))
                            {
                                mon.objectsToDrop.Add(this.HotGoo);
                            }
                        }
                    }
                }
            }
            foreach (GameLocation l in e.Added)
            {
                foreach (NPC m in l.characters)
                {
                    if (m is Monster)
                    {
                        Monster mon = m as Monster;
                        if (mon is DustSpirit)
                        {
                            if (Game1.random.NextDouble() < 0.01)
                            {
                                mon.objectsToDrop.Add(this.CocoaBean);
                            }

                        }
                    }
                }
            }
        }


        /// <summary>Get whether the player has any ring with the given ID equipped.</summary>
        /// <param name="id">The ring ID to match.</param>
        public bool HasRingEquipped(string id)
        {
            return this.CountRingsEquipped(id) > 0;
        }

        /// <summary>Count the number of rings with the given ID equipped by the player.</summary>
        /// <param name="id">The ring ID to match.</param>
        public int CountRingsEquipped(string id)
        {
            int count =
                (Game1.player.leftRing.Value?.GetEffectsOfRingMultiplier(id) ?? 0)
                + (Game1.player.rightRing.Value?.GetEffectsOfRingMultiplier(id) ?? 0);

            if (this.WearMoreRings != null)
                count = Math.Max(count, this.WearMoreRings.CountEquippedRings(Game1.player, Int32.Parse(id)));

            return count;
        }
    }
}