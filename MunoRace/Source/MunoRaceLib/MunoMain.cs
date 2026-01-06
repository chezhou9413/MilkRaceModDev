using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MunoRaceLib
{
    [StaticConstructorOnStartup]
    public static class MunoMain
    {
        static Harmony harmony;
        static MunoMain()
        {
            harmony = new Harmony("chezhou.Race.MunoRaceLib");
            harmony.PatchAll(typeof(MunoMain).Assembly);
        }
    }
}
