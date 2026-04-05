using HarmonyLib;
using MunoRaceLib.MunoDefRef;
using System.Reflection;
using RimWorld;
using Verse;

namespace MunoRaceLib.Patch
{
    [HarmonyPatch]
    public static class Patch_PawnApparelTracker_TryDrop
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.TryDrop), new[] { typeof(Apparel), typeof(Apparel).MakeByRefType(), typeof(IntVec3), typeof(bool) });
        }

        public static bool Prefix(Pawn_ApparelTracker __instance, Apparel ap, ref Apparel resultingAp, ref bool __result)
        {
            if (ap == null)
            {
                return true;
            }

            if (ap.def == MunoDefDataRef.MunoRace_Apparel_Tentaclefrenzyarmor || ap.def == MunoDefDataRef.MunoRace_Apparel_TentaclefrenzyarmorHead)
            {
                if (__instance?.pawn != null)
                {
                    Messages.Message("狂乱触手甲已与穿戴者结合，无法脱下。", __instance.pawn, MessageTypeDefOf.RejectInput, false);
                }

                resultingAp = null;
                __result = false;
                return false;
            }

            return true;
        }
    }
}
