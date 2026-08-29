using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Models;

namespace wyu.wyuCode.Patch;

/// <summary>
/// 让心烛(复制的怪物副本)永远不生成行动/意图。
///
/// 关键：必须用 Prefix 直接跳过原 RollMove —— 因为复制的怪物(如幽灵船)的状态机
/// 含 ConditionalBranchState，在副本上执行 RollMove 会抛 "No valid next state found"。
/// 所以直接不执行原方法，把 NextMove 固定为 NOTHING + HiddenIntent。
///
/// 心烛识别用静态标记集合(ShadowModels)，不依赖 Power——
/// 因为 Power 是在 CreatureCmd.Add 之后才挂的，而 RollMove 在 Add 内部就会执行。
/// </summary>
[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.RollMove))]
public static class YinYinIdleIntentPatch
{
    /// <summary>被标记为心烛的怪物模型（召唤时在 CloneMonster 里注册）。</summary>
    public static readonly HashSet<MonsterModel> ShadowModels = new();

    /// <summary>
    /// 在心烛的 RollMove 之前拦截并直接跳过原方法（返回 false）。
    /// 这样幽灵船的条件分支状态机不会被执行，也就不会崩溃。
    /// </summary>
    private static bool Prefix(MonsterModel __instance)
    {
        if (!ShadowModels.Contains(__instance))
        {
            return true; // 不是心烛，正常执行原 RollMove
        }

        // 生成一个"什么都不做"的移动：HiddenIntent 表示意图隐藏/无攻击
        MoveState idleMove = new("NOTHING", NothingMove, new HiddenIntent());
        idleMove.FollowUpState = idleMove;

        // 直接写 private set 的 NextMove
        var field = typeof(MonsterModel).GetField("<NextMove>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(__instance, idleMove);

        return false; // 跳过原 RollMove，避免幽灵船状态机崩溃
    }

    private static Task NothingMove(IReadOnlyList<Creature> _)
    {
        return Task.CompletedTask;
    }
}
