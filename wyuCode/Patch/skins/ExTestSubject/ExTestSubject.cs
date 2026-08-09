using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System;

namespace wyu.wyuCode.Patch;

/// <summary>
/// 把 TEST_SUBJECT（TestSubject 三阶段 Boss）的视觉场景替换成自定义 spine。
/// 场景根节点是 Node2D 时,运行时桥接为 WyuCreatureVisualBradge。
/// 场景文件路径按需改成你自己的。
/// </summary>
[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.CreateVisuals))]
public static class TestSubjectVisualReplacePatch
{
    private const string TargetMonsterId = "TEST_SUBJECT";
    private const string CustomScenePath = "res://wyu/Scenes/creatureVisual/testsubject_mod.tscn";

    private static bool Prefix(MonsterModel __instance, ref NCreatureVisuals __result)
    {
        if (!__instance.Id.Entry.Equals(TargetMonsterId, System.StringComparison.OrdinalIgnoreCase))
            return true; // 不是目标怪物,走原逻辑

        try
        {
            if (!ResourceLoader.Exists(CustomScenePath))
            {
                GD.Print($"[wyu][替换失败] 找不到场景文件: {CustomScenePath}，已回退原版 TestSubject 视觉。");
                return true;
            }

            PackedScene? scene = ResourceLoader.Load<PackedScene>(CustomScenePath);
            if (scene == null)
            {
                GD.Print($"[wyu][替换失败] 场景加载失败: {CustomScenePath}，已回退原版 TestSubject 视觉。");
                return true;
            }

            Node node = scene.Instantiate(PackedScene.GenEditState.Disabled);

            if (node is NCreatureVisuals visuals)
            {
                GD.Print("[wyu][替换成功] TestSubject 已切换为自定义视觉场景（原生 NCreatureVisuals）。");
                __result = visuals;
                return false; // 跳过原 CreateVisuals
            }

            if (node is Node2D rawNode)
            {
                WyuCreatureVisualBradge bridge = new WyuCreatureVisualBradge
                {
                    Name = rawNode.Name,
                    Transform = rawNode.Transform,
                    Visible = rawNode.Visible,
                    ProcessMode = rawNode.ProcessMode
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
                GD.Print("[wyu][替换成功] TestSubject 已切换为自定义视觉场景（运行时桥接模式）。");
                return false;
            }

            GD.Print("[wyu][替换失败] TestSubject 场景根节点既不是 NCreatureVisuals 也不是 Node2D，已回退原版视觉。");
            return true;
        }
        catch (Exception ex)
        {
            GD.Print($"[wyu][替换异常] TestSubject 替换过程中抛异常: {ex}");
            return true;
        }
    }

    private static void SetOwnerRecursive(Node node, Node owner)
    {
        node.Owner = owner;
        for (int i = 0; i < node.GetChildCount(); i++)
            SetOwnerRecursive(node.GetChild(i), owner);
    }
}
