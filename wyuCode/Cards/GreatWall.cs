using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using wyu.wyuCode.Character;
using wyu.wyuCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;

using MegaCrit.Sts2.Core.Commands;

// 提供数值
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using MegaCrit.Sts2.Core.Helpers;



namespace wyu.wyuCode.Cards;

public class GreatWall():
    wyuCard(cost: 2, 
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.Self
    )
{
    private const decimal ReplyDivisor = 100000m;
    private static readonly HttpClient ReplyHttpClient = new();
    private static readonly object ReplyCacheLock = new();  // 锁对象
    private static decimal? ReplyCountCache;
    private static Task? ReplyCountWarmupTask;

    // 自定义边框
    // public override bool HasBuiltInOverlay => true;

    static GreatWall()
    {
        EnsureReplyCountWarmup();
    }

    // 添加打击标签(Strike)
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(50m, ValueProp.Move),
        new CalculationBaseVar(0m),
		new CalculationExtraVar(1m),
        // 读取缓存评论数
		new CalculatedVar("CalculatedBlock").WithMultiplier(BlockMultiplier),
    ];

    private static decimal BlockMultiplier(CardModel card, Creature? target)
    {
        EnsureReplyCountWarmup();

        decimal replyCount = ReplyCountCache ?? 0m;

        return Math.Max(0m, replyCount);
    }

    private static void EnsureReplyCountWarmup()
    {
        // 有值或者正在读取中,返回
        if (ReplyCountCache.HasValue || ReplyCountWarmupTask is not null)
        {
            return;
        }
        // 当评论数没有值时,进行异步值读取
        ReplyCountWarmupTask = WarmUpReplyCountAsync();
    }

    private static async Task WarmUpReplyCountAsync()
    {
        try
        {
            decimal replyCount = await LoadReplyCountAsync();

            lock (ReplyCacheLock)
            {
                ReplyCountCache = replyCount;
            }
        }
        catch (Exception e)
        {
            Log.Error($"评论数加载失败: {e.Message}");
        }
    }

    private static async Task<decimal> LoadReplyCountAsync()
    {
        // 读取评论数的地方
        // 访问视频
        string bvid = "BV1fy4y1L7Rq"; // 视频的BV号
        string url = $"https://api.bilibili.com/x/web-interface/view?bvid={bvid}";

        Log.Info("正在加载评论数...");

        ReplyHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

        HttpResponseMessage response = await ReplyHttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(responseBody);
        JsonElement root = document.RootElement;
        int code = root.GetProperty("code").GetInt32();

        if (code == 0) // 请求成功
        {
            decimal replyCount = root.GetProperty("data").GetProperty("stat").GetProperty("reply").GetInt64();
            Log.Info($"视频 {bvid} 的评论数为: {replyCount}");
            return replyCount/100000m; // 返回评论数除以10万
        }

        string? message = root.GetProperty("message").GetString();
        Log.Error($"API 请求失败: {message}");

        return 0m;

    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [

    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌
        // decimal replyBonus = await LoadReplyCountAsync(); // 直接取回数值，不回写动态变量

        // Log.Info($"格挡计算成功，当前格挡获取为{replyBonus}");

        await CreatureCmd.GainBlock(base.Owner.Creature, ReplyCountCache ?? 50m, ValueProp.Move, cardPlay);
    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }


}