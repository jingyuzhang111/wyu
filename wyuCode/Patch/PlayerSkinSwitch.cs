using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace wyu.wyuCode.Patch;

/// <summary>
/// 玩家换肤按钮：挂在游戏全局 UI（NGlobalUi）的左上角。
/// 点击 → 循环切换到下一套玩家皮肤：
///  - 商店：当场刷新本地玩家形象（复用 MerchantSpinePatch）；
///  - 战斗 / 休息处：下一套会在"下次进入对应房间"时自动生效（CustomVisualPath / rest / shop 都读当前皮肤）。
/// 注：已用 SetSkeletonDataRes 验证过"战斗内热换骨"可行（换骨生效、尺寸变化），
///     但战斗/基建严格成套，且换骨后需重建动画器 + 播入场动画才安全；
///     等第二套皮肤资源就绪后，再做战斗内正式热切换。
/// </summary>
[HarmonyPatch(typeof(NGlobalUi), "_Ready")]
public static class PlayerSkinSwitch
{
    /// <summary>发布开关：false = 不创建换肤按钮（皮肤系统仍生效，默认皮肤展示）。改回 true 即恢复按钮。</summary>
    private const bool Enabled = false;

    private const string LayerNodeName = "WyuPlayerSkinSwitch";
    private static Button? _button;

    static void Postfix(NGlobalUi __instance)
    {
        if (!Enabled) return;
        EnsureButton(__instance);
    }

    /// <summary>在给定父节点下（若无则）创建左上角换肤按钮。</summary>
    public static void EnsureButton(Node parent)
    {
        if (!Enabled) return;
        if (parent == null || !GodotObject.IsInstanceValid(parent)) return;
        if (parent.GetNodeOrNull<CanvasLayer>(LayerNodeName) != null) return;

        var layer = new CanvasLayer
        {
            Name = LayerNodeName,
            Layer = 100, // 高于绝大多数 UI，保证始终可见可点
        };

        _button = new Button
        {
            Text = PlayerSkinRegistry.Current.DisplayName,
            Position = new Vector2(8f, 8f),
            FocusMode = Control.FocusModeEnum.None,
        };
        _button.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _button.Pressed += OnPressed;

        layer.AddChild(_button);
        parent.AddChild(layer);
        GD.Print($"[wyu][皮肤] 换肤按钮已创建，当前皮肤: {PlayerSkinRegistry.Current.DisplayName}");
    }

    private static void OnPressed()
    {
        var skin = PlayerSkinRegistry.CycleNext();
        if (_button != null)
            _button.Text = skin.DisplayName;
        GD.Print($"[wyu][皮肤] 已切换到: {skin.DisplayName} ({skin.Id})");

        if (NMerchantRoom.Instance != null)
        {
            MerchantSpinePatch.RefreshLocalPlayer(NMerchantRoom.Instance);
            GD.Print("[wyu][皮肤] 已在商店即时刷新玩家形象");
        }
        else
        {
            GD.Print("[wyu][皮肤] 非商店场景：新皮肤将在下次进入对应房间时生效（战斗内热切换待第二套资源就绪后再做）");
        }
    }
}

