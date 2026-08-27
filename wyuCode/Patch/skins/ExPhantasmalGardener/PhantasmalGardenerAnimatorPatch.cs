using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;

namespace wyu.wyuCode.Patch;

/// <summary>
/// 修复幻象园丁(PhantasmalGardener)受击动画的空引用崩溃。
///
/// 原版 GenerateAnimator 的 Hit 分支 lambda 直接写：
///     GetPower&lt;SkittishPower&gt;().HasGainedBlockThisTurn
/// 没做 null 检查 —— 它假设 SkittishPower(胆怯,机制buff)永远在怪物身上。
///
/// 当「封锁」(XiaoKeSealPower) 把目标身上所有 Buff 物理移除(包括 SkittishPower)后，
/// 怪物受击触发 Hit 动画时 GetPower 返回 null，就抛 NullReferenceException 卡住。
///
/// 这里用完全相同的动画状态表，只把受击 lambda 改成 null 安全写法。
/// 这样封锁可以照常压制一切 buff（包括 SkittishPower），动画不再崩。
/// </summary>
[HarmonyPatch(typeof(PhantasmalGardener), nameof(PhantasmalGardener.GenerateAnimator))]
public static class PhantasmalGardenerAnimatorPatch
{
    private static bool Prefix(PhantasmalGardener __instance, MegaSprite controller, ref CreatureAnimator __result)
    {
        __result = Build(__instance, controller);
        return false; // 跳过原 GenerateAnimator
    }

    private static CreatureAnimator Build(PhantasmalGardener gardener, MegaSprite controller)
    {
        AnimState idle = new AnimState("idle_loop", isLooping: true);
        AnimState buff = new AnimState("buff");
        AnimState attack = new AnimState("attack");
        AnimState attackMulti = new AnimState("attack_multi");
        AnimState hurtExtended = new AnimState("hurt_extended");
        AnimState hurt = new AnimState("hurt");
        AnimState die = new AnimState("die");
        AnimState blockLoop = new AnimState("block_loop", isLooping: true);
        AnimState blockStart = new AnimState("block_start");
        AnimState blockEnd = new AnimState("block_end");

        buff.NextState = idle;
        attack.NextState = idle;
        attackMulti.NextState = idle;
        hurtExtended.NextState = idle;
        hurt.NextState = blockLoop;
        blockStart.NextState = blockLoop;
        blockEnd.NextState = idle;

        CreatureAnimator animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Cast", buff);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("AttackMulti", attackMulti);
        animator.AddAnyState("Dead", die);
        // 受击分支：null 安全 —— 封锁可能已移除 SkittishPower，GetPower 可能返回 null
        animator.AddAnyState("Hit", hurtExtended, () => gardener.Creature.GetPower<SkittishPower>()?.HasGainedBlockThisTurn != true);
        animator.AddAnyState("Hit", hurt, () => gardener.Creature.GetPower<SkittishPower>()?.HasGainedBlockThisTurn == true);
        animator.AddAnyState("BlockStart", blockStart);
        animator.AddAnyState("BlockEnd", blockEnd);
        return animator;
    }
}
