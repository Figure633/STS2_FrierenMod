using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Shared.Compatibility;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;

// 仅验证捕鸟新方案可复用的原生机制；不改变捕鸟卡及任何玩家存档。
public static class PetFeasibilityTests
{
    public static async Task<int> Run()
    {
        int count=0;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;GD.Print("PASS 捕鸟可行性："+text);}
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"BIRD-PET-FEASIBILITY");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        var combat=new CombatState(runState:run);combat.AddPlayer(player);
        var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        var context=new ThrowingPlayerChoiceContext();
        var bird=await PlayerCmd.AddPet<DampCultist>(player);
        bird.SetCurrentHpInternal(17);
        await PowerCmd.Apply<DieForYouPower>(context,bird,1,player.Creature,null);
        Check(bird.PetOwner==player && bird.Side==CombatSide.Player && !combat.Enemies.Contains(bird),
            "鸟类模型可通过原生AddPet加入友方，不占敌人存活数量");
        int hp=player.Creature.CurrentHp;
        await GameApi.Damage(context,player.Creature,7,ValueProp.Move,enemy,null);
        Check(bird.CurrentHp==10 && player.Creature.CurrentHp==hp,"原生DieForYou可由鸟类承受玩家受到的攻击");
        await GameApi.Damage(context,player.Creature,3,ValueProp.Unblockable|ValueProp.Unpowered,null,null);
        Check(bird.CurrentHp==10 && player.Creature.CurrentHp==hp-3,"失去生命不误当作敌人的攻击拦截");
        await GameApi.Damage(context,player.Creature,15,ValueProp.Move,enemy,null);
        Check(bird.IsDead && player.Creature.CurrentHp==hp-8,"超过鸟类剩余生命的伤害按原生规则溢出至玩家");
        await GameApi.Damage(context,player.Creature,4,ValueProp.Move,enemy,null);
        Check(player.Creature.CurrentHp==hp-12 && bird.IsDead,"鸟类死亡后不继续替伤或自动复活");
        RunManager.Instance.CleanUp();return count;
    }
}
