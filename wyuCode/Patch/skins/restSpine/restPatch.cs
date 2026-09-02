using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers; // RunWhenSpineReady
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace wyu.wyuCode.Patch;

[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.Create))]
public static class RestSiteSpinePatch
{
    // 玩家休闲视觉场景与休息动画名由当前皮肤决定
    private static string CustomScenePath => PlayerSkinRegistry.Current.LeisureVisualScenePath;
    private static string RelaxAnimName => PlayerSkinRegistry.Current.RestSiteAnim;

    // 休息角色尺寸：放大 >1.0，缩小 <1.0（1.0 = 用场景里 Visuals 的原始缩放，不动）。
    // 只作用于休息点；商店仍用场景里的 -0.91 缩放。若想商店也一起变，改场景文件
    // wyu/Scenes/creatureVisual/yumaobi_jijian.tscn 里 Visuals 节点的 scale 即可。
    private const float RestScale = 2f;

    static void Postfix(Player player, ref NRestSiteCharacter __result)
    {
        // 调试：打印所有 Create 调用
        var charId = player.Character?.Id?.Entry ?? "null";
        GD.Print($"[wyu][RestSite] Create Postfix 触发, 角色ID={charId}");

        if (charId != "WYU-WYU")
            return;

        GD.Print("[wyu][RestSite] 匹配 wyu，开始替换 Spine");

        // 1. 移除原场景自带的 SpineSprite
        //    必须同步 RemoveChild（不能只 QueueFree）：
        //    这里在 Create 时节点还没进树，QueueFree 是延迟到帧末才删除，
        //    而游戏 _Ready 在进树时立即执行，会扫描子节点里所有 SpineSprite
        //    并播放 overgrowth_loop/hive_loop/glory_loop —— 若骨架没有该动画
        //    会在 spine 原生层(C++)崩溃导致闪退。
        var toRemove = new Godot.Collections.Array<Node>();
        foreach (var child in __result.GetChildren())
        {
            if (child is Node2D && child.GetClass() == "SpineSprite")
                toRemove.Add(child);
        }
        foreach (var oldSpine in toRemove)
        {
            __result.RemoveChild(oldSpine); // 同步移出子节点列表，_Ready 就扫不到
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
        //    注意：不能直接挂在 __result 下 —— 游戏的 GetChildSpineNodes() 会扫描
        //    所有直接子节点，只要是 SpineSprite 就尝试播放当前幕动画
        //    (overgrowth_loop/hive_loop/glory_loop)，找不到会报错。
        //    所以包一层普通 Node2D 容器，让它不被扫描到。
        root.RemoveChild(customSpine);

        var container = new Node2D { Name = "WyuRestSpineContainer" };
        container.Scale = new Vector2(RestScale, RestScale); // 只调休息角色大小（不影响商店）
        container.AddChild(customSpine);
        __result.AddChild(container);
        root.QueueFree();

        // 4. 等骨架就绪后播 Relax
        //    不能用 CreateTimer(0.0f) + GetAnimationState()：SpineSprite 的骨架是异步加载的，
        //    下一帧很可能还没就绪，此时 GetAnimationState() 会抛 InvalidOperationException；
        //    该异常从 timer 回调逃逸到引擎层，未处理会直接闪退。
        //    用 RunWhenSpineReady：跨帧等待骨架就绪、校验对象有效性（节点被释放/退树则安全跳过），
        //    与游戏 NRestSiteCharacter._Ready 驱动动画的方式一致（也同仓库其他补丁的写法）。
        var character = __result;
        var megaSprite = new MegaSprite(customSpine);
        character.TreeEntered += () =>
        {
            character.RunWhenSpineReady(megaSprite, state =>
            {
                state.SetAnimation(RelaxAnimName, true);
                GD.Print("[wyu][RestSite] Relax 动画已播放");
            });
        };
    }
}
