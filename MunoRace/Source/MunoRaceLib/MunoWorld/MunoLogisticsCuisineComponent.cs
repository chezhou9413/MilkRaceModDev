using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    //负责保存后勤管理员分子料理送达、试吃、反馈和冷却状态。
    public class MunoLogisticsCuisineComponent : GameComponent
    {
        private const int CooldownTicks = GenDate.TicksPerDay * 30;
        private MunoLogisticsCuisineState state;
        private string trackedCuisineId;
        private bool trackedCuisineBoosted;
        private bool replacementUsed;
        private bool nextCuisineBoosted;
        private int cooldownUntilTick = -1;

        //初始化分子料理存档组件。
        public MunoLogisticsCuisineComponent(Game game)
        {
        }

        //返回当前分子料理流程状态。
        public MunoLogisticsCuisineState State
        {
            get
            {
                RefreshCooldownIfNeeded();
                return state;
            }
        }

        //保存或读取分子料理流程状态。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref state, "state", MunoLogisticsCuisineState.Available);
            Scribe_Values.Look(ref trackedCuisineId, "trackedCuisineId");
            Scribe_Values.Look(ref trackedCuisineBoosted, "trackedCuisineBoosted", defaultValue: false);
            Scribe_Values.Look(ref replacementUsed, "replacementUsed", defaultValue: false);
            Scribe_Values.Look(ref nextCuisineBoosted, "nextCuisineBoosted", defaultValue: false);
            Scribe_Values.Look(ref cooldownUntilTick, "cooldownUntilTick", -1);
        }

        //返回当前游戏中的分子料理组件。
        public static MunoLogisticsCuisineComponent Current()
        {
            return Verse.Current.Game?.GetComponent<MunoLogisticsCuisineComponent>();
        }

        //尝试请求一份新的分子料理并通过空投舱送达。
        public bool TryRequestCuisine(Pawn negotiator, out string replyText)
        {
            RefreshCooldownIfNeeded();
            if (state == MunoLogisticsCuisineState.Cooldown)
            {
                replyText = MunoLogisticsCuisineText.CooldownText;
                return false;
            }

            if (state == MunoLogisticsCuisineState.AwaitingTaste)
            {
                replyText = MunoLogisticsCuisineText.AwaitingTasteText;
                return false;
            }

            if (state == MunoLogisticsCuisineState.PendingFeedback)
            {
                replyText = MunoLogisticsCuisineText.PendingFeedbackText;
                return false;
            }

            if (!TryResolveDeliveryMap(negotiator, out Map map, out IntVec3 near))
            {
                replyText = "当前没有可用地图，后勤管理员无法送达分子料理。";
                return false;
            }

            DropCuisine(map, near, MunoLogisticsCuisineText.DeliveryLetterLabel, MunoLogisticsCuisineText.DeliveryLetterText);
            replyText = MunoLogisticsCuisineText.AcceptReplyText;
            return true;
        }

        //在被追踪的料理被玩家殖民者吃完后结算状态。
        public void NotifyCuisineTasted(Thing cuisine, Pawn ingester)
        {
            if (!IsTrackedCuisine(cuisine))
            {
                return;
            }

            if (ingester == null || !ingester.IsColonist)
            {
                NotifyCuisineLost(cuisine, ingester?.Map ?? cuisine.MapHeld);
                return;
            }

            trackedCuisineId = null;
            replacementUsed = false;
            state = MunoLogisticsCuisineState.PendingFeedback;
            string effectText = MunoLogisticsCuisineEffectUtility.ApplyRandomEffect(ingester, trackedCuisineBoosted);
            trackedCuisineBoosted = false;
            Messages.Message("分子料理效果：" + effectText, ingester, MessageTypeDefOf.PositiveEvent);
            Find.LetterStack.ReceiveLetter(
                MunoLogisticsCuisineText.FeedbackLetterLabel,
                MunoLogisticsCuisineText.FeedbackLetterText + "\n\n本次料理效果：" + effectText,
                LetterDefOf.PositiveEvent,
                ingester);
        }

        //在被追踪的料理非试吃销毁时处理补发或冷却。
        public void NotifyCuisineLost(Thing cuisine, Map previousMap)
        {
            if (!IsTrackedCuisine(cuisine) || state != MunoLogisticsCuisineState.AwaitingTaste)
            {
                return;
            }

            trackedCuisineId = null;
            bool lostCuisineBoosted = trackedCuisineBoosted;
            trackedCuisineBoosted = false;
            if (!replacementUsed && TryResolveDeliveryMap(previousMap, out Map map, out IntVec3 near))
            {
                replacementUsed = true;
                nextCuisineBoosted = lostCuisineBoosted;
                DropCuisine(map, near, MunoLogisticsCuisineText.ReplacementLetterLabel, MunoLogisticsCuisineText.ReplacementLetterText);
                return;
            }

            EnterCooldown();
        }

        //处理玩家对料理的反馈并返回通讯回复文本。
        public string ApplyFeedback(MunoLogisticsCuisineFeedback feedback, Pawn negotiator)
        {
            if (state != MunoLogisticsCuisineState.PendingFeedback)
            {
                return MunoLogisticsCuisineText.AwaitingTasteText;
            }

            switch (feedback)
            {
                case MunoLogisticsCuisineFeedback.Delicious:
                    nextCuisineBoosted = true;
                    EnterCooldown();
                    return MunoLogisticsCuisineText.DeliciousFeedbackReplyText;
                case MunoLogisticsCuisineFeedback.Ordinary:
                    EnterCooldown();
                    return MunoLogisticsCuisineText.OrdinaryFeedbackReplyText;
                case MunoLogisticsCuisineFeedback.Awful:
                    DropLavishMeals(negotiator);
                    EnterCooldown();
                    return MunoLogisticsCuisineText.AwfulFeedbackReplyText;
                default:
                    return MunoLogisticsCuisineText.OrdinaryFeedbackReplyText;
            }
        }

        //送出料理后记录被追踪的具体物品。
        private void TrackCuisine(Thing cuisine)
        {
            trackedCuisineId = cuisine?.ThingID;
            trackedCuisineBoosted = nextCuisineBoosted;
            state = MunoLogisticsCuisineState.AwaitingTaste;
            if (nextCuisineBoosted)
            {
                nextCuisineBoosted = false;
            }
        }

        //判断指定料理是否是当前等待试吃的料理。
        private bool IsTrackedCuisine(Thing cuisine)
        {
            return cuisine != null
                && cuisine.def == MunoDefDataRef.MunoRace_MolecularCuisine
                && !trackedCuisineId.NullOrEmpty()
                && trackedCuisineId == cuisine.ThingID;
        }

        //空投一份分子料理并发送蓝色信件。
        private void DropCuisine(Map map, IntVec3 near, string letterLabel, string letterText)
        {
            Thing cuisine = ThingMaker.MakeThing(MunoDefDataRef.MunoRace_MolecularCuisine);
            cuisine.stackCount = 1;
            TrackCuisine(cuisine);
            DropThing(map, near, cuisine, letterLabel, letterText);
        }

        //向玩家空投难吃反馈补偿。
        private void DropLavishMeals(Pawn negotiator)
        {
            if (!TryResolveDeliveryMap(negotiator, out Map map, out IntVec3 near))
            {
                return;
            }

            ThingDef mealDef = DefDatabase<ThingDef>.GetNamed("MealLavish");
            Thing meals = ThingMaker.MakeThing(mealDef);
            meals.stackCount = 10;
            DropThing(map, near, meals, MunoLogisticsCuisineText.CompensationLetterLabel, MunoLogisticsCuisineText.CompensationLetterText);
        }

        //把物品通过空投舱送到指定地图并发送蓝色信件。
        private static void DropThing(Map map, IntVec3 near, Thing thing, string letterLabel, string letterText)
        {
            List<Thing> things = new List<Thing> { thing };
            DropPodUtility.DropThingsNear(near, map, things, 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: true, forbid: false, allowFogged: true, Faction.OfPlayer);
            Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.PositiveEvent, new TargetInfo(near, map));
        }

        //进入一个月冷却状态。
        private void EnterCooldown()
        {
            trackedCuisineId = null;
            trackedCuisineBoosted = false;
            replacementUsed = false;
            state = MunoLogisticsCuisineState.Cooldown;
            cooldownUntilTick = GenTicks.TicksAbs + CooldownTicks;
            Messages.Message("后勤管理员分子料理进入 30 天冷却。", MessageTypeDefOf.NeutralEvent);
        }

        //冷却结束后恢复可请求状态。
        private void RefreshCooldownIfNeeded()
        {
            if (state == MunoLogisticsCuisineState.Cooldown && cooldownUntilTick >= 0 && GenTicks.TicksAbs >= cooldownUntilTick)
            {
                state = MunoLogisticsCuisineState.Available;
                cooldownUntilTick = -1;
            }
        }

        //根据通讯者或玩家地图寻找空投位置。
        private static bool TryResolveDeliveryMap(Pawn negotiator, out Map map, out IntVec3 near)
        {
            if (negotiator?.Map != null)
            {
                map = negotiator.Map;
                near = negotiator.Position;
                return true;
            }

            return TryResolveDeliveryMap((Map)null, out map, out near);
        }

        //根据原地图或玩家主地图寻找空投位置。
        private static bool TryResolveDeliveryMap(Map preferredMap, out Map map, out IntVec3 near)
        {
            if (preferredMap != null && Find.Maps.Contains(preferredMap) && preferredMap.IsPlayerHome)
            {
                map = preferredMap;
                near = map.mapPawns.FreeColonistsSpawned.Count > 0 ? map.mapPawns.FreeColonistsSpawned.RandomElement().Position : map.Center;
                return true;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                if (Find.Maps[i].IsPlayerHome)
                {
                    map = Find.Maps[i];
                    near = map.mapPawns.FreeColonistsSpawned.Count > 0 ? map.mapPawns.FreeColonistsSpawned.RandomElement().Position : map.Center;
                    return true;
                }
            }

            map = null;
            near = IntVec3.Invalid;
            return false;
        }
    }
}
