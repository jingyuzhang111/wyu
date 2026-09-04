using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System;

namespace wyu.wyuCode.Patch;

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.CreateVisuals))]
public static class GuardbotVisualReplacePatch
{
    private const string TargetMonsterId = "GUARDBOT";
    private const string CustomScenePath = "res://wyu/Scenes/creatureVisual/guardbot_mod.tscn";
    private static readonly bool VerboseAllMonsters = false;

    private static bool Prefix(MonsterModel __instance, ref NCreatureVisuals __result)
    {
        string currentId = __instance.Id.Entry;

        if (VerboseAllMonsters)
        {
        }

        // 只替换 Guardbot，其他怪物走原逻辑
        if (!currentId.Equals(TargetMonsterId, System.StringComparison.OrdinalIgnoreCase))
            return true;


        try
        {
            bool exists = ResourceLoader.Exists(CustomScenePath);

            if (!exists)
            {
                GD.Print($"[wyu][替换失败] 找不到场景文件: {CustomScenePath}，已回退原版 {TargetMonsterId} 视觉。");
                return true;
            }

            PackedScene? scene = ResourceLoader.Load<PackedScene>(CustomScenePath);
            if (scene == null)
            {
                GD.Print($"[wyu][替换失败] 场景加载失败: {CustomScenePath}，已回退原版 {TargetMonsterId} 视觉。");
                return true; // 回退原版，避免崩
            }

            Node node = scene.Instantiate(PackedScene.GenEditState.Disabled);

            if (node is NCreatureVisuals visuals)
            {
                GD.Print($"[wyu][替换成功] {TargetMonsterId} 已切换为自定义视觉场景（原生 NCreatureVisuals）。");
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