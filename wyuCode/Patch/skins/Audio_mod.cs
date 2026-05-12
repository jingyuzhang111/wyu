using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace wyu.wyuCode.Patch;

using wyu.wyuCode.Cards;


// 自动注入
[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed), new[] { typeof(CombatState), typeof(CardPlay) })]
public static class AudioMod
{
    private sealed record VoiceLineOption(string LocKey, string AudioPath);

    private static readonly LocString FallbackLine = new("characters", "WYU-ATTACK.test");
    private static readonly Dictionary<CardType, VoiceLineOption[]> VoiceLinePools = new()
    {
        [CardType.Attack] =
        [
            new VoiceLineOption("WYU-ATTACK.type.attack", "res://src/yumaobi2/audio/作战中1.wav"),
            new VoiceLineOption("WYU-ATTACK.type.attack.alt1", "res://src/yumaobi2/audio/作战中2.wav"),
            new VoiceLineOption("WYU-ATTACK.type.skill.alt1", "res://src/yumaobi2/audio/行动开始.wav")
        ],
        [CardType.Skill] =
        [
            new VoiceLineOption("WYU-ATTACK.type.skill", "res://src/yumaobi2/audio/作战中4.wav"),
            new VoiceLineOption("WYU-ATTACK.type.skill.alt1", "res://src/yumaobi2/audio/行动开始.wav")
        ],
        [CardType.Power] =
        [
            new VoiceLineOption("WYU-ATTACK.type.power", "res://src/yumaobi2/audio/部署1.wav"),
            new VoiceLineOption("WYU-ATTACK.type.power.alt1", "res://src/yumaobi2/audio/选中干员2.wav")
        ]
    };

    private static void Postfix(CombatState combatState, CardPlay cardPlay)
    {
        if (cardPlay?.Card?.Owner?.Character == null)
        {
            Log.Info("[wyu][AudioMod] cardPlay or owner character is null");
            return;
        }

        string? charId = cardPlay.Card.Owner.Character.Id?.Entry;
        Log.Info($"[wyu][AudioMod] 命中 BeforeCardPlayed, charId={charId}, card={cardPlay.Card}");

        bool isWyu = cardPlay.Card.Owner.Character is wyu.wyuCode.Character.wyu
            || string.Equals(charId, wyu.wyuCode.Character.wyu.CharacterId, System.StringComparison.OrdinalIgnoreCase);
        if (!isWyu)
        {
            return;
        }

        Log.Info("[wyu][AudioMod] 尝试进行语音播放，卡牌名称: " + cardPlay.Card);
        PlaySpeechAndVoice(cardPlay);
    }

    private static void PlaySpeechAndVoice(CardPlay cardPlay)
    {
        
        (LocString line, string? streamPath) = GetRandomLineAndVoice(cardPlay);
        TalkCmd.Play(line, cardPlay.Card.Owner.Creature, vfxColor: VfxColor.White);
        Log.Info($"[wyu][AudioMod] TalkCmd.Play type={cardPlay.Card.Type}, loc={line.LocEntryKey}");

        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(cardPlay.Card.Owner.Creature);
        if (ownerNode?.Visuals == null)
        {
            Log.Info("[wyu][AudioMod] ownerNode.Visuals is null");
            return;
        }

        AudioStreamPlayer2D? voicePlayer = FindVoicePlayer(ownerNode.Visuals);
        if (voicePlayer == null)
        {
            Log.Info("[wyu][AudioMod] AudioStreamPlayer2D not found under Visuals");
            return;
        }

        bool streamExists = !string.IsNullOrEmpty(streamPath) && ResourceLoader.Exists(streamPath);
        Log.Info($"[wyu][AudioMod] streamPath={streamPath}, exists={streamExists}");
        if (streamExists)
        {
            AudioStream? stream = ResourceLoader.Load<AudioStream>(streamPath);
            if (stream != null)
            {
                voicePlayer.Stream = stream;
                Log.Info("[wyu][AudioMod] stream loaded and assigned");
            }
        }

        if (voicePlayer.Playing)
        {
            voicePlayer.Stop();
        }

        voicePlayer.Play();
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

    private static (LocString line, string? streamPath) GetRandomLineAndVoice(CardPlay cardPlay)
    {
        if (cardPlay?.Card is WaGaAttack)
        {
            return (new LocString("characters", "WAGAWAGA"), "res://src/ailini/作战中1.wav");
        }


        CardType cardType = cardPlay.Card.Type;
        if (!VoiceLinePools.TryGetValue(cardType, out VoiceLineOption[]? candidates) || candidates.Length == 0)
        {
            return (GetDefaultLineForCardType(cardType), null);
        }

        List<VoiceLineOption> valid = new(candidates.Length);
        foreach (VoiceLineOption option in candidates)
        {
            if (LocString.Exists("characters", option.LocKey) && ResourceLoader.Exists(option.AudioPath))
            {
                valid.Add(option);
            }
        }

        if (valid.Count == 0)
        {
            return (GetDefaultLineForCardType(cardType), null);
        }

        VoiceLineOption selected = valid[Random.Shared.Next(valid.Count)];
        return (new LocString("characters", selected.LocKey), selected.AudioPath);
    }

    private static LocString GetDefaultLineForCardType(CardType cardType)
    {
        string key = cardType switch
        {
            CardType.Attack => "WYU-ATTACK.type.attack",
            CardType.Skill => "WYU-ATTACK.type.skill",
            CardType.Power => "WYU-ATTACK.type.power",
            _ => "WYU-ATTACK.test"
        };

        return LocString.Exists("characters", key)
            ? new LocString("characters", key)
            : FallbackLine;
    }
}
