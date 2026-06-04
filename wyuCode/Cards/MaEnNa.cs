using System.Collections.Generic;
using System.Threading.Tasks;
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

using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Godot;

namespace wyu.wyuCode.Cards;

public class MaEnNa():
    wyuCard(cost: 0,
    type: CardType.Attack,
    rarity: CardRarity.Rare,
    target: TargetType.AllAllies
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;

    /// <summary>累计攻击牌数是否已达标（达标后保持可打出，直到被打出）</summary>
    private bool _thresholdReached;

    protected override bool ShouldGlowGoldInternal => IsPlayable;
    protected override bool IsPlayable => _thresholdReached;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(45, ValueProp.Move),
        new CardsVar(20),
        new IntVar("attackCount", 0),   // 用于卡面显示当前攻击牌计数
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
    ];

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        // 本牌被打出 → 清空计数，重新开始累积
        if (cardPlay.Card == this)
        {
            _thresholdReached = false;
            DynamicVars["attackCount"].BaseValue = 0;
            return Task.CompletedTask;
        }

        // 不是自己的牌 → 忽略（联机场景）
        if (cardPlay.Card.Owner != base.Owner)
            return Task.CompletedTask;

        // 不是攻击牌 → 忽略
        if (cardPlay.Card.Type != CardType.Attack)
            return Task.CompletedTask;

        // 累计攻击牌 +1
        DynamicVars["attackCount"].BaseValue++;

        // 达到 CardsVar，标记为可打出
        if (DynamicVars["attackCount"].BaseValue >= DynamicVars["Cards"].BaseValue)
        {
            // DynamicVars["attackCount"].BaseValue = 0;
            _thresholdReached = true;
        }

        return Task.CompletedTask;
    }

    // ── 打出效果 ──

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(base.CombatState)
            .WithHitFx("vfx/vfx_attack_slash", null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars["Cards"].UpgradeValueBy(-5);
    }
}
