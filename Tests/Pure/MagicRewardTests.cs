using FrierenMod.Core;

public static class MagicRewardTests
{
    public static int Run()
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS 掉落：" + name); }
        int Drops(int pity, bool elite = false, bool escaped = false) => Enumerable.Range(0, 100)
            .Count(roll => MagicRewardRules.ShouldDrop(roll, pity, elite, escaped));
        Check(Drops(0)==40 && Drops(0, true)==70 && Drops(20)==60 && Drops(-10, true)==60,
            "普通40%、精英70%，补偿按百分点累加");
        Check(Drops(100)==100 && Drops(-100)==0 && Drops(100, escaped:true)==0 && Drops(0, true, true)==70,
            "概率上下界与普通逃跑不掉落，精英保留一代例外");
        int[] Distribution(int offset) => Enum.GetValues<MagicRewardRarity>().Select(rarity => Enumerable.Range(0,100)
            .Count(roll => MagicRewardRules.Rarity(roll, offset)==rarity)).ToArray();
        Check(Distribution(5).SequenceEqual([35,15,0,50]) && Distribution(-15).SequenceEqual([15,17,18,50]) &&
            Distribution(-35).SequenceEqual([0,17,38,45]), "普通减20后的完整分类概率正确");
        Check(MagicRewardRules.AfterOffer(5, MagicRewardRarity.Common)==-15 &&
            MagicRewardRules.AfterOffer(-15, MagicRewardRarity.Common)==-35 &&
            MagicRewardRules.AfterOffer(-35, MagicRewardRarity.Common)==-40 &&
            MagicRewardRules.AfterOffer(-40, MagicRewardRarity.Rare)==5, "普通补偿减20、最低-40、稀有重置+5");
        Check(MagicRewardRules.AfterOffer(-15, MagicRewardRarity.Prop)==-15 &&
            MagicRewardRules.AfterOffer(-15, MagicRewardRarity.Uncommon)==-15, "魔具和罕见不改稀有度补偿");
        var draws = new Queue<int>([99,0,99,0,0,0]);
        var offer = MagicRewardRules.Offer(5, max => { int value=draws.Dequeue(); if(value>=max)throw new Exception("越界随机数"); return value; });
        Check(offer.Choices.Length==3 && offer.Choices.Distinct().Count()==3 && offer.RarityOffset==5 &&
            SpellCatalog.Get(offer.Choices[0]).Rarity==FactorRarity.Common && SpellCatalog.Get(offer.Choices[1]).Rarity==FactorRarity.Common &&
            SpellCatalog.Get(offer.Choices[2]).Rarity==FactorRarity.Rare, "同一组三选一逐项更新补偿，且不重复");
        offer = MagicRewardRules.Offer(5, max => max==100 ? 40 : 0);
        Check(offer.Choices.Distinct().Count()==3 && offer.Choices.All(id=>MagicPropRules.All.Any(p=>p.Id==id)) && offer.RarityOffset==5,
            "魔具可组成三选一，使用八种可掉落池并排除教程");
        foreach(var puzzle in SpellCatalog.All.Where(f=>f.Puzzle)) {
            int offset = puzzle.Rarity==FactorRarity.Rare ? -15 : 5;
            int roll = puzzle.Rarity==FactorRarity.Common ? 99 : 0;
            offer = MagicRewardRules.Offer(offset, max=>max==100 ? roll : max-1);
            Check(offer.Choices.Contains(puzzle.Id), "沿用一代默认允许谜题因子掉落："+puzzle.Id);
        }
        var deck = new MagicDeckState { RewardPity=20, RewardRarityOffset=-35 };
        var restored=MagicDeckState.FromJson(deck.ToJson());
        Check(restored.RewardPity==20 && restored.RewardRarityOffset==-35 &&
            MagicDeckState.FromJson("{\"Version\":3,\"Items\":[]}").RewardRarityOffset==5,
            "两种补偿独立保存，旧档稀有度补偿默认+5");
        return count;
    }
}
