using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace FrierenMod.Scripts.Cards;

// 运费属于单张主牌组实例；原生深拷贝、DeckVersion和SavedProperty负责复制与存读档。
public sealed class ValuableCargo() : FrierenCard(-1,CardType.Quest,CardRarity.Quest,TargetType.None)
{
    protected override string ArtName => "UnbelievableToolBox";
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Fare",0)];
    [SavedProperty] public int Fare {get=>DynamicVars["Fare"].IntValue;set{AssertMutable();DynamicVars["Fare"].BaseValue=Math.Max(0,value);}}
    private string _lastStop="";
    [SavedProperty] public string LastStop {get=>_lastStop;set{AssertMutable();_lastStop=value;}}
    private int _contract;
    [SavedProperty] public int ContractId {get=>_contract;set{AssertMutable();_contract=value;}}
}
