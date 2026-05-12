using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace wyu.wyuCode.Powers;

public class XiaoKeSealPower : wyuPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private readonly Dictionary<ModelId, int> _suppressedBuffs = new();

    private bool _isRestoring;

    public override async Task AfterApplied(MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier, MegaCrit.Sts2.Core.Models.CardModel? cardSource)
    {
        await SuppressCurrentBuffs();
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier, MegaCrit.Sts2.Core.Models.CardModel? cardSource)
    {
        if (_isRestoring || Amount <= 0)
        {
            return;
        }

        // While seal is active, immediately suppress any newly gained buff on the same owner.
        if (power != this 
        && power.Owner == Owner 
        && power.Type == PowerType.Buff 
        && power is not SandpitPower 
        && power.Amount > 0)
        {
            await SuppressCurrentBuffs();
        }
    }

    private async Task SuppressCurrentBuffs()
    {
        List<PowerModel> buffs = Owner.Powers.Where(p => p != this && p.Type == PowerType.Buff && p is not SandpitPower).ToList();
        foreach (PowerModel power in buffs)
        {
            if (_suppressedBuffs.TryGetValue(power.Id, out int existing))
            {
                _suppressedBuffs[power.Id] = existing + power.Amount;
            }
            else
            {
                _suppressedBuffs[power.Id] = power.Amount;
            }

            await PowerCmd.Remove(power);
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // Consume one stack at end of owner's side. Restore all buffs when the last stack expires.
        if (side != Owner.Side)
        {
            return;
        }
        if (Amount > 1)
        {
            await PowerCmd.ModifyAmount(this, -1m, Applier, null, silent: true);
            return;
        }

        // await PowerCmd.TickDownDuration(this);  // 自动衰减层数，这么好用的东西，我之前都做的什么依托。
        await PowerCmd.Remove(this);
        await RestoreSuppressedBuffs();
    }

    public override async Task AfterRemoved(MegaCrit.Sts2.Core.Entities.Creatures.Creature oldOwner)
    {
        await RestoreSuppressedBuffs();
    }

    private async Task RestoreSuppressedBuffs()
    {
        if (_suppressedBuffs.Count == 0)
        {
            return;
        }

        _isRestoring = true;
        try
        {
            foreach (KeyValuePair<ModelId, int> entry in _suppressedBuffs)
            {
                if (entry.Value <= 0)
                {
                    continue;
                }

                PowerModel? canonical = ModelDb.GetByIdOrNull<PowerModel>(entry.Key);
                if (canonical == null)
                {
                    continue;
                }

                await PowerCmd.Apply(canonical.ToMutable(), Owner, entry.Value, Applier, null, silent: true);
            }
        }
        finally
        {
            _suppressedBuffs.Clear();
            _isRestoring = false;
        }
    }

}
