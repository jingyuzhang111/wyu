using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace wyu.wyuCode;

public class wyuKeywords
{
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword WOLF;

    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword JIANREN;
}
