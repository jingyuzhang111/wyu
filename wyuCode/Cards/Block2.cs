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
using MegaCrit.Sts2.Core.Entities.Powers;



namespace wyu.wyuCode.Cards;

public class Block2():
    wyuCard(cost: 1, 
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;
    public override bool GainsBlock => true;

    // 添加打击标签(Strike)
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(10, ValueProp.Move),
        new DynamicVar("BufferDeath", 1),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [

    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = base.Owner.Creature;
        var powers = creature.Powers.ToList();
        foreach (var power in powers)
        {
            if (power.Type == PowerType.Debuff && power.StackType == PowerStackType.Counter){
                await PowerCmd.ModifyAmount(choiceContext, power, 
                                            -DynamicVars["BufferDeath"].BaseValue, 
                                            creature, this, silent: true);
            }
            // 负的力量/敏捷归正：最多归到 0（不会产生正增益）
            if ((power is StrengthPower or DexterityPower) && power.Amount < 0)
            {
                decimal correction = DynamicVars["BufferDeath"].BaseValue;
                decimal target = Math.Min(0m, power.Amount + correction); // 上限钳到 0
                await PowerCmd.ModifyAmount(choiceContext, power, target - power.Amount, creature, this, silent: true);
            }
        }
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["BufferDeath"].UpgradeValueBy(1m);
    }


}