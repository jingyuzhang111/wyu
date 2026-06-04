using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace wyu.wyuCode.Cards;

public class Zc325SovereignBlade() :
    wyuCard(cost: 0, type: CardType.Attack, rarity: CardRarity.Token, target: TargetType.AnyEnemy)
{
    private const string _sfx = "event:/sfx/characters/regent/regent_sovereign_blade";

    protected override IEnumerable<string> ExtraRunAssetPaths => NSovereignBladeVfx.AssetPaths;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10, ValueProp.Move),
    ];


    // 打出：大剑飞向目标造成伤害
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var attack = DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this)
            .WithAttackerAnim("Cast", base.Owner.Character.AttackAnimDelay)
            .WithAttackerFx(null, _sfx)
            .Targeting(cardPlay.Target!)
            .BeforeDamage(delegate
            {
                var vfx = GetVfxNode(base.Owner, this);
                var targetNode = NCombatRoom.Instance?.GetCreatureNode(cardPlay.Target);
                if (vfx != null && targetNode != null)
                    vfx.Attack(targetNode.VfxSpawnPosition);
                return Task.CompletedTask;
            });
        await attack.Execute(choiceContext);
    }

    // 入牌堆时生成环绕大剑 VFX，消耗时移除
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card != this) return Task.CompletedTask;
        if (oldPileType == PileType.None || oldPileType == PileType.Exhaust)
            PlayCombatRoomForgeVfx(base.Owner, this);
        if (card.Pile?.Type == PileType.Exhaust)
            GetVfxNode(base.Owner, this)?.RemoveSovereignBlade();
        return Task.CompletedTask;
    }

    // ---------- VFX 相关 ----------

    public static NSovereignBladeVfx? GetVfxNode(Player player, CardModel card)
    {
        var original = card.DupeOf ?? card;
        return NCombatRoom.Instance?.GetCreatureNode(player.Creature)
            ?.GetChildren().OfType<NSovereignBladeVfx>()
            .FirstOrDefault(b => b.Card == original);
    }

    public static void PlayCombatRoomForgeVfx(Player player, CardModel card)
    {
        var nCreature = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
        if (nCreature == null) return;

        var vfx = GetVfxNode(player, card);
        bool isNew = vfx == null;
        if (isNew)
        {
            vfx = NSovereignBladeVfx.Create(card);
            nCreature.AddChildSafely(vfx);
            vfx.Position = Vector2.Zero;

            // 剑骨上挂 325.png
            var swordBone = vfx.GetNodeOrNull<Node2D>("SpineSword/SwordBone");
            if (swordBone != null)
            {
                var tex = ResourceLoader.Load<Texture2D>("res://src/zc/325.png");
                if (tex != null)
                    swordBone.AddChild(new Sprite2D
                    {
                        Name = "Zc325Overlay", Texture = tex, Centered = true,
                        Position = Vector2.Zero, Scale = Vector2.One * 0.8f, ZIndex = 10,
                    });
            }

            SfxCmd.Play("event:/sfx/characters/regent/regent_forge");
        }
        else
        {
            SfxCmd.Play("event:/sfx/characters/regent/regent_refine");
        }

        if (vfx == null) return;

        vfx.Forge(card.DynamicVars.Damage.IntValue, isNew);

        // Forge 之后再隐藏原版大剑模型（Forge 会重设可见性，必须在之后执行）
        if (isNew)
            HideOriginalSwordVisuals(vfx);
    }

    private static void HideOriginalSwordVisuals(NSovereignBladeVfx vfx)
    {
        // ScaleContainer 内：隐藏贴图，保留 SpawnFlames/ForgeSparks 粒子
        var scaleContainer = vfx.GetNodeOrNull<Node2D>("SpineSword/SwordBone/ScaleContainer");
        if (scaleContainer != null)
            foreach (var child in scaleContainer.GetChildren())
            {
                if (child is GpuParticles2D gp)
                {
                    var n = child.Name.ToString();
                    if (n == "SpawnFlames" || n == "SpawnFlamesBack" || n == "ForgeSparks") continue;
                    gp.Visible = false;
                }
                else if (child is CanvasItem ci)
                    ci.Visible = false;
            }

        // SpineSword 下隐藏脊骨模型（保留 SwordBone）
        var spineSword = vfx.GetNodeOrNull<Node2D>("SpineSword");
        if (spineSword != null)
            foreach (var child in spineSword.GetChildren())
            {
                if (child.Name.ToString() is "SwordBone" or "SlashParticles") continue;
                if (child is CanvasItem ci) ci.Visible = false;
            }

    }

    // ---------- 工具 ----------

    public static async Task<IEnumerable<CardModel>> CreateInHand(Player owner, int count, CombatState combatState)
    {
        if (count <= 0 || combatState == null || CombatManager.Instance.IsOverOrEnding)
            return [];
        var blades = new List<CardModel>();
        for (int i = 0; i < count; i++)
            blades.Add(combatState.CreateCard<Zc325SovereignBlade>(owner));
        await CardPileCmd.AddGeneratedCardsToCombat(blades, PileType.Hand, addedByPlayer: true);
        return blades;
    }
}
