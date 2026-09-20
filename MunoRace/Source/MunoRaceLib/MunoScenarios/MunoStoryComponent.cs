using System.Collections.Generic;
using System.Linq;
using MunoRaceLib.MunoEndings;
using MunoRaceLib.MunoPursuit;
using MunoRaceLib.MunoRecording;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoScenarios
{
    //保存开局类型和起始时刻，并按固定间隔驱动摄像、追击与结局资格。
    public class MunoStoryComponent : GameComponent
    {
        public MunoStartMode mode;
        public int startTick = -1;

        //创建存档专属的剧情状态。
        public MunoStoryComponent(Game game) { }

        //初始化开局外交、逃亡者名单及玩法说明。
        public void Begin(MunoStartMode startMode, List<Pawn> starters)
        {
            mode = startMode;
            startTick = Find.TickManager.TicksGame;
            Faction faction = MunoStoryUtility.Faction;
            if (faction == null)
                throw new System.InvalidOperationException("缪诺剧本要求世界中存在缪诺聚落。");
            int goodwill = mode == MunoStartMode.Fugitives ? -100 : mode == MunoStartMode.Expedition ? 80 : 0;
            faction.TryAffectGoodwillWith(Faction.OfPlayer, goodwill - faction.GoodwillWith(Faction.OfPlayer),
                canSendMessage: false, canSendHostilityLetter: false);
            if (mode == MunoStartMode.Fugitives)
                Current.Game.GetComponent<MunoPursuitComponent>().Begin(starters);
            string text = mode == MunoStartMode.Expedition
                ? "聚落已为三名开拓者准备物资。选中殖民者可以通过联络终端联系聚落。"
                : mode == MunoStartMode.SurvivalShow
                    ? "生存挑战开始。让一名殖民者穿戴摄像机，定期自动拍摄附近殖民者。完整拍完才能积累片段。通过摄像机上传录像带可获得物资，无需通讯台。"
                    : "你们暂时逃离了聚落。追击队将在宽限期后到来；达成和平、原始逃亡者全部离开玩家队伍或完成融入结局，可以解除追击。";
            Find.LetterStack.ReceiveLetter("缪诺开局", text, LetterDefOf.NeutralEvent);
        }

        //低频扫描穿戴摄像机的殖民者，并撤销已经不满足条件的结局资格。
        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 250 != 0) return;
            foreach (Map map in Find.Maps)
            {
                //自动拍摄会再次查询并重建原版复用的殖民者列表，快照用于保持本轮遍历稳定。
                Pawn[] colonists = map.mapPawns.FreeColonistsSpawned.ToArray();
                foreach (Pawn pawn in colonists)
                {
                    if (pawn.apparel == null) continue;
                    foreach (Apparel apparel in pawn.apparel.WornApparel)
                        apparel.GetComp<Comp_MunoCamera>()?.Sample(pawn);
                }
            }
            Current.Game.GetComponent<MunoEndingComponent>().RefreshEligibility();
        }

        //保存开局类型和起始游戏刻，避免读档改变七天边界。
        public override void ExposeData()
        {
            Scribe_Values.Look(ref mode, "mode");
            Scribe_Values.Look(ref startTick, "startTick", -1);
        }
    }
}
