using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace wyu.wyuCode.Patch;

/// <summary>
/// 坎诺特 替换「假商人」（FakeMerchantMonster）视觉与动画。
/// 假商人是"反转商人"事件里的特殊怪，走标准 MonsterModel 流程，因此可套 CreateVisuals 替换。
/// 它 override 了 GenerateAnimator（动作触发名：Attack/spew/throw/Cast/Hit/Dead），
/// 所以这里也替换 GenerateAnimator，映射到坎诺特动画：
///   攻击类(Attack/spew/throw) → Skill；施加/加buff(Cast) → Skill2；死亡(Dead) → Skill3；其余(Hit/Idle) → Idle。
/// </summary>
public static class FakeMerchantReplacePatch
{
    private const string CustomScenePath = "res://wyu/Scenes/creatureVisual/kannuote.tscn";

    // ---------- 1. 视觉替换：只对假商人生效 ----------
    [HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.CreateVisuals))]
    public static class FakeMerchantVisualReplacePatch
    {
        private static bool Prefix(MonsterModel __instance, ref NCreatureVisuals __result)
        {
            // 用类型匹配，避免依赖怪 ID 字符串（FakeMerchantMonster 是 sealed 类）
            if (__instance is not FakeMerchantMonster)
            {
                return true;
            }

            try
            {
                if (!ResourceLoader.Exists(CustomScenePath))
                {
                    GD.Print("[wyu][假商人] 找不到场景，已回退原版视觉: " + CustomScenePath);
                    return true;
                }

                PackedScene? scene = ResourceLoader.Load<PackedScene>(CustomScenePath);
                if (scene == null)
                {
                    GD.Print("[wyu][假商人] 场景加载失败，已回退原版视觉");
                    return true;
                }

                Node node = scene.Instantiate(PackedScene.GenEditState.Disabled);

                if (node is NCreatureVisuals visuals)
                {
                    GD.Print("[wyu][假商人] 已替换为坎诺特（原生 NCreatureVisuals）");
                    __result = visuals;
                    return false;
                }

                if (node is Node2D rawNode)
                {
                    var bridge = new WyuCreatureVisualBradge
                    {
                        Name = rawNode.Name,
                        Transform = rawNode.Transform,
                        Visible = rawNode.Visible,
                        ProcessMode = rawNode.ProcessMode,
                    };

                    while (rawNode.GetChildCount() > 0)
                    {
                        Node child = rawNode.GetChild(0);
                        rawNode.RemoveChild(child);
                        bridge.AddChild(child);
                        SetOwnerRecursive(child, bridge);
                    }

                    rawNode.QueueFree();
                    __result = bridge;
                    GD.Print("[wyu][假商人] 已替换为坎诺特（运行时桥接模式）");
                    return false;
                }

                GD.Print("[wyu][假商人] 场景根节点类型不支持，已回退原版视觉");
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr("[wyu][假商人] CreateVisuals 替换异常: " + ex);
                return true;
            }
        }

        private static void SetOwnerRecursive(Node node, Node owner)
        {
            node.Owner = owner;
            for (int i = 0; i < node.GetChildCount(); i++)
            {
                SetOwnerRecursive(node.GetChild(i), owner);
            }
        }
    }

    // ---------- 2. 动画控制器：坎诺特动画名映射 ----------
    public static class FakeMerchantAnimatorBuilder
    {
        public static CreatureAnimator Build(MegaSprite controller)
        {
            AnimState idle = new("Idle", isLooping: true);
            AnimState skill = new("Skill", isLooping: false);   // 攻击
            AnimState skill2 = new("Skill2", isLooping: false); // 施加效果/自加 buff
            AnimState die = new("Skill3", isLooping: false);    // 死亡

            skill.NextState = idle;
            skill2.NextState = idle;

            CreatureAnimator animator = new(idle, controller);
            animator.AddAnyState("Dead", die);       // 死亡 → Skill3
            animator.AddAnyState("Hit", idle);       // 受击无专门动画 → 回 Idle
            animator.AddAnyState("Attack", skill);   // 挥击 → Skill
            animator.AddAnyState("spew", skill);     // 喷金币(多段攻击) → Skill
            animator.AddAnyState("throw", skill);    // 扔遗物(攻击) → Skill
            animator.AddAnyState("Cast", skill2);    // 施加/自加力量 → Skill2
            return animator;
        }
    }

    [HarmonyPatch(typeof(FakeMerchantMonster), nameof(FakeMerchantMonster.GenerateAnimator))]
    public static class FakeMerchantAnimatorPatch
    {
        private static bool Prefix(MegaSprite controller, ref CreatureAnimator __result)
        {
            __result = FakeMerchantAnimatorBuilder.Build(controller);
            return false; // 跳过原 GenerateAnimator
        }
    }
}
