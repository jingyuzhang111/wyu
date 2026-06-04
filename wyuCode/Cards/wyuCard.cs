using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using wyu.wyuCode.Character;
using wyu.wyuCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Entities.Creatures;
using System.Collections.Concurrent;
namespace wyu.wyuCode.Cards;

[Pool(typeof(wyuCardPool))]
public abstract class wyuCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    //Image size:
    //Normal art: 1000x760 (Using 500x380 should also work, it will simply be scaled.)
    //Full art: 606x852
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    
    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190
    
    //Uses card_portraits/card_name.png as image path. These should be smaller images.
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    // 修改为可修改属性
    public virtual string[] mytypes { get; set; } = ["wyu"];

    private static readonly ConcurrentDictionary<Type, Func<Creature, decimal>> CurrentHpReaders = new();


    protected static decimal ReadCurrentHp(Creature creature)
    {
        var reader = CurrentHpReaders.GetOrAdd(creature.GetType(), BuildCurrentHpReader);
        return reader(creature);
    }


    private static Func<Creature, decimal> BuildCurrentHpReader(Type type)
    {
        var property = type.GetProperty("CurrentHp") ?? type.GetProperty("CurrentHealth");
        if (property != null)
        {
            return creature =>
            {
                object? value = property.GetValue(creature);
                return value != null ? Convert.ToDecimal(value) : 0m;
            };
        }

        var field = type.GetField("CurrentHp") ?? type.GetField("CurrentHealth");
        if (field != null)
        {
            return creature =>
            {
                object? value = field.GetValue(creature);
                return value != null ? Convert.ToDecimal(value) : 0m;
            };
        }

        return _ => 0m;
    }
}