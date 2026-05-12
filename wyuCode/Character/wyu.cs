using BaseLib.Abstracts;
using wyu.wyuCode.Extensions;
using Godot;
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
	public override int StartingHp => 75;
	
	public override IEnumerable<CardModel> StartingDeck => [

		// 基础卡牌暂定为这四种
		// ModelDb.Card<Warrior>(),
		// ModelDb.Card<JianHao>(),
		// ModelDb.Card<Attack>(),
		// ModelDb.Card<Attack>(),
		// ModelDb.Card<Attack>(),
		// ModelDb.Card<Attack>(),
		// ModelDb.Card<Block>(),
		// ModelDb.Card<Block>(),
		// ModelDb.Card<Block>(),
		// ModelDb.Card<Block>(),


		ModelDb.Card<Laugh>(),
		ModelDb.Card<JiLi>(),
		ModelDb.Card<GetEnergy>(),
		ModelDb.Card<ZuoLeT1>(),
		ModelDb.Card<GreatWall>(),
		ModelDb.Card<JianHaoForm>(),


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
	public override string CustomVisualPath => "res://wyu/Scenes/creatureVisual/testVisual.tscn";
	public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
	public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
	public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
	public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();

	// 角色选择页面替换
	public override string CustomCharacterSelectBg => "res://scenes/screens/char_select/char_select_bg_wyu.tscn";
}
