using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace wyu.wyuCode.Relics;

public sealed class XvlaguAngry : wyuRelic
{
    // 遗物稀有度
    public override RelicRarity Rarity => RelicRarity.Common;

    // ---- 效果:打出能力牌后,下一张攻击牌的伤害 ×2 ----
    // 用"状态标记"而不是"锁定某张具体牌":
    // 卡面预览(UpdateCardPreview)和实际结算都走 ModifyDamageMultiplicative,
    // 只要标记还在,攻击牌的伤害预览就会实时显示 ×2,打出第一张攻击牌后标记被消耗。
    private bool _pendingDouble;

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        switch (cardPlay.Card.Type)
        {
            case CardType.Power:   // 打出能力牌 → 给下一张攻击牌上标记
                _pendingDouble = true;
                break;
            case CardType.Attack:  // 攻击牌已打出 → 消耗掉标记
                _pendingDouble = false;
                break;
        }
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        // 没有标记 → 不翻倍
        if (!_pendingDouble) return 1m;
        // 只有持有者自己打出的攻击牌伤害才翻倍
        if (dealer != Owner.Creature) return 1m;
        if (cardSource == null || cardSource.Type != CardType.Attack) return 1m;
        return 2m;
    }
}