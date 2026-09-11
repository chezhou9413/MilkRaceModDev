using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoQuests
{
    //保存情报所属派系，使用成功后生成该派系的限时据点并消耗情报。
    public class CompUseEffect_MunoOutpostIntel : CompUseEffect
    {
        private Faction sourceFaction;

        //读取物品定义中的据点生成参数。
        private CompProperties_MunoOutpostIntel IntelProps => (CompProperties_MunoOutpostIntel)props;

        //绑定情报原持有者所属的敌对派系。
        public void Initialize(Faction faction)
        {
            sourceFaction = faction;
        }

        //说明情报所属派系、使用效果和据点存在期限。
        public override string CompInspectStringExtra()
        {
            return "情报所属派系：" + (sourceFaction == null ? "未绑定" : sourceFaction.Name)
                + "\n使用后揭示一个存在 " + IntelProps.siteDays.ToString("0.#") + " 天的据点。";
        }

        //限制在地面地图由玩家成员使用，并明确说明失效派系等不能使用的原因。
        public override AcceptanceReport CanBeUsedBy(Pawn pawn)
        {
            if (pawn.Faction != Faction.OfPlayer || !pawn.Spawned) return "需要由地图上的玩家成员使用。";
            if (pawn.Map.Tile.LayerDef != PlanetLayerDefOf.Surface) return "需要在地面地图使用据点情报。";
            if (sourceFaction == null || sourceFaction.defeated) return "情报所属派系已经失效。";
            if (sourceFaction.IsPlayer || !sourceFaction.HostileTo(Faction.OfPlayer)) return "情报所属派系已不再敌对玩家。";
            return true;
        }

        //使用时寻找可达地块，创建带原版到期组件的据点，成功后才消耗物品。
        public override void DoEffect(Pawn usedBy)
        {
            AcceptanceReport report = CanBeUsedBy(usedBy);
            if (!report.Accepted) throw new InvalidOperationException(report.Reason);
            Map map = usedBy.Map;
            if (!TileFinder.TryFindNewSiteTile(out PlanetTile tile, map.Tile, IntelProps.minSiteDistance,
                IntelProps.maxSiteDistance, canBeSpace: false, layer: map.Tile.Layer))
            {
                Messages.Message("附近没有可达的空闲据点地块，情报未被消耗。", parent, MessageTypeDefOf.RejectInput, false);
                return;
            }
            float points = Mathf.Max(200f, StorytellerUtility.DefaultThreatPointsNow(map) * IntelProps.threatFactor);
            Site site = SiteMaker.MakeSite(new[] { SitePartDefOf.BanditCamp }, tile, sourceFaction, threatPoints: points);
            site.GetComponent<TimeoutComp>().StartTimeout((int)(IntelProps.siteDays * GenDate.TicksPerDay));
            Find.WorldObjects.Add(site);
            //先记录情报已被带回的事实，再消耗实体，避免附加奖励依赖已销毁物品的位置。
            foreach (QuestPart_MunoCapture mission in Find.QuestManager.QuestsListForReading
                .Where(quest => quest.State == QuestState.Ongoing)
                .SelectMany(quest => quest.PartsListForReading).OfType<QuestPart_MunoCapture>())
                mission.NotifyIntelligenceUsed(parent, map);
            parent.Destroy(DestroyMode.Vanish);
            Find.LetterStack.ReceiveLetter("情报揭示了新的据点", "已确认 " + sourceFaction.Name
                + " 的一处据点。请在 " + IntelProps.siteDays.ToString("0.#")
                + " 天内抵达；到期时尚未进入的据点将消失。", LetterDefOf.NeutralEvent, new LookTargets(site));
        }

        //保存情报所属派系，运输、掉落和读档后仍指向同一派系。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref sourceFaction, "sourceFaction");
        }
    }
}
