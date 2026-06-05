using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace wyu.wyuCode.Patch;

[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.Create))]
public static class RestSiteSpinePatch
{
    private const string CustomScenePath = "res://wyu/Scenes/creatureVisual/yumaobi_jijian.tscn";
    private const string RelaxAnimName = "Sit";

    static void Postfix(Player player, ref NRestSiteCharacter __result)
    {
        // 调试：打印所有 Create 调用
        var charId = player.Character?.Id?.Entry ?? "null";
        GD.Print($"[wyu][RestSite] Create Postfix 触发, 角色ID={charId}");

        if (charId != "WYU-WYU")
            return;

        GD.Print("[wyu][RestSite] 匹配 wyu，开始替换 Spine");

        // 1. 移除原场景自带的 SpineSprite
        var toRemove = new Godot.Collections.Array<Node>();
        foreach (var child in __result.GetChildren())
        {
            if (child is Node2D && child.GetClass() == "SpineSprite")
                toRemove.Add(child);
        }
        foreach (var oldSpine in toRemove)
        {
            oldSpine.QueueFree();
        }

        // 2. 加载自定义场景，提取 SpineSprite
        if (!ResourceLoader.Exists(CustomScenePath))
        {
            GD.PushWarning($"[wyu][RestSite] 找不到场景: {CustomScenePath}");
            return;
        }

        var scene = ResourceLoader.Load<PackedScene>(CustomScenePath);
        var root = scene.Instantiate(PackedScene.GenEditState.Disabled);
        var customSpine = root.GetNodeOrNull<Node2D>("Visuals");

        if (customSpine == null)
        {
            root.QueueFree();
            GD.PushWarning("[wyu][RestSite] 自定义场景中找不到 Visuals 节点");
            return;
        }

        // 3. 把 SpineSprite 搬进 NRestSiteCharacter
        root.RemoveChild(customSpine);
        __result.AddChild(customSpine);
        root.QueueFree();

        // 4. 延迟播 Relax（等 _Ready 跑完，下一帧覆盖动画）
        var character = __result;
        character.TreeEntered += () =>
        {
            var timer = character.GetTree().CreateTimer(0.0f);
            timer.Timeout += () =>
            {
                var megaSprite = new MegaSprite(customSpine);
                megaSprite.GetAnimationState().SetAnimation(RelaxAnimName, true);
                GD.Print("[wyu][RestSite] Relax 动画已播放");
            };
        };
    }
}
