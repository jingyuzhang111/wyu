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
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;


using MegaCrit.Sts2.Core.Helpers;
using wyu.wyuCode.Powers;
using BaseLib.Patches.Features;
using wyu.wyuCode.Patch;

namespace wyu.wyuCode.Cards;

public class All():
    wyuCard(cost: 0, 
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: CustomTargetType.Anyone
    ),
    IFullArtCard
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;

    // 按目标分池,保证不冲突：
    // - 目标是怪物 → 白名单：通用 buff + 通用 debuff(全部直接读代码验证过,无 Owner.Player 访问、无怪物强转,
    //   打任何怪物都安全)。
    // - 目标是玩家 → 黑名单：收集所有原版 power(各种角色能力/机器能力/debuff),只排除危险项。

    // 怪物目标：通用 buff(效果已逐个读源码确认)
    private static readonly HashSet<Type> MonsterBuffs = new()
    {
        typeof(StrengthPower),     // 力量:造成攻击伤害 +Amount
        typeof(DexterityPower),    // 敏捷:获得格挡 +Amount
        typeof(ThornsPower),       // 荆棘:受到攻击时反击 Amount 伤害给攻击者
        typeof(RegenPower),        // 再生:回合结束时回复 Amount 生命,层数-1
        typeof(BlurPower),         // 保留格挡:回合结束格挡不清空,层数-1
        typeof(BufferPower),       // 缓冲:抵挡下一次受到的伤害,层数-1
        typeof(RitualPower),       // 仪式:回合结束时 +Amount 力量
        typeof(IntangiblePower),   // 虚体:受到的伤害上限为 1
        typeof(FlameBarrierPower), // 火焰屏障:当回合受击反伤 Amount,回合结束移除
        typeof(EnvenomPower),      // 涂毒:造成攻击伤害后给目标 +Amount 中毒
        typeof(NoxiousFumesPower), // 毒雾:回合开始给敌方全体 +Amount 中毒
    };

    // 怪物目标：通用 debuff(效果已逐个读源码确认)
    private static readonly HashSet<Type> MonsterDebuffs = new()
    {
        typeof(VulnerablePower),  // 易伤:受到的攻击伤害 ×1.5
        typeof(WeakPower),        // 虚弱:造成的攻击伤害 ×0.75
        typeof(FrailPower),       // 脆弱:获得的格挡 ×0.75
        typeof(PoisonPower),      // 中毒:回合结束受到 Amount 点伤害,层数-1
        typeof(SlowPower),        // 迟缓:每打出一张牌,持有者受到的伤害 +10%
        typeof(ConstrictPower),   // 束缚:回合结束受到 Amount 点伤害
        typeof(ImbalancedPower),  // 失衡:攻击被完全格挡则眩晕
        typeof(DebilitatePower),  // 削弱:易伤/虚弱效果增强
        typeof(ManglePower),      // 残废:暂时降低力量(回合结束恢复)
    };

    private static List<PowerModel>? _monsterPool;
    private static List<PowerModel> GetMonsterPool()
    {
        if (_monsterPool == null)
        {
            _monsterPool = ModelDb.AllPowers
                .Where(p => MonsterBuffs.Contains(p.GetType()) || MonsterDebuffs.Contains(p.GetType()))
                .ToList();
        }
        return _monsterPool;
    }

    // 对“玩家目标”不可用的 power：
    // - 内部强转/读取 Owner.Monster 或 Applier.Monster(玩家没有 Monster → NRE/强转崩)
    // - 怪物专属的死亡召唤/复活/阻止战斗结束/标记召唤物机制(打到玩家会软锁或召唤怪物)
    private static readonly HashSet<Type> UnusableOnPlayer = new()
    {
        typeof(AsleepPower),        // 强转 LagavulinMatriarch
        typeof(SlumberPower),       // 强转 SlumberingBeetle
        typeof(FlutterPower),       // 读 Owner.Monster
        typeof(RavenousPower),      // 强转 CorpseSlug
        typeof(ShriekPower),        // 强转 TerrorEel
        typeof(SteamEruptionPower), // 死亡时强转 WaterfallGiant
        typeof(ReattachPower),      // Owner.Monster.SetMoveImmediate
        typeof(IllusionPower),      // Owner.Monster.MoveStateMachine
        typeof(BarricadePower),     // Applier.Monster.Title → NRE
        typeof(ShrinkPower),        // Applier.Monster.Title → NRE
        typeof(AdaptablePower),     // 怪物复活机制
        typeof(InfestedPower),      // 死亡召唤 + 阻止战斗结束
        typeof(StockPower),         // 死亡召唤 + 阻止战斗结束
        typeof(SurprisePower),      // 死亡召唤 + 阻止战斗结束
        typeof(MinionPower),        // 标记为召唤物
        typeof(PainfulStabsPower),  // 死亡时从战斗移除
        typeof(DieForYouPower),     // 宠物替身机制
    };

    // 玩家 buff 池：黑名单方案 —— 程序化收集所有原版 power(能力型 buff + debuff + 各种机器能力,自动跟随游戏更新),
    // 只排除 UnusableOnPlayer(怪物强转/读 Owner.Monster/软锁战斗等),其余全收,越乱越有意思。
    private static List<PowerModel>? _playerPool;
    private static List<PowerModel> GetPlayerPool()
    {
        if (_playerPool == null)
        {
            _playerPool = ModelDb.AllPowers
                .Where(p => p.GetType().Assembly == typeof(PowerModel).Assembly)         // 只原版
                .Where(p => p.GetType().Namespace == "MegaCrit.Sts2.Core.Models.Powers") // 排除 Mocks
                .Where(p => p.Type != PowerType.None)                                    // 排除占位类型
                .Where(p => !UnusableOnPlayer.Contains(p.GetType()))                     // 黑名单
                .ToList();
        }
        return _playerPool;
    }

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("PowerNum", 10),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        // 按目标分池：怪物走白名单(通用 buff + debuff)；玩家走黑名单(收所有原版 power,排除危险项)
        List<PowerModel> candidates = cardPlay.Target.IsMonster
            ? GetMonsterPool().ToList()
            : GetPlayerPool().ToList();
        var rng = base.Owner.RunState.Rng.CombatTargets;
        rng.Shuffle(candidates);                   // 原地洗牌,需要 IList
        var picked = candidates.Take(base.DynamicVars["PowerNum"].IntValue).ToList();

        foreach (var power in picked)
        {
            await PowerCmd.Apply(choiceContext, power.ToMutable(), cardPlay.Target, 1m, base.Owner.Creature, this);
        }

    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars["PowerNum"].UpgradeValueBy(5m);
    }

}