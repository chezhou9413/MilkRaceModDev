using System;
using System.Collections.Generic;
using HarmonyLib;
using MunoRaceLib.MunoScenarios;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    //将原版商队和援军请求接入对应管理员，保留原版资格、费用和冷却。
    public static class MunoCommServiceMenu
    {
        private static readonly Func<Map, Faction, Pawn, DiaOption> TraderOption =
            AccessTools.MethodDelegate<Func<Map, Faction, Pawn, DiaOption>>(
                AccessTools.Method(typeof(FactionDialogMaker), "RequestTraderOption"));
        private static readonly Func<Map, Faction, Pawn, DiaOption> MilitaryOption =
            AccessTools.MethodDelegate<Func<Map, Faction, Pawn, DiaOption>>(
                AccessTools.Method(typeof(FactionDialogMaker), "RequestMilitaryAidOption"));

        //打开军事管理员的人口交换和军事支援菜单。
        public static void OpenMilitary(Pawn negotiator)
        {
            Open(negotiator, true, null);
        }

        //打开后勤管理员的商队申请和分子料理菜单。
        public static void OpenLogistics(Pawn negotiator, Action openCuisine)
        {
            Open(negotiator, false, openCuisine);
        }

        //敌对期间暂停常规合作，融入申请仍由聚落管理员处理。
        public static bool CanUsePeacefulServices()
        {
            Faction faction = MunoStoryUtility.Faction;
            if (faction != null && !faction.defeated && !faction.deactivated && !faction.HostileTo(Faction.OfPlayer))
                return true;
            Messages.Message("缪诺聚落当前无法办理常规合作事务，请先恢复和平关系。", MessageTypeDefOf.RejectInput, false);
            return false;
        }

        //创建会自动暂停的管理员对话，并将原版返回选项接回当前管理员。
        private static void Open(Pawn negotiator, bool military, Action openCuisine)
        {
            Faction faction = MunoStoryUtility.Faction;
            if (negotiator?.Map == null || faction == null || faction.defeated || faction.deactivated)
            {
                Messages.Message("当前无法联络缪诺管理员。", MessageTypeDefOf.RejectInput, false);
                return;
            }
            Find.WindowStack.Add(new Dialog_MunoCommNodeTree(BuildMenu(negotiator, faction, military, openCuisine),
                military ? "缪诺军事管理员" : "缪诺后勤管理员",
                military ? "UI/MunoCommPortrait" : "UI/MunoMarriagePortrait"));
        }

        //每次返回时重新读取好感和冷却，避免继续使用已经过期的请求选项。
        private static DiaNode BuildMenu(Pawn negotiator, Faction faction, bool military, Action openCuisine)
        {
            var root = new DiaNode(military ? "请选择需要办理的军事事务。" : "请选择需要办理的后勤事务。");
            var service = new DiaOption(military ? "收集当地人口" : "分子料理试吃")
            {
                resolveTree = true,
                action = () =>
                {
                    if (!CanUsePeacefulServices()) return;
                    if (military) Find.WindowStack.Add(new Dialog_MunoHostageExchange(negotiator, true));
                    else openCuisine();
                }
            };
            if (faction.HostileTo(Faction.OfPlayer)) service.Disable("敌对期间暂停");
            root.options.Add(service);

            Map map = negotiator.Map;
            DiaOption request;
            if (map == null || !map.IsPlayerHome)
            {
                request = new DiaOption(military ? "请求军事支援" : "申请商队");
                request.Disable("只能从玩家基地发起请求");
            }
            else
            {
                request = military ? MilitaryOption(map, faction, negotiator) : TraderOption(map, faction, negotiator);
                if (negotiator.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
                    request.Disable("WorkTypeDisablesOption".Translate(SkillDefOf.Social.label));
                else if (!negotiator.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
                    request.Disable("操作者无法交谈");
                BindReturns(request, () => BuildMenu(negotiator, faction, military, openCuisine), new HashSet<DiaNode>());
            }
            root.options.Add(request);
            root.options.Add(new DiaOption("返回通讯终端") { resolveTree = true });
            return root;
        }

        //原版请求的确认、取消和完成分支统一返回所属管理员。
        private static void BindReturns(DiaOption option, Func<DiaNode> menu, HashSet<DiaNode> visited)
        {
            if (option.linkLateBind != null) option.linkLateBind = menu;
            if (option.link == null || !visited.Add(option.link)) return;
            foreach (DiaOption child in option.link.options) BindReturns(child, menu, visited);
        }
    }
}
