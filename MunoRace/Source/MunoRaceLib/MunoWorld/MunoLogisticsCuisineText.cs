namespace MunoRaceLib.MunoWorld
{
    //负责集中保存后勤管理员分子料理通讯文本。
    public static class MunoLogisticsCuisineText
    {
        public const string RequestText = "我们做了一份新的料理，你能尝尝吗？";
        public const string AcceptReplyText = "太好了，我们马上送过来。";
        public const string RejectReplyText = "真可惜，本来是想找其他人试试的。";
        public const string AwaitingTasteText = "料理应该已经送到了，请先尝尝看。";
        public const string PendingFeedbackText = "料理已经品尝过了吗？我想听听你的反馈。";
        public const string CooldownText = "暂时没有分子料理。";
        public const string DeliciousFeedbackReplyText = "噢！？真的吗，真是太好了，我一直担心做不好，拿不准口味，不过你这么说，我就放心了，谢谢你的品尝和反馈，如果下次还想吃的话，可以联系我噢。";
        public const string OrdinaryFeedbackReplyText = "啊...这样啊，好吧看来我这次没做好，下次我会努力做的更好的，把口味和口感做的更好一些，下次...可以再请你试试吧？";
        public const string AwfulFeedbackReplyText = "变质？很难吃吗？...非常抱歉，我...应该是没把握好这次料理的处理...非常抱歉，我会送给你们补偿的。";
        public const string DeliveryLetterLabel = "缪诺分子料理送达";
        public const string DeliveryLetterText = "后勤管理员送来了一份新的分子料理，希望你能品尝后给出反馈。";
        public const string ReplacementLetterLabel = "缪诺分子料理补发";
        public const string ReplacementLetterText = "后勤管理员发现料理似乎没有被正常品尝，于是补发了一份新的分子料理。";
        public const string FeedbackLetterLabel = "后勤管理员等待反馈";
        public const string FeedbackLetterText = "分子料理已经被品尝。后勤管理员希望你通过通讯台告诉她这次料理的口味如何。";
        public const string CompensationLetterLabel = "缪诺料理补偿送达";
        public const string CompensationLetterText = "后勤管理员为失败的分子料理送来了 10 份奢侈食物作为补偿。";
    }
}
