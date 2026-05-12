using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace wyu.wyuCode.Powers;

public class EwPower : wyuPower
{
    // -1: 未初始化; >0: 还需等待的回合数; 0: 当前回合开始可触发

    // 效果类型Buff, Debuff...
    public override PowerType Type => PowerType.Buff;

    // 效果堆叠类型 可堆叠与不可堆叠
	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player.Creature != base.Owner)
		{
			return;
		}

		CombatState? combatState = player.Creature.CombatState;
		if (combatState == null)
		{
			return;
		}

		bool flag;
		using (CardSelectCmd.PushSelector(new VakuuCardSelector()))
		{
			BlockingPlayerChoiceContext autoContext = new BlockingPlayerChoiceContext();
			int cardsPlayed;
			for (cardsPlayed = 0; cardsPlayed < 13; cardsPlayed++)
			{
				if (CombatManager.Instance.IsOverOrEnding)
				{
					break;
				}
				CardPile pile = PileType.Hand.GetPile(player);
				CardModel? card = pile.Cards.FirstOrDefault((CardModel c) => c.CanPlay());
				if (card == null)
				{
					break;
				}
				Creature? target = GetTarget(card, combatState, player);
				await card.SpendResources();
				await CardCmd.AutoPlay(autoContext, card, target, AutoPlayType.Default, skipXCapture: true);
			}
			flag = cardsPlayed >= 13;
		}

		LocString line = (flag ? new LocString("powers", "WYU-EW_POWER.warning") : new LocString("powers", "WYU-EW_POWER.approval"));
		TalkCmd.Play(line, base.Owner, vfxColor: VfxColor.White);
		await PowerCmd.TickDownDuration(this);
		PlayerCmd.EndTurn(player, canBackOut: false);
	}


	private Creature? GetTarget(CardModel card, CombatState combatState, Player player)
	{
		Rng combatTargets = player.RunState.Rng.CombatTargets;
		return card.TargetType switch
		{
			TargetType.AnyEnemy => combatState.HittableEnemies.FirstOrDefault(), 
			TargetType.AnyAlly => combatTargets.NextItem(combatState.Allies.Where((Creature c) => c != null && c.IsAlive && c.IsPlayer && c != base.Owner)), 
			TargetType.AnyPlayer => base.Owner, 
			_ => null, 
		};
	}
}