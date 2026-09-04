using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace wyu.wyuCode.Patch;

/// <summary>
/// 兜底接管：无 override GenerateAnimator 的怪（Guardbot/Noisebot/Stabbot 等）会走基类
/// MonsterModel.GenerateAnimator，而 Harmony 打基类虚方法拦不住"无 override 子类实例"的调用
/// （Fabricator/Ovicopter 有 override、补丁打在各自方法上，能拦）。
///
/// 对策：patch NCreature._Ready（它必然调用 GenerateAnimator 并把结果存入 _spineAnimator），
/// 在 _Ready 完成后，若该怪登记为"兜底接管"目标且尚未被 GenerateAnimator 补丁接管，
/// 就把 _spineAnimator 替换成我们的自定义 CreatureAnimator——之后游戏对它的所有
/// SetAnimationTrigger("Idle"/"Attack"/"Hit"/"Dead") 都会走我们 AnimState 的名字映射。
/// 原默认 animator 从未成功播过动画（idle_loop 找不到），替换后无副作用。
/// </summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class CreatureAnimatorFallbackPatch
{
    [HarmonyPostfix]
    static void Postfix(NCreature __instance)
    {
        try
        {
            MonsterModel? monster = __instance?.Entity?.Monster;
            if (__instance == null || monster == null)
            {
                return;
            }

            string id = monster.Id.Entry;
            if (!MonsterAnimatorPatch.IsFallbackToReady(id))
            {
                return; // override 怪由 GenerateAnimator 补丁处理，这里不管
            }
            if (MonsterAnimatorPatch.IsIntercepted(monster))
            {
                return; // 已被 GenerateAnimator 补丁接管（未来版本基类补丁恢复时避免重复）
            }

            MegaSprite? spine = __instance.Visuals?.SpineBody;
            if (spine == null)
            {
                return;
            }
            if (!MonsterAnimatorPatch.TryBuild(id, monster, spine, out CreatureAnimator animator))
            {
                return;
            }

            // 替换私有字段 _spineAnimator
            var field = AccessTools.Field(typeof(NCreature), "_spineAnimator");
            if (field == null)
            {
                GD.PushWarning("[wyu][动画] 找不到 NCreature._spineAnimator 字段，跳过 _Ready 兜底接管");
                return;
            }
            field.SetValue(__instance, animator);

            // 原 _Ready 只把 BoundsUpdated 连到了默认 animator；替换后为新 animator 重连一次
            AccessTools.Method(typeof(NCreature), "ConnectSpineAnimatorSignals")?.Invoke(__instance, null);
        }
        catch (Exception ex)
        {
            GD.PrintErr("[wyu][动画] CreatureAnimatorFallbackPatch 异常: " + ex);
        }
    }
}
