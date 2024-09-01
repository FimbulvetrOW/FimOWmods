using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TenCrowns.AppCore;
using TenCrowns.GameCore;
using static TenCrowns.GameCore.DefaultMapScript;
using static TenCrowns.GameCore.Player;
using Mohawk.SystemCore;
using System.Reflection;
using UnityEngine;

namespace MinResourcesMod
{
    public class MinResources : ModEntryPointAdapter
    {
        public const string MY_HARMONY_ID = "Fim.MinResources.patch";
        public Harmony harmony;

        public override void Initialize(ModSettings modSettings)
        {
            if (harmony != null)
                return;
            harmony = new Harmony(MY_HARMONY_ID);
            harmony.PatchAll();

        }

        public override void Shutdown()
        {
            if (harmony == null)
                return;
            harmony.UnpatchAll(MY_HARMONY_ID);
            harmony = null;
        }
    }
    [HarmonyPatch]

    public class AddUnaffiliatedResourcesPatch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(DefaultMapScript), "WeightedShuffle", new Type[] { typeof(List<ResourceType>), typeof(int), typeof(int) })]
        public static void MinResWeightedShuffle(DefaultMapScript instance, List<ResourceType> resources, int fromIndex, int toIndex)
        {
            // its a stub so it has no initial content
            throw new NotImplementedException("It's a stub");
        }

        [HarmonyPatch(typeof(DefaultMapScript), "WeightedShuffle", new Type[] { typeof(List<ResourceType>) })]
        static bool Prefix(DefaultMapScript __instance, List<ResourceType> resources)
        {
            MinResWeightedShuffle(__instance, resources, 0, resources.Count);
            return false;
        }

        //protected virtual Dictionary<int, ResourceType> PlaceRingResourcesSpecific(TileData siteTile,
        //ResourceType resource, int minRing, int maxRing, int minResources, int maxResources, bool ignoreHeight = false,
        //IDictionary<int, ResourceType> checkTiles = null, int minDistance = int.MinValue, int maxDistance = int.MaxValue)
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(DefaultMapScript), "PlaceRingResourcesSpecific", new Type[] { typeof(TileData), typeof(ResourceType),
            typeof(int), typeof(int), typeof(int), typeof(int), typeof(bool), typeof(IDictionary<int, ResourceType>), typeof(int), typeof(int) })]
        public static Dictionary<int, ResourceType> MinResPlaceRingResourcesSpecific(DefaultMapScript instance, TileData siteTile,
        ResourceType resource, int minRing, int maxRing, int minResources, int maxResources, bool ignoreHeight,
        IDictionary<int, ResourceType> checkTiles, int minDistance, int maxDistance)
        {
            // its a stub so it has no initial content
            throw new NotImplementedException("It's a stub");
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(DefaultMapScript), "TileDistance", new Type[] { typeof(int), typeof(int) })]
        public static int MinResTileDistance(DefaultMapScript instance, int tileID, int otherTileID)
        {
            // its a stub so it has no initial content
            throw new NotImplementedException("It's a stub");
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(DefaultMapScript), "GetTile", new Type[] { typeof(int) })]
        public static TileData MinResGetTile(DefaultMapScript instance, int index)
        {
            // its a stub so it has no initial content
            throw new NotImplementedException("It's a stub");
        }

        //protected virtual TileData TileAdjacent(TileData tile, DirectionType dir)
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(DefaultMapScript), "TileAdjacent", new Type[] { typeof(TileData), typeof(DirectionType) })]
        public static TileData MinResTileAdjacent(DefaultMapScript instance, TileData tile, DirectionType dir)
        {
            // its a stub so it has no initial content
            throw new NotImplementedException("It's a stub");
        }

        public static int CountAdjacentSameResource(DefaultMapScript instance, TileData tileData)
        {
            {
                int count = 0;
                for (DirectionType eLoopDirection = 0; eLoopDirection < DirectionType.NUM_TYPES; eLoopDirection++)
                {
                    //TileData pAdjacent = instance.TileAdjacent(tileData, eLoopDirection);
                    TileData pAdjacent = MinResTileAdjacent(instance, tileData, eLoopDirection);
                    if (pAdjacent != null)
                    {
                        if (tileData.meResource == pAdjacent.meResource)
                        {
                            ++count;
                        }
                    }
                }
                return count;
            }
        }

        public class TileScore
        {
            public TileData tile;
            int score;
            public TileScore(TileData tile, int score)
            {
                this.tile = tile;
                this.score = score;
            }
            public int Score
            {
                get { return score; }
                set { score = value; }
            }
        }


        [HarmonyPatch(typeof(DefaultMapScript), "AddUnaffiliatedResources", new Type[] { })]
        static bool Prefix(DefaultMapScript __instance,
            Infos ___infos, RandomStruct ___random, Dictionary<ResourceType, int> ___resourceCounts, HashSet<int> ___unreachableTiles,
            Dictionary<int, int> ___closestCitySites, Dictionary<int, ResourceType> ___placedResources)
        {
            //#################################################
            //PART I: Reflection, here I come. Help


            //UnityEngine.Debug.Log("Are we at least getting this far?");
            if (__instance == null)
            {
                UnityEngine.Debug.Log("No DefaultMapScript instance to work with");
                return true;
            }

            PropertyInfo PI_Players = AccessTools.Property(typeof(DefaultMapScript), "Players");
            if (PI_Players == null)
            {
                UnityEngine.Debug.Log("Players null");
            }
            int iPlayerCount = ((List<PlayerType>)PI_Players.GetValue(__instance)).Count;

            PropertyInfo PI_ResourceDensity = AccessTools.Property(typeof(DefaultMapScript), "ResourceDensity");
            if (PI_ResourceDensity == null)
            {
                UnityEngine.Debug.Log("ResourceDensity null");
            }

            PropertyInfo PI_CitySiteMaxResourceDistance = AccessTools.Property(typeof(DefaultMapScript), "CitySiteMaxResourceDistance");
            if (PI_CitySiteMaxResourceDistance == null)
            {
                UnityEngine.Debug.Log("CitySiteMaxResourceDistance null");
            }
            int iCitySiteMaxResourceDistance = (int)(PI_CitySiteMaxResourceDistance.GetValue(__instance));

            //if I ever get bored enough, I could replace all these Reflections with reverse patches (https://harmony.pardeike.net/articles/reverse-patching.html)
            MethodInfo MI_GetTile = AccessTools.Method(typeof(DefaultMapScript), "GetTile", new Type[] { typeof(int), typeof(int) });
            MethodInfo MI_GetClosestCityDistance = AccessTools.Method(typeof(DefaultMapScript), "GetClosestCityDistance", new Type[] { typeof(TileData) });
            MethodInfo MI_CanPlaceResource = AccessTools.Method(typeof(DefaultMapScript), "CanPlaceResource", new Type[] { typeof(TileData), typeof(ResourceType), typeof(bool), typeof(IDictionary<int, ResourceType>), typeof(int), typeof(int) });
            MethodInfo MI_PlaceResource = AccessTools.Method(typeof(DefaultMapScript), "PlaceResource", new Type[] { typeof(TileData), typeof(ResourceType) });
            MethodInfo MI_TileAdjacent = AccessTools.Method(typeof(DefaultMapScript), "TileAdjacent", new Type[] { typeof(TileData), typeof(DirectionType) });
            if (MI_GetTile == null)
            {
                UnityEngine.Debug.Log("GetTile null");
            }
            if (MI_GetClosestCityDistance == null)
            {
                UnityEngine.Debug.Log("GetClosestCityDistance null");
            }
            if (MI_CanPlaceResource == null)
            {
                UnityEngine.Debug.Log("CanPlaceResource null");
            }
            if (MI_PlaceResource == null)
            {
                UnityEngine.Debug.Log("PlaceResource null");
            }

            System.Type tResourceCategory = typeof(DefaultMapScript).GetNestedType("ResourceCategory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (tResourceCategory == null)
            {
                UnityEngine.Debug.Log("ResourceCategory Type null");
            }

            object rcLUXURY = (tResourceCategory.GetField("LUXURY")).GetValue(__instance);
            if (rcLUXURY == null)
            {
                UnityEngine.Debug.Log("ResourceCategory Value NULL");
            }
            object rcLATE_GATHERED = (tResourceCategory.GetField("LATE_GATHERED")).GetValue(__instance);
            if (rcLATE_GATHERED == null)
            {
                UnityEngine.Debug.Log("ResourceCategory Value NULL");
            }

            MethodInfo MI_IsResourceCategory = AccessTools.Method(typeof(DefaultMapScript), "IsResourceCategory", new Type[] { typeof(ResourceType), (System.Type)(tResourceCategory) } );
            if (MI_PlaceResource == null)
            {
                UnityEngine.Debug.Log("IsResourceCategory null");
            }

            System.Type tResourceDensityType = typeof(DefaultMapScript).GetNestedType("ResourceDensityType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (tResourceCategory == null)
            {
                UnityEngine.Debug.Log("ResourceDensityType Type null");
            }

            object rdPOOR = (tResourceDensityType.GetField("POOR")).GetValue(__instance);
            if (rdPOOR == null)
            {
                UnityEngine.Debug.Log("ResourceDensityType Value POOR NULL");
            }

            object rdRICH = (tResourceDensityType.GetField("RICH")).GetValue(__instance);
            if (rdRICH == null)
            {
                UnityEngine.Debug.Log("ResourceDensityType Value RICH NULL");
            }

            int resourceDensityPercent = 100;

            if ((object)PI_ResourceDensity.GetValue(__instance) == rdPOOR)
            {
                resourceDensityPercent *= 2;
                resourceDensityPercent /= 3;
            }
            else if ((object)PI_ResourceDensity.GetValue(__instance) == rdRICH)
            {
                resourceDensityPercent *= 3;
                resourceDensityPercent /= 2;
            }



            //using (new UnityProfileScope("DefaultMapScript.AddUnaffiliatedResources"))
            {
                //###################################################
                //PART II: Try placing Luxuries on unaffiliated tiles


                List<TileData> shuffledTiles = new List<TileData>();
                List<TileData> shuffledTilesReduced = new List<TileData>();

                for (int y = 0; y < __instance.MapHeight; ++y)
                {
                    for (int x = 0; x < __instance.MapWidth; ++x)
                    {
                        //TileData tile = GetTile(x, y);
                        TileData tile = (TileData)MI_GetTile.Invoke(__instance, new object[] {x, y} );
                        //if (GetClosestCityDistance(tile) > __instance.CitySiteMaxResourceDistance)
                        if ((int)MI_GetClosestCityDistance.Invoke(__instance, new object[] { tile } ) > iCitySiteMaxResourceDistance)
                        {
                            shuffledTiles.Add(tile);

                            //collect tiles for the second attempt
                            if (!tile.isWater(___infos) && !(___unreachableTiles.Contains(tile.ID)))
                            {
                                bool bValid = false;
                                if (tile.isImpassable(___infos))      //skip unreachable tiles for bIgnoreHeight = true, we don't want Olives inside mountain ranges
                                {
                                    //at least one adjacent tile needs to be reachable, passable land
                                    for (DirectionType dir = 0; dir < DirectionType.NUM_TYPES; ++dir)
                                    {
                                        //TileData adjacent = TileAdjacent(tile, dir);
                                        TileData adjacent = (TileData)MI_TileAdjacent.Invoke(__instance, new object[] { tile, dir } );
                                        if (adjacent != null)
                                        {
                                            if (adjacent.isImpassable(___infos)) continue;
                                            if (adjacent.isWater(___infos)) continue;
                                            if (___unreachableTiles.Contains(adjacent.ID)) continue;

                                            bValid = true;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    bValid = true;
                                }
                                if (bValid) shuffledTilesReduced.Add(tile);
                            }
                        }
                    }
                }

                shuffledTiles.Shuffle(___random.NextSeed());
                shuffledTilesReduced.Shuffle(___random.NextSeed());

                List<ResourceType> shuffledResources = new List<ResourceType>();
                List<ResourceType> shuffledFailedResources = new List<ResourceType>();
                List<ResourceType> shuffledNonLuxResources = new List<ResourceType>(); //new list for separation
                List<ResourceType> shuffledNonLuxFailedResources = new List<ResourceType>(); //new list for separation

                //the changes start here
                int iMinLuxury = 1 + ((2 * resourceDensityPercent * iPlayerCount) / (100 * 5));    // 1 + 2/5 of player number (rounded down)
                int iMinNonLuxury = 1 + ((1 * resourceDensityPercent * iPlayerCount) / (100 * 4)); // 1 + 1/4 of player number (rounded down)
                int iToPlace = 0;

                int iMaxResource = 0 + ((5 * resourceDensityPercent * iPlayerCount) / (100 * 2));    // 5/2 * player number (rounded down)

                int iLatitudeRange = __instance.MaxLatitude - __instance.MinLatitude;

                // place at least iMinLuxury instances of each luxury resource, iMinNonLuxury instances of non-luxury resources.
                for (ResourceType eLoopResource = 0; eLoopResource < ___infos.resourcesNum(); eLoopResource++)
                {
                    if (___infos.resource(eLoopResource).miProbThousand > 0 /* && ___resourceCounts.GetOrDefault(eLoopResource, 0) == 0 */)
                    {

                        //if (IsResourceCategory(eLoopResource, ResourceCategory.LUXURY))
                        if ((bool)MI_IsResourceCategory.Invoke(__instance, new object[] { eLoopResource, rcLUXURY } ))
                        {
                            //condition moved here
                            if (___resourceCounts.GetOrDefault(eLoopResource, 0) < iMinLuxury)
                            {
                                if (__instance.MinLatitude + (iLatitudeRange / 4) > ___infos.resource(eLoopResource).miMaxLatitude)
                                {
                                    ___infos.resource(eLoopResource).miMaxLatitude = __instance.MinLatitude + (iLatitudeRange / 4);
                                    UnityEngine.Debug.Log("iMaxLatitude of " + ___infos.resource(eLoopResource).mzType + " increased to " + ___infos.resource(eLoopResource).miMaxLatitude.ToStringCached());
                                }
                                if (__instance.MaxLatitude - (iLatitudeRange / 4) < ___infos.resource(eLoopResource).miMinLatitude)
                                {
                                    ___infos.resource(eLoopResource).miMinLatitude = __instance.MaxLatitude - (iLatitudeRange / 4);
                                    UnityEngine.Debug.Log("iMinLatitude of " + ___infos.resource(eLoopResource).mzType + " decreased to " + ___infos.resource(eLoopResource).miMaxLatitude.ToStringCached());
                                }

                                shuffledResources.Add(eLoopResource);
                                iToPlace += iMinLuxury - ___resourceCounts.GetOrDefault(eLoopResource, 0);
                            }
                        }
                        else //also place non-luxuries. 
                        {
                            if (___resourceCounts.GetOrDefault(eLoopResource, 0) < iMinNonLuxury)
                            {
                                if (__instance.MinLatitude + (iLatitudeRange / 5) > ___infos.resource(eLoopResource).miMaxLatitude)
                                {
                                    ___infos.resource(eLoopResource).miMaxLatitude = __instance.MinLatitude + (iLatitudeRange / 5);
                                    UnityEngine.Debug.Log("iMaxLatitude of " + ___infos.resource(eLoopResource).mzType + " increased to " + ___infos.resource(eLoopResource).miMaxLatitude.ToStringCached());
                                }
                                if (__instance.MaxLatitude - (iLatitudeRange / 5) < ___infos.resource(eLoopResource).miMinLatitude)
                                {
                                    ___infos.resource(eLoopResource).miMinLatitude = __instance.MaxLatitude - (iLatitudeRange / 5);
                                    UnityEngine.Debug.Log("iMinLatitude of " + ___infos.resource(eLoopResource).mzType + " decreased to " + ___infos.resource(eLoopResource).miMaxLatitude.ToStringCached());
                                }

                                shuffledNonLuxResources.Add(eLoopResource);
                                iToPlace += iMinNonLuxury - ___resourceCounts.GetOrDefault(eLoopResource, 0);
                            }
                        }
                    }
                }

                UnityEngine.Debug.Log("extra Resources to place: " + shuffledResources.Count().ToStringCached() + " luxury types, " + shuffledNonLuxResources.Count().ToStringCached() + " non-luxury types, " + iToPlace.ToStringCached() + " pieces total");
                UnityEngine.Debug.Log("Minimum Luxury amount: " + iMinLuxury.ToStringCached() + " , minimum non-luxury: " + iMinNonLuxury.ToStringCached());

                for (int i = 0; i < iMinLuxury; i++) //tiered approach
                {
                    shuffledResources.Shuffle(___random.NextSeed());

                    foreach (ResourceType eLoopResource in shuffledResources)
                    {
                        //int iLoopResourcePlaced = ___resourceCounts.GetOrDefault(eLoopResource, 0);
                        if (___resourceCounts.GetOrDefault(eLoopResource, 0) > i) continue;
                        if (shuffledFailedResources.Contains(eLoopResource)) continue;

                        bool bPlaced = false;
                        foreach (TileData tile in shuffledTiles)
                        {
                            //if (CanPlaceResource(tile, eLoopResource))
                            if ((bool)(MI_CanPlaceResource.Invoke(__instance, new object[] { tile, eLoopResource, false, null, int.MinValue, int.MaxValue } )))
                            {
                                //PlaceResource(tile, eLoopResource);
                                MI_PlaceResource.Invoke(__instance, new object[] { tile, eLoopResource } );
                                bPlaced = true;
                                break;
                            }
                        }

                        //trying again, with bIgnoreHeight = true but only with shuffledTilesReduced
                        if (!bPlaced)
                        {
                            //UnityEngine.Debug.Log("Could not place minimum Luxury amount the normal way, only " + iLoopResourcePlaced.ToStringCached() + " of " + ___infos.resource(eLoopResource).mzType + ". Trying again with bIgnoreHeight = true");

                            foreach (TileData tile in shuffledTilesReduced)
                            {
                                //if (CanPlaceResource(tile, eLoopResource))
                                if ((bool)(MI_CanPlaceResource.Invoke(__instance, new object[] { tile, eLoopResource, true, null, int.MinValue, int.MaxValue })))
                                {
                                    //PlaceResource(tile, eLoopResource);
                                    MI_PlaceResource.Invoke(__instance, new object[] { tile, eLoopResource } );
                                    bPlaced = true;
                                    break;
                                }
                            }
                        }

                        if (!bPlaced)
                        {
                            UnityEngine.Debug.Log("Could not place minimum Luxury amount, only " + ___resourceCounts.GetOrDefault(eLoopResource, 0).ToStringCached() + " of " + ___infos.resource(eLoopResource).mzType);
                            shuffledFailedResources.Add(eLoopResource);
                        }
                    }
                }

                foreach (ResourceType eLoopResource in shuffledResources)
                {
                    if (!(shuffledFailedResources.Contains(eLoopResource)))
                    {
                        UnityEngine.Debug.Log("Properly reached minimum Luxury amount: " + ___resourceCounts.GetOrDefault(eLoopResource, 0).ToStringCached() + "x " + ___infos.resource(eLoopResource).mzType);
                    }
                }

                //shuffledResources.Clear();
                //shuffledFailedResources.Clear();


                //################################################################
                //PART III: Try to place non-luxury resources on unaffiliate tiles

                //repeat for non-luxuries. Separated to give give more priority to Luxuries
                shuffledNonLuxResources.Shuffle(___random.NextSeed());
                foreach (ResourceType eLoopResource in shuffledNonLuxResources)
                {
                    int iLoopResourcePlaced = ___resourceCounts.GetOrDefault(eLoopResource, 0);
                    foreach (TileData tile in shuffledTiles)
                    {
                        //if (CanPlaceResource(tile, eLoopResource))
                        if ((bool)(MI_CanPlaceResource.Invoke(__instance, new object[] { tile, eLoopResource, false, null, int.MinValue, int.MaxValue } )))
                        {
                            //PlaceResource(tile, eLoopResource);
                            MI_PlaceResource.Invoke(__instance, new object[] { tile, eLoopResource } );

                            //only end this if enough were placed
                            iLoopResourcePlaced++;
                            {
                                if (iLoopResourcePlaced >= iMinNonLuxury)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    {
                        if (iLoopResourcePlaced < iMinNonLuxury)
                        {
                            shuffledNonLuxFailedResources.Add(eLoopResource);
                            UnityEngine.Debug.Log("Could not place minimum non-luxury amount, only " + iLoopResourcePlaced.ToStringCached() + " of " + ___infos.resource(eLoopResource).mzType);
                        }
                        else
                        {
                            UnityEngine.Debug.Log("Properly reached minimum non-luxury amount: " + iLoopResourcePlaced.ToStringCached() + "x " + ___infos.resource(eLoopResource).mzType);
                        }
                    }
                }
                //shuffledNonLuxResources.Clear();
                //repeat end
                //collect Overabundance info
                List<ResourceType> sortedOverabundantResources = new List<ResourceType>(); //new list for overabundant resources
                List<ResourceType> shuffledNotOverabundantResources = new List<ResourceType>(); //new list for the others / not overabundant resources
                
                for (ResourceType eLoopResource = 0; eLoopResource < ___infos.resourcesNum(); eLoopResource++)
                {
                    if (___infos.resource(eLoopResource).miProbThousand > 0) // we really only need those that will actually be placed,
                                                                             // so unless there was a Resource with iMinPerPlayer > 0 but iProbThousand == 0,
                                                                             // we can skip iProbThousand == 0 types.
                    {
                        if (___resourceCounts.GetOrDefault(eLoopResource, 0) > iMaxResource && 
                            (___resourceCounts.GetOrDefault(eLoopResource) > (___infos.resource(eLoopResource).miMinPerPlayer * iPlayerCount))) //allow miMinPerPlayer to override iMaxResource
                        {
                            if (sortedOverabundantResources.Count() == 0)
                            {
                                sortedOverabundantResources.Add(eLoopResource);
                            }
                            else
                            {
                                //By the power of stackoverflow (I think this actually works)
                                var index = sortedOverabundantResources.BinarySearch(eLoopResource, 
                                    Comparer<ResourceType>.Create((x, y) => ___resourceCounts.GetOrDefault(x, 0).CompareTo(___resourceCounts.GetOrDefault(y, 0))));
                                if (index < 0) index = ~index;
                                sortedOverabundantResources.Insert(index, eLoopResource);
                            }
                        }
                        else
                        {
                            shuffledNotOverabundantResources.Add(eLoopResource);
                        }
                    }
                }

                string overAbundanceText = "";
                bool bFirst = true;
                foreach (ResourceType eLoopResource in sortedOverabundantResources)
                {
                    if (bFirst)
                    {
                        overAbundanceText = "Overabundant at start: ";
                        bFirst = false;
                    }
                    else
                    {
                        overAbundanceText += "; ";
                    }
                    overAbundanceText += ___infos.resource(eLoopResource).mzType + ": " + ___resourceCounts.GetOrDefault(eLoopResource, 0).ToStringCached() + "x";
                }
                if (overAbundanceText != "")
                {
                    UnityEngine.Debug.Log(overAbundanceText);
                }
                overAbundanceText = "";


                //################################################################
                //PART IV: Try to place luxury resources on city tiles by replacing city resources of similar types

                if (shuffledFailedResources.Count > 0)
                {
                    int iDistanceWeight = 100;
                    int iResourceCountWeight = 50 / iPlayerCount;
                    int iRandWeight = 100;
                    List<TileScore> CitySiteLuxuryTileScores = new List<TileScore>();

                    for (int y = 0; y < __instance.MapHeight; ++y)
                    {
                        for (int x = 0; x < __instance.MapWidth; ++x)
                        {
                            //TileData tile = GetTile(x, y);
                            TileData tile = (TileData)MI_GetTile.Invoke(__instance, new object[] { x, y });
                            int iCityDistance = (int)MI_GetClosestCityDistance.Invoke(__instance, new object[] { tile });
                            int iTileScore = 0;

                            //if (GetClosestCityDistance(tile) > __instance.CitySiteMaxResourceDistance)
                            if (iCityDistance <= iCitySiteMaxResourceDistance)
                            {
                                if (tile.meResource != ResourceType.NONE)
                                {
                                    if (___resourceCounts.GetOrDefault(tile.meResource, 0) > iPlayerCount &&
                                        (___resourceCounts.GetOrDefault(tile.meResource, 0) > (___infos.resource(tile.meResource).miMinPerPlayer * iPlayerCount)))
                                    {
                                        if ((bool)MI_IsResourceCategory.Invoke(__instance, new object[] { tile.meResource, rcLUXURY }))
                                        {
                                            if (CountAdjacentSameResource(__instance, tile) == 0) //disregard clusters
                                            {
                                                iTileScore = iDistanceWeight * iCityDistance + (___resourceCounts.GetOrDefault(tile.meResource, 0) * iResourceCountWeight) + ___random.Next(iRandWeight);
                                                CitySiteLuxuryTileScores.Add(new TileScore(tile, iTileScore));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }



                    {
                        List<ResourceType> shuffledFailedAgainResources = new List<ResourceType>();

                        CitySiteLuxuryTileScores.Sort((x, y) =>
                        {
                            return (y.Score).CompareTo((x.Score));
                        });

                        bool bPlaced = false;
                        Dictionary<int, ResourceType> placed = new Dictionary<int, ResourceType>();
                        ResourceType eTileReplacedLuxury = ResourceType.NONE;
                        TileData eReplacedLuxuryTile = null;
                        int iRing = 0;

                        for (int i = 0; i < iMinLuxury; i++) //tiered approach again
                        {
                            foreach (ResourceType eLoopResource in shuffledFailedResources)
                            {
                                if (___resourceCounts.GetOrDefault(eLoopResource, 0) > i) continue;
                                if (shuffledFailedAgainResources.Contains(eLoopResource)) continue;

                                eTileReplacedLuxury = ResourceType.NONE;
                                eReplacedLuxuryTile = null;
                                placed.Clear();
                                bPlaced = false;

                                foreach (TileScore TileAndScore in CitySiteLuxuryTileScores)
                                {
                                    if (((bool)MI_IsResourceCategory.Invoke(__instance, new object[] { eLoopResource, rcLATE_GATHERED }) ==
                                        (bool)MI_IsResourceCategory.Invoke(__instance, new object[] { TileAndScore.tile.meResource, rcLATE_GATHERED })))
                                    {
                                        eTileReplacedLuxury = TileAndScore.tile.meResource;
                                        eReplacedLuxuryTile = TileAndScore.tile;

                                        TileAndScore.tile.meResource = ResourceType.NONE;
                                        ___placedResources[TileAndScore.tile.ID] = ResourceType.NONE;

                                        iRing = MinResTileDistance(__instance, TileAndScore.tile.ID, ___closestCitySites[TileAndScore.tile.ID]);

                                        placed = MinResPlaceRingResourcesSpecific(__instance, MinResGetTile(__instance, ___closestCitySites[TileAndScore.tile.ID]), eLoopResource, iRing, iRing, 1, 1, false, null, int.MinValue, int.MaxValue);

                                        if (placed.Count >= 1)
                                        {
                                            bPlaced = true;
                                            CitySiteLuxuryTileScores.Remove(TileAndScore);
                                            ___resourceCounts[eTileReplacedLuxury] = ___resourceCounts.GetOrDefault(eTileReplacedLuxury, 0) - 1;

                                            if (___resourceCounts.GetOrDefault(eTileReplacedLuxury, 0) <= iPlayerCount ||
                                            (___resourceCounts.GetOrDefault(eTileReplacedLuxury, 0) <= (___infos.resource(eTileReplacedLuxury).miMinPerPlayer * iPlayerCount)))
                                            {
                                                //remove all tiles with that resource from list if count got too low
                                                foreach (TileScore TileAndScoreRemove in CitySiteLuxuryTileScores)
                                                {
                                                    if (TileAndScoreRemove.tile.meResource == eTileReplacedLuxury)
                                                    {
                                                        CitySiteLuxuryTileScores.Remove(TileAndScoreRemove);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                foreach (TileScore TileAndScoreReduce in CitySiteLuxuryTileScores)
                                                {
                                                    if (TileAndScoreReduce.tile.meResource == eTileReplacedLuxury)
                                                    {
                                                        TileAndScoreReduce.Score -= iResourceCountWeight;
                                                    }
                                                }
                                            }

                                            CitySiteLuxuryTileScores.Sort((x, y) =>
                                            {
                                                return (y.Score).CompareTo((x.Score));
                                            });

                                            break;
                                        }
                                        else
                                        {
                                            //if the luxury could not be placed, restore tile resources to original
                                            eReplacedLuxuryTile.meResource = eTileReplacedLuxury;
                                            ___placedResources[eReplacedLuxuryTile.ID] = eTileReplacedLuxury;
                                        }

                                    }
                                }

                                if (!bPlaced)
                                {
                                    UnityEngine.Debug.Log("Even with city tiles, could not place minimum Luxury amount, only " + ___resourceCounts.GetOrDefault(eLoopResource, 0).ToStringCached() + " of " + ___infos.resource(eLoopResource).mzType);
                                    shuffledFailedAgainResources.Add(eLoopResource);
                                }
                            }
                        }

                        foreach (ResourceType eLoopResource in shuffledFailedResources)
                        {
                            if (!(shuffledFailedAgainResources.Contains(eLoopResource)))
                            {
                                UnityEngine.Debug.Log("Using city tiles, properly reached minimum Luxury amount: " + ___resourceCounts.GetOrDefault(eLoopResource, 0).ToStringCached() + "x " + ___infos.resource(eLoopResource).mzType);
                            }
                        }

                    }

                }


                foreach (TileData tile in shuffledTiles)
                {
                    shuffledNotOverabundantResources.Shuffle(___random.NextSeed());
                    ResourceType ePlacedResource = ResourceType.NONE;

                    foreach (ResourceType eLoopResource in shuffledNotOverabundantResources)
                    {
                        if ((___resourceCounts.GetOrDefault(eLoopResource) < (___infos.resource(eLoopResource).miMinPerPlayer * iPlayerCount)) ||
                            (___random.Next(1000) < (___infos.resource(eLoopResource).miProbThousand * resourceDensityPercent) / 100))
                        {
                            //if (CanPlaceResource(tile, eLoopResource))
                            if ((bool)(MI_CanPlaceResource.Invoke(__instance, new object[] { tile, eLoopResource, false, null, int.MinValue, int.MaxValue } )))
                            {
                                //PlaceResource(tile, eLoopResource);
                                MI_PlaceResource.Invoke(__instance, new object[] { tile, eLoopResource } );
                                ePlacedResource = eLoopResource;

                                break;
                            }
                        }
                    }
                    if (ePlacedResource != ResourceType.NONE)
                    {
                        //if (___resourceCounts.GetOrDefault(ePlacedResource, 0) > iMaxResource &&
                        //    (___resourceCounts.GetOrDefault(ePlacedResource) > (___infos.resource(ePlacedResource).miMinPerPlayer * iPlayerCount))) //allow miMinPerPlayer to override iMaxResource
                        //{
                        //    shuffledNotOverabundantResources.Remove(ePlacedResource);
                        //    sortedOverabundantResources.Insert(0, ePlacedResource); //because of miMinPerPlayer this could theoretically lead to unsorted list! (but only if miMinPerPlayer * players.Count > iMaxResource)
                        //}

                        continue;
                    }
                    //only after failing to place other Resources: try placing Resources that are already overabundant, starting with the one with the lowest number of prior placements

                    int iLoopIndex = -1;
                    //foreach (ResourceType eLoopResource in sortedOverabundantResources)
                    for (int i = 0; i < sortedOverabundantResources.Count(); i++)
                    {
                        ResourceType eLoopResource = sortedOverabundantResources[i];
                        if ((___random.Next(1000) < (___infos.resource(eLoopResource).miProbThousand * resourceDensityPercent) / 100)) // no need to check for miMinPerPlayer anymore
                        {
                            //if (CanPlaceResource(tile, eLoopResource))
                            if ((bool)(MI_CanPlaceResource.Invoke(__instance, new object[] { tile, eLoopResource, false, null, int.MinValue, int.MaxValue })))
                            {
                                //PlaceResource(tile, eLoopResource);
                                MI_PlaceResource.Invoke(__instance, new object[] { tile, eLoopResource });
                                ePlacedResource = eLoopResource;
                                iLoopIndex = i;

                                break;
                            }
                        }
                    }

                    //if (ePlacedResource != ResourceType.NONE && iLoopIndex != -1)
                    //{
                    //    if (iLoopIndex + 1 < sortedOverabundantResources.Count() && ___resourceCounts.GetOrDefault(ePlacedResource, 0) > ___resourceCounts.GetOrDefault(sortedOverabundantResources[iLoopIndex + 1], 0))
                    //    {
                    //        //linear sort doesn't care about theoretically unsorted list
                    //        sortedOverabundantResources.RemoveAt(iLoopIndex);
                    //        if (___resourceCounts.GetOrDefault(ePlacedResource, 0) > ___resourceCounts.GetOrDefault(sortedOverabundantResources.Last(), 0))
                    //        {
                    //            sortedOverabundantResources.Add(ePlacedResource);
                    //        }
                    //        else
                    //        {
                    //            for (int index = iLoopIndex + 2; index < sortedOverabundantResources.Count() - 1; index++)
                    //            {
                    //                if (___resourceCounts.GetOrDefault(ePlacedResource, 0) <= ___resourceCounts.GetOrDefault(sortedOverabundantResources[index], 0))
                    //                {
                    //                    sortedOverabundantResources.Insert(index, ePlacedResource);
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                }

                overAbundanceText = "";
                bFirst = true;
                foreach (ResourceType eLoopResource in sortedOverabundantResources)
                {
                    if (bFirst)
                    {
                        overAbundanceText = "Overabundant at the end: ";
                        bFirst = false;
                    }
                    else
                    {
                        overAbundanceText += "; ";
                    }
                    overAbundanceText += ___infos.resource(eLoopResource).mzType + ": " + ___resourceCounts.GetOrDefault(eLoopResource, 0).ToStringCached() + "x";
                }
                if (overAbundanceText != "")
                {
                    UnityEngine.Debug.Log(overAbundanceText);
                }
                overAbundanceText = null;

            }

            //UnityEngine.Debug.Log("This Mod is actually being used.");

            return false; //don't run the original method
        }
    }
   
}
