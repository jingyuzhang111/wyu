using BaseLib.Abstracts;
using wyu.wyuCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

using wyu.wyuCode.Cards;
using wyu.wyuCode.Relics;
using MegaCrit.Sts2.Core.Modding;

namespace wyu.wyuCode.Character;


public class wyu : PlaceholderCharacterModel
{
	public const string CharacterId = "wyu";
	
	public static readonly Color Color = new("ffffff");

	public override Color NameColor => Color;
	public override CharacterGender Gender => CharacterGender.Neutral;
	public override int StartingHp => 80;
	
	public override IEnumerable<CardModel> StartingDeck => [

		// 基础卡牌暂定为这四种
		ModelDb.Card<Warrior>(),
		ModelDb.Card<JianHao>(),
		ModelDb.Card<Attack>(),
		ModelDb.Card<Attack>(),
		ModelDb.Card<Attack>(),
		ModelDb.Card<Attack>(),
		ModelDb.Card<Block>(),
		ModelDb.Card<Block>(),
		ModelDb.Card<Block>(),
		ModelDb.Card<Block>(),


		// ModelDb.Card<Laugh>(),
		// ModelDb.Card<MaEnNa>(),

		// ModelDb.Card<GreatWall>(),
		// ModelDb.Card<Zc325>(),
		// ModelDb.Card<PeiPei>(),


	];

	public override IReadOnlyList<RelicModel> StartingRelics =>
	[
		ModelDb.Relic<JueShi>(),
		ModelDb.Relic<Brother>(),
	];
	
	public override CardPoolModel CardPool => ModelDb.CardPool<wyuCardPool>();
	public override RelicPoolModel RelicPool => ModelDb.RelicPool<wyuRelicPool>();
	public override PotionPoolModel PotionPool => ModelDb.PotionPool<wyuPotionPool>();
	
	/*  PlaceholderCharacterModel will utilize placeholder basegame assets for most of your character assets until you
		override all the other methods that define those assets. 
		These are just some of the simplest assets, given some placeholders to differentiate your character with. 
		You don't have to, but you're suggested to rename these images. */

	// 加载角色模型
	public override string CustomVisualPath => "res://wyu/Scenes/creatureVisual/testVisual.tscn";
	public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
	public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
	public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
	public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();
	
	public string ArmPointingTexturePath => "res://src/zc/325.png";

	// 角色选择页面替换
	public override string CustomCharacterSelectBg => "res://scenes/screens/char_select/char_select_bg_wyu.tscn";

	// 玩家(羽毛笔/yumaobi2)动画：与怪物替换(MonsterAnimatorPatch)完全相同的 AnimState + AddAnyState 写法。
	// 羽毛笔 char_421_crow.skel 动画名：idle_loop / attack / hurt / die / Skill_1 / Skill_2_*。
	// 游戏默认名 idle_loop/attack/hurt/die 全部匹配，只有 cast 缺失 → 用 Skill_1 兜底，避免播放不存在动画卡 default。
	public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
	{
		AnimState idle   = new AnimState("idle_loop", isLooping: true);   // 待机(循环)
		AnimState attack = new AnimState("attack", isLooping: false);     // 攻击
		AnimState hurt   = new AnimState("hurt", isLooping: false);       // 受击(羽毛笔有 hurt)
		AnimState die    = new AnimState("die", isLooping: false);        // 死亡(播完停末尾)
		AnimState cast   = new AnimState("Skill_1", isLooping: false);    // 施法(无 cast,用 Skill_1)

		// 动作播完自动回到待机
		attack.NextState = idle;
		hurt.NextState = idle;
		cast.NextState = idle;
		// die 不设 NextState → 停在死亡姿势

		CreatureAnimator animator = new CreatureAnimator(idle, controller);
		animator.AddAnyState("Idle", idle);
		animator.AddAnyState("Attack", attack);
		animator.AddAnyState("Hit", hurt);
		animator.AddAnyState("Dead", die);
		animator.AddAnyState("Cast", cast);

		return animator;
	}
}
