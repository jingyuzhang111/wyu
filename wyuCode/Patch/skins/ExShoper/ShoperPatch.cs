using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers; // RunWhenSpineReady
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace wyu.wyuCode.Patch;

/// <summary>
/// 替换商人 NPC 模型 —— 成熟路线（参考创意工坊 Merchant2CuteII）：
/// patch NMerchantButton._Ready，找到 %MerchantVisual 后【保留节点】，
/// 仅用 SetSkeletonDataRes 把它的 Spine 骨骼换成 keluxier，不删节点不重建，
/// 避免 AfterRoomIsLoaded + 删节点重建造成的"原版商人投影/阴影残留"。
/// </summary>
[HarmonyPatch(typeof(NMerchantButton), "_Ready")]
public static class MerchantNPCSpinePatch
{
    private const string KeluxierSkeletonDataPath = "res://src/keluxier/new_spine_skeleton_data_resource.tres";
    private const string KannuoteSkeletonDataPath = "res://src/坎诺特/new_spine_skeleton_data_resource.tres";

    [HarmonyPrefix]
    static void Prefix(NMerchantButton __instance)
    {
        try
        {
            Node? merchantVisual = TryGetMerchantVisual(__instance);
            if (merchantVisual is not Node2D visual)
            {
                GD.PushWarning("[wyu][ShopNPC] 找不到 %MerchantVisual");
                return;
            }

            // 假商人事件里的商人按钮单独处理：换成坎诺特（真商店仍是可露希尔）
            bool isFakeMerchant = IsInsideFakeMerchant(__instance);
            string skeletonPath = isFakeMerchant ? KannuoteSkeletonDataPath : KeluxierSkeletonDataPath;

            if (!ResourceLoader.Exists(skeletonPath))
            {
                GD.PushWarning($"[wyu][ShopNPC] 找不到骨骼: {skeletonPath}");
                return;
            }

            Resource? res = ResourceLoader.Load<Resource>(skeletonPath);
            if (res == null)
            {
                GD.PushWarning("[wyu][ShopNPC] 骨骼加载失败");
                return;
            }

            // 保留原节点（transform/父链不动，模型仍在原商人站位），只替换 Spine 骨骼数据
            var mega = new MegaSprite(visual);
            mega.SetSkeletonDataRes(new MegaSkeletonDataResource(res));
            GD.Print($"[wyu][ShopNPC] {(isFakeMerchant ? "假商人" : "真商人")}按钮换骨为 {(isFakeMerchant ? "kannuote(坎诺特)" : "keluxier(可露希尔)")}：初始 pos={visual.Position} scale={visual.Scale}");

            if (isFakeMerchant)
            {
                // 坎诺特：坎诺特本地体型小，等骨架真正就绪后按基准 ×2 放大 + 左右镜像
                // （scale.x 强制取负即镜像，朝向玩家；锚点位置不动）。数值现场确认。
                visual.RunWhenSpineReady(mega, _ =>
                {
                    Vector2 baseScale = visual.Scale;
                    Vector2 basePos = visual.Position;

                    visual.Scale = new Vector2(
                        -Mathf.Abs(baseScale.X) * 2f,
                        Mathf.Abs(baseScale.Y) * 2f);
                    GD.Print($"[wyu][ShopNPC] 假商人坎诺特应用 ×2+镜像：原 pos={basePos} scale={baseScale} " +
                             $"-> 现 pos={visual.Position} scale={visual.Scale}");
                });
                return;
            }

            // ---- 真商店（可露希尔）固化尺寸/位置 ----------------
            // 等骨架真正就绪再设置 transform（过早设会被骨架初始化复位）。
            // 数值经现场确认：放大 + 镜像 + 屏幕内位置补偿。
            visual.RunWhenSpineReady(mega, _ =>
            {
                Vector2 baseScale = visual.Scale;
                Vector2 basePos = visual.Position;

                visual.Scale = new Vector2(-0.8345235f, 0.8345235f);
                visual.Position = new Vector2(107.3f, 263.32f);
                GD.Print($"[wyu][ShopNPC] 可露希尔就绪并应用尺寸/朝向：" +
                         $"原 pos={basePos} scale={baseScale} " +
                         $"-> 现 pos={visual.Position} scale={visual.Scale}");
            });
        }
        catch (Exception ex)
        {
            GD.PrintErr("[wyu][ShopNPC] 换骨异常: " + ex);
        }
    }

    /// <summary>该商人按钮是否位于假商人事件（NFakeMerchant）中。</summary>
    private static bool IsInsideFakeMerchant(Node node)
    {
        Node? cur = node;
        while (cur != null)
        {
            if (cur is NFakeMerchant)
            {
                return true;
            }
            cur = cur.GetParent();
        }
        return false;
    }

    /// <summary>兼容不同游戏版本查找 %MerchantVisual（% 唯一名 → 直接名 → 递归查找）。</summary>
    private static Node? TryGetMerchantVisual(Node root)
    {
        return root.GetNodeOrNull<Node>("%MerchantVisual")
               ?? root.GetNodeOrNull<Node>("MerchantVisual")
               ?? root.FindChild("MerchantVisual", true, false);
    }

}
