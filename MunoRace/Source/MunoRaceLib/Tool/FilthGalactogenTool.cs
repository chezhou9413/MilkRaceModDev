using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MunoRaceLib.Tool
{
    public static class FilthGalactogenTool
    {
        public static List<string> paths = new List<string>()
            {
                "Fleck/MilkSplatter/SplatterA",
                "Fleck/MilkSplatter/SplatterB",
                "Fleck/MilkSplatter/SplatterC",
            };
        public static void SpawnFilthGalactogen(Pawn pawn)
        {
            ThingDef filthDef = MunoDefDataRef.Muno_Filth_Galactogen;
            if (filthDef != null)
            {
                FilthMaker.TryMakeFilth(pawn.Position, pawn.Map, filthDef, pawn.LabelShort, 1);
            }
        }

        public static void SpawnMilkSplatter(Vector3 position, Map map, int count = 6)
        {
            if (map == null) return;
            FleckDef fleckDef = MunoDefDataRef.Muno_Fleck_MilkSplatter;
            fleckDef.graphicData.texPath = paths.RandomElement();

            for (int i = 0; i < count; i++)
            {
                FleckCreationData data = FleckMaker.GetDataStatic(position, map, fleckDef);
                float angle = Rand.Range(0f, 360f);
                float speed = Rand.Range(0.5f,1f);
                data.velocity = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * speed;
                data.rotation = Rand.Range(0f, 360f);
                data.rotationRate = Rand.Range(-100f, 100f);
                data.scale = Rand.Range(0.4f, 0.8f);
                map.flecks.CreateFleck(data);
            }
        }
    }
}
