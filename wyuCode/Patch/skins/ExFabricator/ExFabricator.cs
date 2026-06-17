using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System;

namespace wyu.wyuCode.Patch;

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.CreateVisuals))]
public static class FabricatorVisualReplacePatch
{
    private const string TargetMonsterId = "FABRICATOR";
    private const string CustomScenePath = "res://wyu/Scenes/creatureVisual/fabricator_mod.tscn";
    private static readonly bool VerboseAllMonsters = true;

    private static bool Prefix(MonsterModel __instance, ref NCreatureVisuals __result)
    {
        string currentId = __instance.Id.Entry;

        if (VerboseAllMonsters)
        {
            GD.Print($"[wyu][替换检测] 进入{TargetMonsterId}补丁，当前怪物ID: {currentId}");
        }

        // 只替换 Grandbot，其他怪物走原逻辑
        if (!currentId.Equals(TargetMonsterId, System.StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            bool exists = ResourceLoader.Exists(CustomScenePath);

            if (!exists)
            {
                return true;
            }

            PackedScene? scene = ResourceLoader.Load<PackedScene>(CustomScenePath);
            if (scene == null)
            {
                return true; // 回退原版，避免崩
            }

            Node node = scene.Instantiate(PackedScene.GenEditState.Disabled);

            if (node is NCreatureVisuals visuals)
            {
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