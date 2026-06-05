using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace wyu.wyuCode.Patch;

[HarmonyPatch(typeof(NMerchantRoom), "AfterRoomIsLoaded")]
public static class MerchantSpinePatch
{
    private const string CustomScenePath = "res://wyu/Scenes/creatureVisual/yumaobi_jijian.tscn";
    private const string IdleAnimName = "Relax";

    static void Postfix(NMerchantRoom __instance)
    {
        // 通过反射拿 _players 私有字段，确认本地玩家是 wyu
        var players = Traverse.Create(__instance).Field<List<Player>>("_players").Value;
        if (players == null || players.Count == 0)
            return;

        // AfterRoomIsLoaded 把本地玩家排到 index 0
        var me = players[0];
        var charId = me.Character?.Id?.Entry ?? "null";
        GD.Print($"[wyu][Shop] AfterRoomIsLoaded Postfix 触发, 角色ID={charId}");

        if (charId != "WYU-WYU")
            return;

        // PlayerVisuals[0] 就是本地玩家的 NMerchantCharacter
        var visuals = __instance.PlayerVisuals;
        if (visuals.Count == 0)
            return;

        var merchantChar = visuals[0] as Node;
        GD.Print("[wyu][Shop] 匹配 wyu，开始替换 Spine");

        // 1. 移除原 SpineSprite（NMerchantCharacter 的子节点就是 SpineSprite）
        foreach (var child in merchantChar.GetChildren())
        {
            if (child is Node2D && child.GetClass() == "SpineSprite")
                child.QueueFree();
        }

        // 2. 加载自定义场景，提取 SpineSprite
        if (!ResourceLoader.Exists(CustomScenePath))
        {
            GD.PushWarning($"[wyu][Shop] 找不到场景: {CustomScenePath}");
            return;
        }

        var scene = ResourceLoader.Load<PackedScene>(CustomScenePath);
        var root = scene.Instantiate(PackedScene.GenEditState.Disabled);
        var customSpine = root.GetNodeOrNull<Node2D>("Visuals");

        if (customSpine == null)
        {
            root.QueueFree();
            GD.PushWarning("[wyu][Shop] 自定义场景中找不到 Visuals 节点");
            return;
        }

        // 3. 搬进 NMerchantCharacter
        root.RemoveChild(customSpine);
        merchantChar.AddChild(customSpine);
        root.QueueFree();

        // 4. 直接播动画（_Ready 已经跑完了，不用延迟）
        var megaSprite = new MegaSprite(customSpine);
        megaSprite.GetAnimationState().SetAnimation(IdleAnimName, true);
        GD.Print("[wyu][Shop] Sit 动画已播放");
    }
}
