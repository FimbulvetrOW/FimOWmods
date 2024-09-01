using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TenCrowns.AppCore;
using TenCrowns.GameCore;
using Mohawk.SystemCore;

namespace BuyCoastal
{
    public class BuyCoastalClass : ModEntryPointAdapter
    {
        public const string MY_HARMONY_ID = "Fim.BuyCoastal.patch";
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

    [HarmonyPatch(typeof(City), nameof(City.canBuyTile), new Type[] { typeof(Tile), typeof(YieldType), typeof(Player), typeof(bool) })]
    public class PatchBuyCoastalCityClass
    {
        static void Postfix(ref bool __result, ref City __instance, Tile pTile, YieldType eYield, Player pActingPlayer, bool bTestEnabled)
        {
            if (!__result && !pTile.isLand()) //Water
            {
                //only coast and lakes, not ocean
                HeightType COAST_HEIGHT = __instance.game().infos().getType<HeightType>("HEIGHT_COAST");
                HeightType LAKE_HEIGHT = __instance.game().infos().getType<HeightType>("HEIGHT_LAKE");
                if (pTile.getHeight() == COAST_HEIGHT || pTile.getHeight() == LAKE_HEIGHT)
                {
                    //check conditions, same as for land tiles

                    //original method as of version 1.0.63890
                    /*
                            public virtual bool canBuyTile(Tile pTile, YieldType eYield, Player pActingPlayer, bool bTestEnabled = true)
                            {
                                if (pTile.isWater())
                                {
                                    return false;
                                }

                                if (pTile.hasOwner() && (pTile.getOwner() != getPlayer()))
                                {
                                    return false;
                                }

                                if (pTile.hasCityTerritory())
                                {
                                    if (pTile.hasImprovement())
                                    {
                                        return false;
                                    }

                                    if (pTile.hasCityTerritory() && (pTile.getCityTerritory() == getID()))
                                    {
                                        return false;
                                    }
                                }

                                if (pTile.isUrban())
                                {
                                    return false;
                                }

                                if (!adjacentToTerritory(pTile))
                                {
                                    return false;
                                }

                                if (bTestEnabled)
                                {
                                    if (!(pTile.hasOwner()))
                                    {
                                        if (!canBuyTileUnlocked(eYield))
                                        {
                                            return false;
                                        }
                                    }

                                    if (pActingPlayer.getYieldStockpileWhole(eYield) < getBuyTileCost(pTile, eYield))
                                    {
                                        return false;
                                    }
                                }

                                return true;
                            }
                    */

                    if (pTile.hasOwner() && (pTile.getOwner() != __instance.getPlayer()))
                    {
                        return;
                    }

                    if (pTile.hasCityTerritory())
                    {
                        if (pTile.hasImprovement())
                        {
                            return;
                        }

                        if (pTile.hasCityTerritory() && (pTile.getCityTerritory() == __instance.getID()))
                        {
                            return;
                        }
                    }

                    // No urban on water
                    /*
                    if (pTile.isUrban())
                    {
                        return;
                    }
                    */

                    if (!__instance.adjacentToTerritory(pTile))
                    {
                        return;
                    }

                    if (bTestEnabled)
                    {
                        if (!(pTile.hasOwner()))
                        {
                            if (!__instance.canBuyTileUnlocked(eYield))
                            {
                                return;
                            }
                        }

                        if (pActingPlayer.getYieldStockpileWhole(eYield) < __instance.getBuyTileCost(pTile, eYield))
                        {
                            return;
                        }
                    }
                    __result = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Unit), nameof(Unit.canBuyTile), new Type[] { typeof(Tile), typeof(City), typeof(YieldType), typeof(Player), typeof(bool) })]
    public class PatchBuyCoastalUnitClass
    {
        static void Postfix(ref bool __result, ref Unit __instance, Tile pTile, City pCity, YieldType eYield, Player pActingPlayer, bool bTestEnabled)
        {
            if (__result)
            {
                //disable buying water tiles for Scouts that can move on water with Exploration law
                //only true water units can buy water tiles
                if (!pTile.isLand() && !__instance.info().mbWater)
                {
                    __result = false;
                }
            }
        }
    }
}
