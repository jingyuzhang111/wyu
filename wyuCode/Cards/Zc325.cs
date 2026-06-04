using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Godot;
using wyu.wyuCode.Character;
using wyu.wyuCode.Extensions;
using wyu.wyuCode.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace wyu.wyuCode.Cards;

public class Zc325() :
    wyuCard(cost: 0,
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.AllEnemies)
{
    // 调试开关，设为 true 可在控制台输出详细日志
    private const bool Debug325 = false;
    private static readonly ConcurrentDictionary<Type, Func<Creature, decimal>> CurrentHpReaders = new();

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("Count3").WithMultiplier(Count3Multiplier),
        new CalculatedVar("Count2").WithMultiplier(Count2Multiplier),
        new CalculatedVar("Count5").WithMultiplier(Count5Multiplier),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromCard<Zc325SovereignBlade>(),
    ];

    // 以下三个函数分别作为 Count3/Count2/Count5 的乘数，供 CalculatedVar 调用
    private static decimal Count3Multiplier(CardModel card, Creature? target) => Collect325Counts(card)['3'];
    private static decimal Count2Multiplier(CardModel card, Creature? target) => Collect325Counts(card)['2'];
    private static decimal Count5Multiplier(CardModel card, Creature? target) => Collect325Counts(card)['5'];

    // 遍历所有符合条件的文本节点，统计其中字符 '3'、'2'、'5' 各自出现的总次数
    private static Dictionary<char, int> Collect325Counts(CardModel card)
    {
        var numbers = new Dictionary<char, int>
        {
            ['3'] = 0,
            ['2'] = 0,
            ['5'] = 0,
        };

        if (Debug325) Log.Info("[Zc325] 开始 Collect325Counts (from filtered labels)");

        foreach (var (_, _, text) in ReadAllLabelTexts())
        {
            Add325Digits(numbers, text);
        }

        if (Debug325) Log.Info($"[Zc325] 最终统计: Count3={numbers['3']}, Count2={numbers['2']}, Count5={numbers['5']}");
        return numbers;
    }

    // 解析单个字符串，将其中出现的 '3'/'2'/'5' 分别计数累加到 numbers 字典中
    private static void Add325Digits(Dictionary<char, int> numbers, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (Debug325) Log.Info($"[Zc325] Add325Digits called with value='{value}'");

        var found = new System.Text.StringBuilder();
        foreach (var c in value)
        {
            if (!numbers.ContainsKey(c))
            {
                continue;
            }

            var prev = numbers.GetValueOrDefault(c);
            numbers[c] = prev + 1;
            found.Append(c);
            if (Debug325) Log.Info($"[Zc325] Digit '{c}' incremented: {prev} -> {numbers[c]}");
        }

        if (Debug325 && found.Length > 0)
        {
            Log.Info($"[Zc325] 从 '{value}' 检测到数字: {found}");
        }
    }

    // 判断是否应跳过该文本节点：只保留战斗场景/战斗UI/顶部栏范围内的节点，
    // 并排除手牌索引、附魔显示等无意义内容
    private static bool ShouldSkipLabel(string path, string text)
    {
        if (!path.Contains("/root/Game/RootSceneContainer/Run/RoomContainer/CombatRoom/") &&
            !path.Contains("/root/Game/RootSceneContainer/Run/GlobalUi/CombatUi/") &&
            !path.Contains("/root/Game/RootSceneContainer/Run/GlobalUi/TopBar/"))
        {
            return true;
        }

        if (path.Contains("/CardContainer/StarIcon/StarLabel") && text == "-1")
        {
            return true;
        }

        if (path.Contains("/CardContainer/Enchantment/Label") && text == "2")
        {
            return true;
        }
        if (path.Contains("CombatVfxContainer/ThoughtBubbleVfx"))        
        {
            return true;
        }
        // if (path.Contains("/HandIndex"))
        // {
        //     return true;
        // }
        if (path.Contains("/DescriptionLabel"))
        {
            return true;
        }
        if (path.Contains("/root/Game/RootSceneContainer/Run/RoomContainer/CombatRoom/CombatUi/Hand/CardHolderContainer/"))
        {
            return true;
        }
        if (path.Contains("/root/Game/RootSceneContainer/Run/RoomContainer/CombatRoom/CombatUi/PlayContainer/Card/CardContainer/"))
        {
            return true;
        }

        return false;
    }

    // 判断文本是否包含字符 '3'、'2'、'5'之一
    private static bool Is325Text(string text) => text.Contains('3') || text.Contains('2') || text.Contains('5');

    // 尝试从 Control 节点获取文本内容（兼容 Label 的 Text 和 RichTextLabel 的 Text/BBCode）
    private static string? GetControlText(Control control)
    {
        if (control is Label label)
            return label.Text;
        if (control is RichTextLabel richLabel)
            return richLabel.Text;
        return null;
    }

    // 递归遍历场景树，收集所有文本包含 '3'、'2'、'5'之一 且未被过滤掉的文本节点（Control 类型）
    private static List<Control> Collect325LabelNodes()
    {
        var controls = new List<Control>();
        try
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            var root = tree?.Root;
            if (root == null)
            {
                return controls;
            }

            var viewportRect = root.GetViewport()?.GetVisibleRect() ?? new Rect2(0, 0, 1920, 1080);

            void Recurse(Node node)
            {
                foreach (var obj in node.GetChildren())
                {
                    if (obj is not Node child)
                    {
                        continue;
                    }

                    if (child is Control control && control is Label or RichTextLabel)
                    {
                        var path = child.GetPath().ToString();
                        var text = GetControlText(control) ?? string.Empty;
                        if (!ShouldSkipLabel(path, text) && Is325Text(text) && control.IsVisibleInTree())
                        {
                            // 过滤掉屏幕外及隐藏的 label（关闭的手牌序号、隐藏面板等）
                            var globalRect = new Rect2(control.GlobalPosition, control.Size);
                            if (viewportRect.Intersects(globalRect))
                            {
                                controls.Add(control);
                            }
                            else if (Debug325)
                            {
                                Log.Info($"[Zc325] 跳过屏幕外 label: '{text}' at {control.GlobalPosition} path={path}");
                            }
                        }
                    }

                    Recurse(child);
                }
            }

            Recurse(root);
        }
        catch (Exception e)
        {
            if (Debug325) Log.Info($"[Zc325] Collect325LabelNodes error: {e.Message}");
        }

        return controls;
    }

    // 读取所有符合条件的文本节点，返回（路径，类名，文本内容）的元组列表
    private static List<(string path, string className, string text)> ReadAllLabelTexts()
    {
        var texts = new List<(string path, string className, string text)>();
        try
        {
            foreach (var ctrl in Collect325LabelNodes())
            {
                texts.Add((ctrl.GetPath().ToString(), ctrl.GetClass(), GetControlText(ctrl) ?? string.Empty));
            }

            if (Debug325) Log.Info($"[Zc325] ReadAllLabelTexts found {texts.Count} labels");
        }
        catch (Exception e)
        {
            if (Debug325) Log.Info($"[Zc325] ReadAllLabelTexts error: {e.Message}");
        }

        return texts;
    }

    // 为所有符合条件的文本节点添加白色背景，便于肉眼确认捕获到了哪些节点（持续显示，用于 debug）
    private static int PaintCollectedLabelsWhite()
    {
        int painted = 0;
        try
        {
            var bg = new StyleBoxFlat { BgColor = new Color(1f, 1f, 1f, 0.3f) };
            foreach (var ctrl in Collect325LabelNodes())
            {
                if (ctrl is Label label)
                    label.AddThemeStyleboxOverride("normal", bg);
                else if (ctrl is RichTextLabel richLabel)
                    richLabel.AddThemeStyleboxOverride("normal", bg);
                painted++;
            }
        }
        catch (Exception e)
        {
            if (Debug325) Log.Info($"[Zc325] PaintCollectedLabelsWhite error: {e.Message}");
        }

        return painted;
    }

    // 尝试通过场景路径找到己方角色位置，用作环绕动画的中心点；找不到则返回屏幕中心
    private static Vector2 GetPlayerOrbitCenter(SceneTree tree)
    {
        var root = tree.Root;
        if (root == null)
        {
            return new Vector2(960f, 540f);
        }

        var candidatePaths = new[]
        {
            "/root/Game/RootSceneContainer/Run/RoomContainer/CombatRoom/CombatSceneContainer/AllyContainer/Creature",
            "/root/Game/RootSceneContainer/Run/RoomContainer/CombatRoom/CombatSceneContainer/AllyContainer",
            "/root/Game/RootSceneContainer/Run/RoomContainer/CombatRoom/CombatUi/Hand/CardHolderContainer"
        };

        foreach (var path in candidatePaths)
        {
            if (root.GetNodeOrNull<Node>(path) is CanvasItem canvasItem)
            {
                return canvasItem.GetGlobalTransformWithCanvas().Origin;
            }
        }

        var rect = root.GetViewport()?.GetVisibleRect() ?? new Rect2(0, 0, 1920, 1080);
        return rect.Size * 0.5f;
    }

    // 显示标签飞行动画，统计 3/2/5 最小值，生成相应数量的君王之剑卡牌添加到手牌
    private async Task<(int c3, int c2, int c5)> Animate325LabelCopiesAsync(
        PlayerChoiceContext choiceContext, CombatState combatState)
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        var root = tree?.Root;
        if (tree == null || root == null)
            return (0, 0, 0);

        var sourceControls = Collect325LabelNodes();
        if (sourceControls.Count == 0)
            return (0, 0, 0);

        var layer = new CanvasLayer { Name = "Zc325OrbitLayer", Layer = 2000 };
        root.AddChild(layer);

        var center = GetPlayerOrbitCenter(tree);
        var orbitCenter = center + Vector2.Up * 60f;
        var random = new System.Random();

        // 第一阶段：所有副本飞向中心附近半径60px的小圆上
        const float gatherRadius = 60f;
        var vanishClones = new List<Control>(sourceControls.Count);
        var tween = tree.CreateTween().SetParallel(true);
        for (int i = 0; i < sourceControls.Count; i++)
        {
            var source = sourceControls[i];
            var text = GetControlText(source) ?? "";
            // 用 source 的屏幕中心作为起始点，避免 Duplicate 继承大尺寸导致文字偏移
            var sourceCenter = source.GlobalPosition + source.Size / 2f;

            var clone = new Label
            {
                Text = text,
                Name = $"Zc325Vanish_{i}",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ZIndex = 1000 + i,
                TopLevel = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Modulate = source.Modulate,
            };
            // 复制字体颜色和大小
            if (source.HasThemeColorOverride("font_color"))
                clone.AddThemeColorOverride("font_color", source.GetThemeColor("font_color"));
            if (source.HasThemeFontSizeOverride("font_size"))
                clone.AddThemeFontSizeOverride("font_size", source.GetThemeFontSize("font_size"));

            layer.AddChild(clone);
            clone.GlobalPosition = sourceCenter;
            vanishClones.Add(clone);

            var angle = (float)(Math.PI * 2.0 * i / Math.Max(1, sourceControls.Count));
            var gatherPos = orbitCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * gatherRadius;
            var flyDuration = 0.5f + (float)random.NextDouble() * 0.3f; // 第一阶段：飞到圆圈
            tween.TweenMethod(Callable.From((float t) =>
            {
                if (!GodotObject.IsInstanceValid(clone)) return;
                clone.GlobalPosition = sourceCenter.Lerp(gatherPos, t);
                clone.Scale = Vector2.One * Mathf.Lerp(1f, 0.85f, t);
                                                                // 这里设置tween动画 Expo为最夸张的一档
            }), 0f, 1f, flyDuration).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Expo); // 先慢后快，夸张曲线
        }
        await tree.ToSignal(tween, Tween.SignalName.Finished);

        // 到齐后旋转着向中心点靠近并淡出（暂时关闭调试）
        var shrinkTween = tree.CreateTween().SetParallel(true);
        for (int i = 0; i < vanishClones.Count; i++)
        {
            var clone = vanishClones[i];
            if (!GodotObject.IsInstanceValid(clone)) continue;
        
            var baseAngle = (float)(Math.PI * 2.0 * i / Math.Max(1, vanishClones.Count));
            shrinkTween.TweenMethod(Callable.From((float t) =>
            {
                if (!GodotObject.IsInstanceValid(clone)) return;
                var spiralAngle = baseAngle + t * Mathf.Tau;
                var spiralRadius = gatherRadius * (1f - t);
                clone.GlobalPosition = orbitCenter + new Vector2(Mathf.Cos(spiralAngle), Mathf.Sin(spiralAngle)) * spiralRadius;
                clone.Modulate = new Color(1f, 1f, 1f, 1f - t);
                clone.Scale = Vector2.One * Mathf.Lerp(0.85f, 0.2f, t);
            }), 0f, 1f, 1f).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad); // 第二阶段：螺旋淡出
        }
        await tree.ToSignal(shrinkTween, Tween.SignalName.Finished);

        // // 暂停观察 label 位置
        // await tree.ToSignal(tree.CreateTimer(3f), SceneTreeTimer.SignalName.Timeout);

        foreach (var c in vanishClones)
            if (GodotObject.IsInstanceValid(c)) c.QueueFree();

        // 统计 3/2/5 总数
        int total3 = 0, total2 = 0, total5 = 0;
        foreach (var (_, _, text) in ReadAllLabelTexts())
            foreach (var ch in text)
            {
                if (ch == '3') total3++;
                else if (ch == '2') total2++;
                else if (ch == '5') total5++;
            }

        int countMin = Math.Min(total3, Math.Min(total2, total5));

        // 第二阶段：逐个生成君王之剑卡牌，每张间隔2秒
        for (int i = 0; i < countMin; i++)
        {
            await Zc325SovereignBlade.CreateInHand(base.Owner, 1, combatState);
            if (i < countMin - 1)
            {
                await tree.ToSignal(tree.CreateTimer(1f), SceneTreeTimer.SignalName.Timeout);
            }
        }

        layer.QueueFree();
        return (total3, total2, total5);
    }


    // 卡牌效果主入口：白高亮 → 环绕动画 → 统计3/2/5 → 取最小值生成君王之剑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // PaintCollectedLabelsWhite();
        // 调试：打印所有含 3/2/5 的 label
        foreach (var (path, _, text) in ReadAllLabelTexts())
            Log.Info($"[Zc325] path={path} text='{text}'");

        var combatState = base.CombatState ?? throw new InvalidOperationException("CombatState is null.");
        var (count3, count2, count5) = await Animate325LabelCopiesAsync(choiceContext, combatState);

        var countMin = (int)Math.Min(count3, Math.Min(count2, count5));
        Log.Info($"Zc325 攻击次数:{countMin} (3:{count3}, 2:{count2}, 5:{count5})");

    }

    // 升级：基础伤害 +4
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
