using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace wyu.wyuCode.Cards;

/// <summary>
/// Zc325 特殊版君王之剑，使用 325.png 作为贴图。复用 NSovereignBladeVfx 的环绕/Forge/Attack 机制。
/// </summary>Zc325SovereignBlade
public class Zc325SovereignBlade() :
    wyuCard(cost: 0,
    type: CardType.Attack,
    rarity: CardRarity.Token,
    target: TargetType.AnyEnemy)
{
    private const int _baseDamage = 10;
    private const string _sovereignBladeSfx = "event:/sfx/characters/regent/regent_sovereign_blade";

    private bool _createdThroughForge;
    private decimal _currentDamage = 10m;
    private decimal _currentRepeats = 1m;

    public override TargetType TargetType
    {
        get
        {
            if (!HasSeekingEdge)
                return TargetType.AnyEnemy;
            return TargetType.AllEnemies;
        }
    }

    protected override IEnumerable<string> ExtraRunAssetPaths => NSovereignBladeVfx.AssetPaths;

    private decimal CurrentDamage
    {
        get => _currentDamage;
        set
        {
            AssertMutable();
            _currentDamage = value;
        }
    }

    private decimal CurrentRepeats
    {
        get => _currentRepeats;
        set
        {
            AssertMutable();
            _currentRepeats = value;
        }
    }

    public bool CreatedThroughForge
    {
        get => _createdThroughForge;
        set
        {
            AssertMutable();
            _createdThroughForge = value;
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("SeekingEdgeAmount").WithMultiplier((CardModel card, Creature? _) => (card != null && card.IsMutable && card.Owner != null) ? card.Owner.Creature.GetPowerAmount<SeekingEdgePower>() : 0),
        new RepeatVar(1)
    };

    private bool HasSeekingEdge
    {
        get
        {
            if (CombatManager.Instance.IsInProgress)
                return base.Owner.Creature.HasPower<SeekingEdgePower>();
            return false;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        AttackCommand attack = DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).WithHitCount(base.DynamicVars.Repeat.IntValue)
            .WithAttackerAnim("Cast", base.Owner.Character.AttackAnimDelay)
            .WithAttackerFx(null, _sovereignBladeSfx);
        
        if (HasSeekingEdge)
        {
            attack = attack.TargetingAllOpponents(base.CombatState).BeforeDamage(delegate
            {
                NSovereignBladeVfx vfxNode = GetVfxNode(base.Owner, this);
                IReadOnlyList<Creature> hittableEnemies = base.CombatState.HittableEnemies;
                if (hittableEnemies.Count > 0)
                {
                    NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(hittableEnemies[0]);
                    if (vfxNode != null && nCreature != null)
                    {
                        vfxNode.Attack(nCreature.VfxSpawnPosition);
                    }
                }
                return Task.CompletedTask;
            }).WithHitFx("vfx/vfx_giant_horizontal_slash", null, "slash_attack.mp3");
        }
        else
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
            attack = attack.Targeting(cardPlay.Target).BeforeDamage(delegate
            {
                NSovereignBladeVfx vfxNode = GetVfxNode(base.Owner, this);
                NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(cardPlay.Target);
                if (vfxNode != null && nCreature != null)
                {
                    vfxNode.Attack(nCreature.VfxSpawnPosition);
                }
                return Task.CompletedTask;
            }).WithHitVfxNode((Creature t) => NBigSlashVfx.Create(t))
                .WithHitVfxNode((Creature t) => NBigSlashImpactVfx.Create(t));
        }
        await attack.Execute(choiceContext);
        
        ParryPower power = base.Owner.Creature.GetPower<ParryPower>();
        if (power != null)
        {
            await power.AfterSovereignBladePlayed(base.Owner.Creature, attack.Results);
        }
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        CreatedThroughForge = false;
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        base.DynamicVars.Damage.BaseValue = CurrentDamage;
        base.DynamicVars.Repeat.BaseValue = CurrentRepeats;
    }

    public override void AfterTransformedFrom()
    {
        RemoveSovereignBladeNode();
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card != this)
            return Task.CompletedTask;

        if ((!CreatedThroughForge && oldPileType == PileType.None) || oldPileType == PileType.Exhaust)
        {
            PlayCombatRoomForgeVfx(base.Owner, this);
        }

        if (card.Pile.Type == PileType.Exhaust)
        {
            RemoveSovereignBladeNode();
        }

        return Task.CompletedTask;
    }

    public void AddDamage(decimal amount)
    {
        base.DynamicVars.Damage.BaseValue += amount;
        CurrentDamage = base.DynamicVars.Damage.BaseValue;
    }

    public void SetRepeats(decimal amount)
    {
        base.DynamicVars.Repeat.BaseValue = amount;
        CurrentRepeats = base.DynamicVars.Repeat.BaseValue;
    }

    public static NSovereignBladeVfx? GetVfxNode(Player player, CardModel card)
    {
        CardModel originalCard = card.DupeOf ?? card;
        return (NCombatRoom.Instance?.GetCreatureNode(player.Creature))?.GetChildren().OfType<NSovereignBladeVfx>().FirstOrDefault((NSovereignBladeVfx b) => b.Card == originalCard);
    }

    public static void PlayCombatRoomForgeVfx(Player player, CardModel card)
    {
        NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (nCreature == null)
        {
            return;
        }

        NSovereignBladeVfx? vfxNode = GetVfxNode(player, card);
        bool isNew = vfxNode == null;
        if (isNew)
        {
            vfxNode = NSovereignBladeVfx.Create(card);
            nCreature.AddChildSafely(vfxNode);
            vfxNode.Position = Vector2.Zero;

            // 在剑骨上附加 325.png 作为替代
            var swordBone = vfxNode.GetNodeOrNull<Node2D>("SpineSword/SwordBone");
            if (swordBone != null)
            {
                var tex = ResourceLoader.Load<Texture2D>("res://src/zc/325.png");
                if (tex != null)
                {
                    var overlay = new Sprite2D
                    {
                        Name = "Zc325Overlay",
                        Texture = tex,
                        Centered = true,
                        Position = Vector2.Zero,
                        Scale = Vector2.One * 0.8f,
                        // Rotation = -Mathf.Pi / 2f,
                        ZIndex = 10,
                    };
                    swordBone.AddChild(overlay);
                }
            }

            SfxCmd.Play("event:/sfx/characters/regent/regent_forge");
        }
        else
        {
            SfxCmd.Play("event:/sfx/characters/regent/regent_refine");
        }

        vfxNode.Forge(card.DynamicVars.Damage.IntValue, isNew);

        // 隐藏原版大剑贴图/模型，保留入场火焰、Forge火花等粒子效果
        if (isNew)
        {
            // ScaleContainer 内的贴图节点隐藏，粒子发射器保留
            var scaleContainer = vfxNode.GetNodeOrNull<Node2D>("SpineSword/SwordBone/ScaleContainer");
            if (scaleContainer != null)
            {
                foreach (var child in scaleContainer.GetChildren())
                {
                    if (child is GpuParticles2D gp)
                    {
                        // 只保留入场火焰和锻造火花，其他常亮粒子隐藏
                        var name = child.Name.ToString();
                        if (name == "SpawnFlames" || name == "SpawnFlamesBack" || name == "ForgeSparks")
                            continue;
                        gp.Visible = false;
                        continue;
                    }
                    if (child is CanvasItem ci)
                        ci.Visible = false; // 隐藏：Hilt, Detail, BladeGlow 等
                }
            }

            // 隐藏 Spine 渲染的剑刃模型（SpineSword 下除 SwordBone 外的子节点）
            var spineSword = vfxNode.GetNodeOrNull<Node2D>("SpineSword");
            if (spineSword != null)
            {
                foreach (var child in spineSword.GetChildren())
                {
                    if (child.Name == "SwordBone" || child.Name == "SlashParticles")
                        continue;
                    if (child is CanvasItem ci)
                        ci.Visible = false;
                }
            }

            // 斩击粒子（攻击时触发，不属于入场效果）
            var slashParticles = vfxNode.GetNodeOrNull<GpuParticles2D>("SpineSword/SlashParticles");
            if (slashParticles != null)
                slashParticles.Visible = false;

            // 拖尾线条（攻击时触发）
            var trail = vfxNode.GetNodeOrNull<Line2D>("Trail");
            if (trail != null)
                trail.Visible = false;
        }
    }

    private void RemoveSovereignBladeNode()
    {
        GetVfxNode(base.Owner, this)?.RemoveSovereignBlade();
    }

    public static async Task<IEnumerable<CardModel>> CreateInHand(Player owner, int count, CombatState combatState)
    {
        if (count == 0 || combatState == null)
            return Array.Empty<CardModel>();
        
        if (CombatManager.Instance.IsOverOrEnding)
            return Array.Empty<CardModel>();

        var blades = new List<CardModel>();
        for (int i = 0; i < count; i++)
        {
            blades.Add(combatState.CreateCard<Zc325SovereignBlade>(owner));
        }
        
        await CardPileCmd.AddGeneratedCardsToCombat(blades, PileType.Hand, addedByPlayer: true);
        return blades;
    }
}
