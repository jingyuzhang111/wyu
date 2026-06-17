using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace wyu.wyuCode.Patch;

/// 替换商人 NPC 的模型
[HarmonyPatch(typeof(NMerchantRoom), "AfterRoomIsLoaded")]
public static class MerchantNPCSpinePatch
{
    private const string CustomScenePath = "res://wyu/Scenes/creatureVisual/keluxier.tscn";
    private const string IdleAnimName = "Skill_2_Idle";

    static void Postfix(NMerchantRoom __instance)
    {
        var oldVisual = __instance.MerchantButton?.GetNode("%MerchantVisual");
        if (oldVisual == null)
        {
            GD.PushWarning("[wyu][ShopNPC] 找不到 %MerchantVisual 节点");
            return;
        }

        if (!ResourceLoader.Exists(CustomScenePath))
        {
            GD.PushWarning($"[wyu][ShopNPC] 找不到场景: {CustomScenePath}");
            return;
        }

        var scene = ResourceLoader.Load<PackedScene>(CustomScenePath);
        var root = scene.Instantiate(PackedScene.GenEditState.Disabled);
        var newSpine = root.GetNodeOrNull<Node2D>("Visuals");

        if (newSpine == null)
        {
            root.QueueFree();
            GD.PushWarning("[wyu][ShopNPC] 自定义场景中找不到 Visuals 节点");
            return;
        }

        // 保留 .tscn 自带的 scale/rotation（那是给这个 skeleton 调的）
        var customScale = newSpine.Scale;
        var customRotation = newSpine.Rotation;

        var parent = oldVisual.GetParent();
        root.RemoveChild(newSpine);
        parent.AddChild(newSpine);
        oldVisual.QueueFree();
        root.QueueFree();

        // 位置归零，缩放/旋转用 .tscn 原值
        newSpine.Position = new Vector2(100, 300);
        newSpine.Scale = customScale;
        newSpine.Rotation = customRotation;

        var megaSprite = new MegaSprite(newSpine);
        megaSprite.GetAnimationState()?.SetAnimation(IdleAnimName, true);
        GD.Print($"[wyu][ShopNPC] 商人模型已替换 pos={newSpine.Position} scale={newSpine.Scale}");
    }
}
