using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Events.Custom; // NFakeMerchant（假商人事件）
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System.Collections.Generic;  // IEnumerable<>
using MegaCrit.Sts2.Core.Nodes.Vfx;
using System.Linq;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
namespace wyu.wyuCode.Patch;


/// 替换商人 NPC 的模型
[HarmonyPatch(typeof(SfxCmd), 
nameof(SfxCmd.Play), 
new[] { typeof(string), typeof(float) })]
public static class ShoperAudioManager
{
    private sealed record VoiceLineOption(string LocKey, string AudioPath);

    /// <summary>总开关：置 true 可完全关闭本 mod 的商人语音接管，避免与其它商人语音 mod 冲突。</summary>
    private const bool DisableMerchantAudio = false;

    public static LocString linePath = new LocString("characters", "NULL");

    private static Dictionary<string, VoiceLineOption[]> merchantSfx = new()
    {
        ["merchant_welcome"] = new VoiceLineOption[]
        {
            new VoiceLineOption("KELUXIER-闲置", "res://src/keluxier/audio/闲置.wav"),
            new VoiceLineOption("KELUXIER-3星结束行动", "res://src/keluxier/audio/3星结束行动.wav"),
        },
        ["merchant_dissapointment"] = new VoiceLineOption[]
        {
            new VoiceLineOption("KELUXIER-戳一下", "res://src/keluxier/audio/戳一下.wav"),
            new VoiceLineOption("KELUXIER-作战中2", "res://src/keluxier/audio/作战中2.wav")
        },
        ["merchant_thank_yous"] = new VoiceLineOption[]
        {
            new VoiceLineOption("KELUXIER-3星结束行动", "res://src/keluxier/audio/3星结束行动.wav"),
            new VoiceLineOption("KELUXIER-信赖触摸", "res://src/keluxier/audio/信赖触摸.wav"),
        }
    }; 

    public static bool Prefix(string sfx, float volume)
    {
        // 总开关：置 true 完全关闭本 mod 的商人语音接管（交给原版/其它 mod）
        if (DisableMerchantAudio)
        {
            return true;
        }

        // 只接管"我们确有映射"的商人语音 key。原先用 Contains("merchant") 过宽，
        // 会静音/误伤一切含 merchant 但未映射的音效（含其它 mod 的 merchant 语音），
        // 现在未命中映射的（含其它 merchant 音效）一律放行原方法。
        if (!IsMappedMerchantSfx(sfx))
        {
            return true; // 继续原方法
        }

        // 假商人事件（反转商人/坎诺特）内不接管：交回原版商人语音，
        // 只有真商店（可露希尔）才替换语音。
        if (MerchantAudioContext.InsideFakeMerchantEvent())
        {
            return true;
        }

        PlayAudio(sfx);
        return false; // 阻止原方法执行
    }

    /// <summary>sfx 名是否命中我们映射的任一商人语音 key（即确实有对应音频可播）。</summary>
    private static bool IsMappedMerchantSfx(string sfx)
    {
        foreach (string key in merchantSfx.Keys)
        {
            if (sfx.Contains(key))
            {
                return true;
            }
        }
        return false;
    }

    private static void PlayAudio(string sfx)
    {
        var (Path, streamPath) = GetRandomLineAndVoice(sfx);
        linePath = Path;
        // 从场景树中得到音频节点
        var tree = (SceneTree)Engine.GetMainLoop();
        AudioStreamPlayer2D? voicePlayer = FindVoicePlayer(tree.Root);
        if (voicePlayer == null)
        {
            Log.Info("[wyu][AudioMod] AudioStreamPlayer2D not found under Visuals");
            return;
        }

        AudioStream? stream = ResourceLoader.Load<AudioStream>(streamPath);
        if (stream != null)
        {
            voicePlayer.Stream = stream;
            Log.Info("[wyu][AudioMod] stream loaded and assigned");
        }
        if (voicePlayer.Playing)
        {
            voicePlayer.Stop();
        }
        voicePlayer.Play();
        
        // 气泡显示替换

    }

    private static AudioStreamPlayer2D? FindVoicePlayer(Node root)
    {
        if (root is AudioStreamPlayer2D direct)
        {
            return direct;
        }

        for (int i = 0; i < root.GetChildCount(); i++)
        {
            Node child = root.GetChild(i);
            AudioStreamPlayer2D? found = FindVoicePlayer(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static (LocString line, string? streamPath) GetRandomLineAndVoice(string sfx)
    {
        foreach (var key in merchantSfx.Keys)
        {
            if (sfx.Contains(key))
            {
                List<VoiceLineOption> options = merchantSfx[key].ToList();
                if (options.Count == 0)
                {
                    return (new LocString("characters", "NULL"), null);
                }
                int randomIndex = Random.Shared.Next(options.Count);
                return (new LocString("characters", options[randomIndex].LocKey), options[randomIndex].AudioPath);
            }
        }
        return (new LocString("characters", "NULL"), null);
    }


}


[HarmonyPatch(typeof(NMerchantDialogue), "ShowRandom", new[] { typeof(IEnumerable<LocString>) })]
public static class ShoperDialogueManager
{
    static bool Prefix(NMerchantDialogue __instance, IEnumerable<LocString> lines)
    {
        // 假商人事件：不替换对白，走原版（避免残留可露希尔台词显示在坎诺特头上）
        if (MerchantAudioContext.InsideFakeMerchantEvent())
        {
            return true;
        }

        var customLine = ShoperAudioManager.linePath;
        if (customLine == null)
            return true;

        var locString = customLine;
        if (locString == null)
            return false;

        // ── 读取 NMerchantDialogue 私有字段 ──
        var _label = Traverse.Create(__instance).Field<MegaRichTextLabel>("_label").Value;
        var _bubble = Traverse.Create(__instance).Field<Sprite2D>("_bubble").Value;
        var _dialogueBox = Traverse.Create(__instance).Field<Node2D>("_dialogueBox").Value;
        var _tweenTraverse = Traverse.Create(__instance).Field<Tween>("_tween");

        if (_label == null || _bubble == null || _dialogueBox == null)
            return false;

        // ── 静态字段 _xRange ──
        var xRange = Traverse.Create(typeof(NMerchantDialogue)).Field<Vector2>("_xRange").Value;

        // ── ① 设置文字 ──
        _label.Text = "[fly_in]" + locString.GetFormattedText() + "[/fly_in]";

        // ── ② 初始状态 ──
        __instance.Modulate = StsColors.transparentWhite;
        __instance.Position = new Vector2(
            Rng.Chaotic.NextFloat(xRange.X, xRange.Y),
            __instance.Position.Y);

        // ── ③ 杀掉旧 tween ──
        var oldTween = _tweenTraverse.Value;
        if (oldTween != null && GodotObject.IsInstanceValid(oldTween))
            oldTween.Kill();

        // ── ④ 创建新 tween（只改 TweenInterval 时长） ──
        var tween = __instance.CreateTween().SetParallel();
        tween.TweenProperty(__instance, "modulate:a", 1f, 0.25);
        tween.TweenProperty(__instance, "scale", Vector2.One, 0.25)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(_label, "visible_ratio", 1f, 0.4).From(0f);
        tween.TweenProperty(_bubble, "scale", new Vector2(0.75f, 0.75f), 0.5)
            .From(new Vector2(0.25f, 0.25f))
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        tween.TweenProperty(_dialogueBox, "position:y", 0f, 0.5)
            .From(-80f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
        tween.Chain();
        tween.TweenInterval(3.0);  // ←══════ 停留时长，改这里 ══════→
        tween.Chain();
        tween.TweenProperty(__instance, "modulate:a", 0f, 0.5)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);

        // ── ⑤ 存回 _tween 字段 ──
        _tweenTraverse.Value = tween;

        return false;
    }
}

/// <summary>
/// 判断当前是否处于"假商人事件"（反转商人）中：事件场景里会实例化 NFakeMerchant。
/// 真商店场景不含它，据此区分真假商人，决定语音是否接管。
/// </summary>
internal static class MerchantAudioContext
{
    public static bool InsideFakeMerchantEvent()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        if (tree == null || !GodotObject.IsInstanceValid(tree.Root))
        {
            return false;
        }
        return ContainsNode<NFakeMerchant>(tree.Root);
    }

    private static bool ContainsNode<T>(Node root) where T : Node
    {
        if (root is T)
        {
            return true;
        }
        for (int i = 0; i < root.GetChildCount(); i++)
        {
            Node child = root.GetChild(i);
            if (ContainsNode<T>(child))
            {
                return true;
            }
        }
        return false;
    }
}
