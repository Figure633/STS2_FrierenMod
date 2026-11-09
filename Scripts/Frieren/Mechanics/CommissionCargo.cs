using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace FrierenMod.Scripts.Mechanics;

public static class CommissionCargo
{
    public static string Location(Player p)=>$"{p.RunState.CurrentActIndex}/{p.RunState.TotalFloor}";
    public static ValuableCargo[] Goods(Player p)=>p.Deck.Cards.OfType<ValuableCargo>().ToArray();
    public static int Freight(Player p)=>Goods(p).Sum(c=>c.Fare);
    public static async Task Add(Player p,int contract){
        var card=p.RunState.CreateCard<ValuableCargo>(p);card.ContractId=contract;card.LastStop=Location(p);card.Fare=0;
        var result=await CardPileCmd.Add(card,PileType.Deck);CardCmd.PreviewCardPileAdd(result);
    }
    public static void Damaged(Creature target,DamageResult result){
        if(target.Player is not {} p||result.UnblockedDamage<=0)return;
        var goods=Goods(p);
        var copies=p.PlayerCombatState?.AllPiles.SelectMany(pile=>pile.Cards).OfType<ValuableCargo>().Distinct().ToArray()??[];
        if(goods.Length==0&&copies.Length==0)return;
        foreach(var card in goods)card.Fare-=2;
        // 战斗牌堆中的原生副本跟随对应主牌组实例；临时复制品只修改自己的临时值。
        foreach(var card in copies)
            card.Fare=card.DeckVersion is ValuableCargo original?original.Fare:Math.Max(0,card.Fare-2);
        p.PlayerCombatState?.Hand.InvokeContentsChanged();
    }
    public static async Task Remove(Player p,int? contract=null){
        var combat=p.PlayerCombatState?.AllPiles.SelectMany(pile=>pile.Cards).OfType<ValuableCargo>()
            .Where(c=>contract==null||c.ContractId==contract).Cast<CardModel>().ToArray()??[];
        if(combat.Length>0)await CardPileCmd.RemoveFromCombat(combat);
        var cards=Goods(p).Where(c=>contract==null||c.ContractId==contract).Cast<CardModel>().ToArray();
        if(cards.Length>0)await CardPileCmd.RemoveFromDeck(cards);
    }
    public static async Task Visit(Player p,AbstractRoom room){
        var state=CommissionCmd.State(p);
        // 放弃、过幕和旧版迁移都只能归还货物，不能兑现运费。
        foreach(int contract in Goods(p).Where(c=>c.ContractId>0&&!state.Quests.Any(q=>q.Id==c.ContractId&&q.Status==CommissionStatus.Accepted)).Select(c=>c.ContractId).Distinct().ToArray())await Remove(p,contract);
        bool advanced=false;
        foreach(var card in Goods(p))if(card.LastStop!=Location(p)){card.LastStop=Location(p);card.Fare+=20;advanced=true;}
        // 在商人处接取/读档不算送达“下一位商人”，必须真的经过新地点。
        if(room is not MerchantRoom||!advanced)return;
        var goods=Goods(p);var active=state.Active;
        if(goods.Length==0&&active?.Goal!=CommissionGoal.Cargo)return;
        int fare=goods.Sum(c=>c.Fare)+(active?.BaseGold??0);
        if(active?.Goal==CommissionGoal.Cargo){active.Status=goods.Length>0?CommissionStatus.Claimed:CommissionStatus.Abandoned;active.PaidFreight=fare;active.Progress=goods.Length>0?1:0;state.Revision++;CommissionCmd.Write(p,state);}
        // 先取快照并锁定交付状态，原生移除与金币命令只执行一次。
        await Remove(p);
        if(fare>0)await PlayerCmd.GainGold(fare,p);
    }
}
