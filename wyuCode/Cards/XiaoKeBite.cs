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


using MegaCrit.Sts2.Core.Helpers;
using wyu.wyuCode.Powers;
using wyu.wyuCode.Enchantments;



namespace wyu.wyuCode.Cards;

public class XiaoKeBite():
    wyuCard(cost: 3, 
    type: CardType.Attack,
    rarity: CardRarity.Common,
    target: TargetType.AnyEnemy
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;
    public override string mytype => "xiaoke";
    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12, ValueProp.Move),
        new DynamicVar("ExtraDamage", 0),
        new DynamicVar("BlocktoDamage", 0.5m),
        new DynamicVar("BlocktoDamagePct", 50m),
        new PowerVar<SiyeBitePower>(4m),
        new EnergyVar(1),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [

    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 卡牌效果的实现地方,在CommonActions里有一些写好的函数,如攻防抽牌烧牌
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");



        // 基础伤害：吃力量，不吃格挡。
        await CreatureCmd.Damage(
            choiceContext,
            cardPlay.Target,
            base.DynamicVars.Damage.BaseValue,
                ValueProp.Unblockable | ValueProp.Move,
                this);


        await PowerCmd.Apply<SiyeBitePower>(cardPlay.Target, base.DynamicVars["SiyeBitePower"].BaseValue, base.Owner.Creature, this);

        // 塞牌 虚无
        CardModel card = base.CombatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Void>(base.Owner);
		CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Discard, addedByPlayer: true));
		await Cmd.Wait(0.5f);


        await PowerCmd.Apply<EnergyNextTurnPower>(base.Owner.Creature, base.DynamicVars.Energy.BaseValue, base.Owner.Creature, this);

    }

    public override void AfterCreated()
    {
        base.AfterCreated();
        if (Enchantment is null)
        {
            CardCmd.Enchant<XiaoKeEnchantment>(this, DynamicVars["BlocktoDamagePct"].BaseValue);
        }
    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars["BlocktoDamage"].UpgradeValueBy(0.1m);
        DynamicVars["BlocktoDamagePct"].UpgradeValueBy(10m);
        DynamicVars["Damage"].UpgradeValueBy(4);
        if (Enchantment is XiaoKeEnchantment e)
        {
            e.Amount += 10;
            e.ModifyCard(); // 重新计算附魔和卡牌动态值
        }
    }


}