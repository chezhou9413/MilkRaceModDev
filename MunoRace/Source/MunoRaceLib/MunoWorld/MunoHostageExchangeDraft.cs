using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    //保存通讯窗口内尚未提交的多人选择、奖励分配和锁定物资预览。
    public class MunoHostageExchangeDraft
    {
        private readonly List<Pawn> selectedPawns = new List<Pawn>();
        private readonly Dictionary<Pawn, MunoExchangeRewardType> rewardTypes = new Dictionary<Pawn, MunoExchangeRewardType>();
        private List<Thing> itemRewards = new List<Thing>();
        private float itemPawnValue;
        private bool previewAttempted;
        private string previewError;

        //返回已选择的 Pawn 列表。
        public List<Pawn> SelectedPawns => selectedPawns;

        //返回已经生成并锁定的随机物资。
        public List<Thing> ItemRewards => itemRewards;

        //返回随机物资对应的上交 Pawn 总市场价值。
        public float ItemPawnValue => itemPawnValue;

        //返回已经生成的随机物资总市场价值，用于奖励预览展示。
        public float ItemRewardMarketValue
        {
            get
            {
                float value = 0f;
                for (int i = 0; i < itemRewards.Count; i++)
                {
                    Thing item = itemRewards[i];
                    if (item != null && !item.Destroyed)
                    {
                        value += item.MarketValue * item.stackCount;
                    }
                }

                return value;
            }
        }

        //返回当前已选人数。
        public int SelectedCount => selectedPawns.Count;

        //清除已经失效的选择，并保持剩余人员的原有顺序。
        public void SyncCandidates(List<Pawn> candidates)
        {
            bool changed = false;
            for (int i = selectedPawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = selectedPawns[i];
                if (pawn == null || candidates == null || !candidates.Contains(pawn))
                {
                    selectedPawns.RemoveAt(i);
                    if (pawn != null)
                    {
                        rewardTypes.Remove(pawn);
                    }
                    changed = true;
                }
            }

            if (changed)
            {
                InvalidatePreview();
            }
        }

        //判断指定 Pawn 是否已被选中。
        public bool IsSelected(Pawn pawn)
        {
            return pawn != null && selectedPawns.Contains(pawn);
        }

        //设置指定 Pawn 的选中状态，并为新选择默认分配缪诺成员奖励。
        public void SetSelected(Pawn pawn, bool selected)
        {
            if (pawn == null || IsSelected(pawn) == selected)
            {
                return;
            }

            if (selected)
            {
                selectedPawns.Add(pawn);
                rewardTypes[pawn] = MunoExchangeRewardType.MunoPawn;
            }
            else
            {
                selectedPawns.Remove(pawn);
                rewardTypes.Remove(pawn);
            }

            InvalidatePreview();
        }

        //选中当前全部合法候选，并保留已选人员的奖励设置。
        public void SelectAll(List<Pawn> candidates)
        {
            if (candidates == null)
            {
                return;
            }

            bool changed = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                Pawn pawn = candidates[i];
                if (pawn == null || selectedPawns.Contains(pawn))
                {
                    continue;
                }

                selectedPawns.Add(pawn);
                rewardTypes[pawn] = MunoExchangeRewardType.MunoPawn;
                changed = true;
            }

            if (changed)
            {
                InvalidatePreview();
            }
        }

        //清空当前全部人员选择和奖励预览。
        public void ClearSelection()
        {
            if (selectedPawns.Count == 0)
            {
                return;
            }

            selectedPawns.Clear();
            rewardTypes.Clear();
            InvalidatePreview();
        }

        //返回指定 Pawn 当前分配的奖励类型。
        public MunoExchangeRewardType GetRewardType(Pawn pawn)
        {
            if (pawn != null && rewardTypes.TryGetValue(pawn, out MunoExchangeRewardType rewardType))
            {
                return rewardType;
            }

            return MunoExchangeRewardType.MunoPawn;
        }

        //设置指定 Pawn 的奖励类型，并使旧物资预览失效。
        public void SetRewardType(Pawn pawn, MunoExchangeRewardType rewardType)
        {
            if (pawn == null || !selectedPawns.Contains(pawn) || GetRewardType(pawn) == rewardType)
            {
                return;
            }

            rewardTypes[pawn] = rewardType;
            InvalidatePreview();
        }

        //将全部已选人员统一设置为指定奖励类型。
        public void SetAllRewardTypes(MunoExchangeRewardType rewardType)
        {
            bool changed = false;
            for (int i = 0; i < selectedPawns.Count; i++)
            {
                Pawn pawn = selectedPawns[i];
                if (GetRewardType(pawn) != rewardType)
                {
                    rewardTypes[pawn] = rewardType;
                    changed = true;
                }
            }

            if (changed)
            {
                InvalidatePreview();
            }
        }

        //统计指定奖励类型对应的已选人员数量。
        public int CountRewardType(MunoExchangeRewardType rewardType)
        {
            int count = 0;
            for (int i = 0; i < selectedPawns.Count; i++)
            {
                if (GetRewardType(selectedPawns[i]) == rewardType)
                {
                    count++;
                }
            }

            return count;
        }

        //构建可传递给地图或远行队交换服务的目标记录副本。
        public List<MunoExchangeTargetRecord> BuildTargetRecords()
        {
            List<MunoExchangeTargetRecord> records = new List<MunoExchangeTargetRecord>();
            for (int i = 0; i < selectedPawns.Count; i++)
            {
                Pawn pawn = selectedPawns[i];
                records.Add(new MunoExchangeTargetRecord(pawn, GetRewardType(pawn)));
            }

            return records;
        }

        //确保随机物资预览已经按当前奖励分配生成并锁定。
        public bool EnsureItemPreview(out string failReason)
        {
            failReason = null;
            if (CountRewardType(MunoExchangeRewardType.RandomItems) == 0)
            {
                DestroyPreviewItems();
                previewAttempted = true;
                previewError = null;
                itemPawnValue = 0f;
                return true;
            }

            if (previewAttempted)
            {
                failReason = previewError;
                return previewError.NullOrEmpty();
            }

            previewAttempted = true;
            List<MunoExchangeTargetRecord> records = BuildTargetRecords();
            if (!MunoExchangeRewardService.TryGenerateRandomItemReward(records, out itemRewards, out itemPawnValue, out previewError))
            {
                failReason = previewError;
                return false;
            }

            return true;
        }

        //销毁当前尚未提交的物资预览，供窗口关闭或重新分配奖励时调用。
        public void DisposePreview()
        {
            DestroyPreviewItems();
            previewAttempted = false;
            previewError = null;
            itemPawnValue = 0f;
        }

        //使当前物资预览失效，并允许下一次绘制重新生成。
        private void InvalidatePreview()
        {
            DisposePreview();
        }

        //销毁尚未被交换会话接管的物资对象。
        private void DestroyPreviewItems()
        {
            MunoExchangeRewardService.DestroyItems(itemRewards);
            itemRewards = new List<Thing>();
        }
    }
}
