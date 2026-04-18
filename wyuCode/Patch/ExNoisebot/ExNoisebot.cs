using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System;

namespace wyu.wyuCode.Patch;

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.CreateVisuals))]
public static class NoisebotVisualReplacePatch
{
    private const string TargetMonsterId = "NOISEBOT";
    private const string CustomScenePath = "res://wyu/Scenes/creatureVisual/noisebot_mod.tscn";
    private static readonly bool VerboseAllMonsters = true;

    private static bool Prefix(MonsterModel __instance, ref NCreatureVisuals __result)
    {
        string currentId = __instance.Id.Entry;

        if (VerboseAllMonsters)
        {
            GD.Print($"[wyu][替换检测] 进入{TargetMonsterId}补丁，当前怪物ID: {currentId}");
        }

        // 只替换 Noisebot，其他怪物走原逻辑
        if (!currentId.Equals(TargetMonsterId, System.StringComparison.OrdinalIgnoreCase))
            return true;

        GD.Print($"[wyu][替换命中] {TargetMonsterId} 命中，模型类型={__instance.GetType().FullName}，开始替换视觉场景: {CustomScenePath}");

        try
        {
            bool exists = ResourceLoader.Exists(CustomScenePath);
            GD.Print($"[wyu][步骤1] {TargetMonsterId} 场景存在性检查: {exists}");

            if (!exists)
            {
                GD.Print($"[wyu][替换失败] 找不到场景文件: {CustomScenePath}，已回退原版 {TargetMonsterId} 视觉。");
                return true;
            }

            PackedScene? scene = ResourceLoader.Load<PackedScene>(CustomScenePath);
            GD.Print($"[wyu][步骤2] {TargetMonsterId} 场景加载结果: {(scene == null ? "null" : "ok")}");
            if (scene == null)
            {
                GD.Print($"[wyu][替换失败] 场景加载失败: {CustomScenePath}，已回退原版 {TargetMonsterId} 视觉。");
                return true; // 回退原版，避免崩
            }

            Node node = scene.Instantiate(PackedScene.GenEditState.Disabled);
            GD.Print($"[wyu][步骤3] {TargetMonsterId} 场景实例化类型: C#={node.GetType().FullName}, Godot={node.GetClass()}");

            if (node is NCreatureVisuals visuals)
            {
                GD.Print($"[wyu][替换成功] {TargetMonsterId} 已切换为自定义视觉场景（原生 NCreatureVisuals）。");
                __result = visuals;
                return false; // 跳过原 CreateVisuals
            }

            if (node is Node2D rawNode)
            {
                GD.Print($"[wyu][步骤4] 检测到根节点是 Node2D，尝试运行时桥接为 WyuCreatureVisualBradge。");

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
                GD.Print($"[wyu][替换成功] {TargetMonsterId} 已切换为自定义视觉场景（运行时桥接模式）。");
                return false;
            }

            GD.Print($"[wyu][替换失败] 场景根节点既不是 NCreatureVisuals 也不是 Node2D，已回退原版 {TargetMonsterId} 视觉。");
            return true;
        }
        catch (Exception ex)
        {
            GD.Print($"[wyu][替换异常] {TargetMonsterId} 替换过程中抛异常: {ex}");
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