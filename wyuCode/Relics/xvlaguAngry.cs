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

    // ---- 效果:打出能力牌后,下一张攻击牌的伤害 ×2(参照游戏内置钢笔尖 PenNib 的实现) ----
    private bool _pendingDouble;           // 打出能力牌后置 true
    private CardModel? _attackToDouble;    // 锁定要翻倍的那张攻击牌

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 上一张牌已打出,清掉锁定
        _attackToDouble = null;

        // 打出能力牌 → 标记下一张攻击牌翻倍
        if (cardPlay.Card.Type == CardType.Power)
        {
            _pendingDouble = true;
        }
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (!_pendingDouble) return;
        if (cardPlay.Card.Type != CardType.Attack) return;  // 只有攻击牌才锁定,标志保留

        _pendingDouble = false;
        _attackToDouble = cardPlay.Card;   // 锁定这张攻击牌(整张伤害都翻倍)
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        // 只有持有者自己打出的、被锁定的那张攻击牌才翻倍
        if (dealer != Owner.Creature) return 1m;
        if (_attackToDouble != null && cardSource == _attackToDouble) return 2m;
        return 1m;
    }
}