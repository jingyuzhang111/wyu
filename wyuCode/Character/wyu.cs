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
	public override int StartingHp => 70;
	
	public override IEnumerable<CardModel> StartingDeck => [
		ModelDb.Card<Attack>(),
		ModelDb.Card<Attack>(),
		ModelDb.Card<Attack>(),
		ModelDb.Card<Attack>(),
		ModelDb.Card<Block>(),
		ModelDb.Card<Block>(),
		ModelDb.Card<Block>(),
		ModelDb.Card<Block>(),
		ModelDb.Card<Warrior>(),
		ModelDb.Card<YouXuYouYan>(),
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
}
