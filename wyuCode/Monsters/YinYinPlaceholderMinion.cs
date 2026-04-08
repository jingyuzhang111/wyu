using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace wyu.wyuCode.Monsters;

public sealed class YinYinPlaceholderMinion : CustomMonsterModel
{
    public override LocString Title => new("monsters", "WYU-YIN_YIN_PLACEHOLDER_MINION.name");

    // 怪物视觉必须是 .tscn 场景；若给 png 会在加载时触发 InvalidCastException。
    // public override string CustomVisualPath => "res://scenes/creature_visuals/big_dummy.tscn";
    public override string CustomVisualPath => "res://wyu/Scenes/wyuVisual.tscn";

    // 显式创建视觉节点
    public override NCreatureVisuals? CreateCustomVisuals()
    {
        return GodotUtils.CreatureVisualsFromScene(CustomVisualPath);
    }

    public override int MinInitialHp => 20;

    public override int MaxInitialHp => 20;

    // 由召唤方在生成时写入：该爪牙受伤后会把等量伤害传给此领袖。
    public Creature? Leader { get; set; }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        if (Leader == null)
        {
            return;
        }

        NCreature? leaderNode = NCombatRoom.Instance?.GetCreatureNode(Leader);
        NCreature? selfNode = NCombatRoom.Instance?.GetCreatureNode(base.Creature);
        if (leaderNode == null || selfNode == null)
        {
            return;
        }

        // 敌方爪牙固定在领袖正上方，避免出生在场景中心。
        // selfNode.GlobalPosition = leaderNode.GlobalPosition + Vector2.Up * 240f;
        selfNode.GlobalPosition = leaderNode.GlobalPosition + Vector2.Right * 140f + Vector2.Up * 240f;
    }

    // 行为状态机
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState idleMove = new("NOTHING", NothingMove, new HiddenIntent());
        idleMove.FollowUpState = idleMove;
        return new MonsterMoveStateMachine(new List<MonsterState> { idleMove }, idleMove);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 敌方回合结束后立即移除，实现“只存在一回合”。
        if (side == CombatSide.Enemy && base.Creature.IsAlive)
        {
            await CreatureCmd.Kill(base.Creature, force: true);
        }
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Creature || Leader == null || !Leader.IsAlive)
        {
            return;
        }

        // 只把实际掉血量传给领袖，避免把被格挡部分也转移过去。
        if (result.UnblockedDamage <= 0)
        {
            return;
        }

        await CreatureCmd.Damage(choiceContext, Leader, result.UnblockedDamage,
            ValueProp.Unblockable | ValueProp.Unpowered, dealer ?? base.Creature, null);
    }

    private static Task NothingMove(IReadOnlyList<Creature> _)
    {
        // 占位动作：不攻击、不施法，仅占位。
        return Task.CompletedTask;
    }
}
