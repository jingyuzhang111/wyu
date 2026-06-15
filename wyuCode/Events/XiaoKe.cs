using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.ValueProps;
using wyu.wyuCode.Cards;
using wyu.wyuCode.Enchantments;

namespace wyu.wyuCode.Events;

public class XiaoKe : wyuEvent
{

    private int leavecount = 0;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Unblockable | ValueProp.Unpowered)
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new List<EventOption>
        {
            // 选项1: 给她蜜饼 → 给一张攻击牌附魔
            new(this, LOVE, Opt("INITIAL", "give_honey_cake"),
                HoverTipFactory.FromEnchantment<XiaoKeEnchantment>()),
            // 选项2: 离开 → 扣血，然后再次选择
            new(this, LEAVE, Opt("INITIAL", "leave")),
        };
    }

    // ---------- 回调 ----------
    private async Task LOVE()
    {
        var enchantment = ModelDb.Enchantment<XiaoKeEnchantment>();
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1);
        var card = (await CardSelectCmd.FromDeckForEnchantment(
            base.Owner, enchantment, 50,
            (CardModel? c) => c?.Type == CardType.Attack && enchantment.CanEnchant(c),
            prefs)).FirstOrDefault();

        if (card != null)
            CardCmd.Enchant<XiaoKeEnchantment>(card, 1);

        SetEventFinished(Desc("LOVE", "card"));
    }

    private async Task LEAVE()
    {
        // 效果施加
        leavecount++;
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(), base.Owner.Creature,
            base.DynamicVars.Damage, null, null);

        // 二次选择：狠心离开 或 心软给蜜饼
        if (leavecount >= 10)
        {
            SetEventState(Desc("LEAVE"), new List<EventOption>
            {
                new(this, ReallyLeave, Opt("REALLY_LEAVE", "leave")),
            });
            return;
        }
        else
        {
            SetEventState(Desc("LEAVE"), new List<EventOption>
            {
                new(this, LOVE, Opt("LEAVE", "give_honey_cake")),
                new(this, LEAVE, Opt("INITIAL", "leave")),
            });
        }

    }

    private async Task ReallyLeave()
    {
        // CreateCard 只注册到 RunState，CardPileCmd.Add 才真正加入牌组
        var card = base.Owner.RunState.CreateCard<XiaoKeEat>(base.Owner);
        var result = await CardPileCmd.Add(card, PileType.Deck);
        CardCmd.PreviewCardPileAdd(new[] { result }, 2f);

        SetEventFinished(Desc("REALLY_LEAVE", "leave"));
    }
}
