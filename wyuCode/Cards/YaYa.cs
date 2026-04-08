using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using wyu.wyuCode.Character;
using wyu.wyuCode.Extensions;
using wyu.wyuCode.Powers;
using MegaCrit.Sts2.Core.Logging;

namespace wyu.wyuCode.Cards;

public class YaYa() : wyuCard(
    cost: 1,
    type: CardType.Attack,
    rarity: CardRarity.Common,
    target: TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PositionalDamageVar(0m, ValueProp.Move, 20m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StealDefencePower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        decimal damage = ((PositionalDamageVar)DynamicVars.Damage).CalculateForTarget(Owner.Creature, cardPlay.Target);

        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }

    public static float GetDistanceRatioInViewport(Creature source, Creature target)
    {
        NCreature? sourceNode = NCombatRoom.Instance?.GetCreatureNode(source);
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (sourceNode == null || targetNode == null)
            return 0.5f;

        Vector2 sourceCenter = sourceNode.Hitbox.GlobalPosition + sourceNode.Hitbox.Size * 0.5f * sourceNode.Hitbox.Scale;
        Vector2 targetCenter = targetNode.Hitbox.GlobalPosition + targetNode.Hitbox.Size * 0.5f * targetNode.Hitbox.Scale;

        float distance = sourceCenter.DistanceTo(targetCenter);
        Vector2 viewportSize = sourceNode.GetViewport().GetVisibleRect().Size;
        float maxDistance = viewportSize.Length();

        if (maxDistance <= 0f)
            return 0.5f;

        return Mathf.Clamp(distance / maxDistance, 0f, 1f);
    }

    private sealed class PositionalDamageVar : DamageVar
    {
        private readonly decimal _maxBonus;

        public PositionalDamageVar(decimal damage, ValueProp props, decimal maxBonus)
            : base(damage, props)
        {
            // 12 最大额外伤害
            _maxBonus = maxBonus;
        }

        public decimal CalculateForTarget(Creature source, Creature? target)
        {
            if (target == null)
                return BaseValue;

            float ratio = GetDistanceRatioInViewport(source, target);
            float min = (float)BaseValue;
            float max = (float)(BaseValue + _maxBonus);
            Log.Info($"最大伤害: {max}, 最小伤害: {min}, 距离比例: {ratio}");
            return Mathf.RoundToInt(Mathf.Lerp(min, max, ratio));
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
        {
            decimal dynamicBase = CalculateForTarget(card.Owner.Creature, target);
            decimal preview = dynamicBase;

            EnchantmentModel? enchantment = card.Enchantment;
            if (enchantment != null)
            {
                preview += enchantment.EnchantDamageAdditive(preview, Props);
                preview *= enchantment.EnchantDamageMultiplicative(preview, Props);
                if (!card.IsEnchantmentPreview)
                {
                    EnchantedValue = preview;
                }
            }

            if (runGlobalHooks)
            {
                preview = Hook.ModifyDamage(
                    card.Owner.RunState,
                    card.CombatState,
                    target,
                    card.Owner.Creature,
                    dynamicBase,
                    Props,
                    card,
                    ModifyDamageHookType.All,
                    previewMode,
                    out IEnumerable<AbstractModel> _);
            }

            PreviewValue = preview;
        }
    }
}
