using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Audio;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using BaseLib.Utils;
using HarmonyLib;
using wyu.wyuCode.Powers;



namespace wyu.wyuCode.Cards;

public class PeiPei():
    wyuCard(cost: 0, 
    type: CardType.Skill,
    rarity: CardRarity.Event,
    target: TargetType.Self
    )
{
    // 自定义边框
    // public override bool HasBuiltInOverlay => true;
    

    // 数值调整的地方, 可添加各种具体效果,定义牌的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        PlayBGM("res://src/peipei/佩佩ep.mp3");
        // 抽牌
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);

    }

    // 升级
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.BaseValue += 1;
    }

    private static void PlayBGM(string path)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        AudioStreamPlayer player = new AudioStreamPlayer { Name = "WyuBGMPlayer" };
        tree.Root.AddChild(player);


        // 关键：用 Load 而不是 Exists 的结果
        var stream = ResourceLoader.Load<AudioStream>(path);
        GD.Print($"[BGM] stream 加载结果: {stream != null}");
        
        if (stream == null) return;
        
        if (player.Playing) player.Stop();
        player.Stream = stream;
        player.VolumeDb = -10; // 可选：调整音量
        player.Play();
        NRunMusicController.Instance?.StopMusic();
        GD.Print("[BGM] 播放开始");
    }

    public static void StopBGM()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var player = tree.Root.GetNodeOrNull<AudioStreamPlayer>("WyuBGMPlayer");
        if (player != null && player.Playing)
        {
            player.Stop();
            NRunMusicController.Instance?.UpdateMusic();
            GD.Print("[BGM] 播放停止");
        }
    }

}
