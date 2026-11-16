using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.UI;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Core;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class SmithGridTests
{
    private static bool SkipStartup()=>false;
    public static async Task<int> Run(Node root)
    {
        int count=0;
        void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;GD.Print("PASS 锻造网格："+message);}
        async Task Tick(){for(int i=0;i<4;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);}
        SaveManager.Instance.InitPrefsDataForTest();LocalContext.NetId=1;
        var patch=new Harmony("frieren.tests.smith-grid");
        patch.Patch(AccessTools.Method(typeof(NCard),nameof(NCard.Create)),prefix:new HarmonyMethod(typeof(LibraryUiTests),"CreateCard"));
        patch.Patch(AccessTools.Method(typeof(NGridCardHolder),nameof(NGridCardHolder.Create)),prefix:new HarmonyMethod(typeof(LibraryUiTests),"CreateHolder"));
        foreach(string method in new[]{nameof(NGame._EnterTree),nameof(NGame._Ready)})
            patch.Patch(AccessTools.Method(typeof(NGame),method),prefix:new HarmonyMethod(typeof(SmithGridTests),nameof(SkipStartup)));
        var game=new NGame{Size=new(1920,1080),ProcessMode=Node.ProcessModeEnum.Disabled};
        var input=new NInputManager();var controller=new NControllerManager();
        AccessTools.Property(typeof(NInputManager),nameof(NInputManager.ControllerManager)).SetValue(input,controller);
        AccessTools.Property(typeof(NGame),nameof(NGame.InputManager)).SetValue(game,input);
        root.AddChild(game);AccessTools.Property(typeof(NGame),nameof(NGame.Instance)).SetValue(null,game);
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"SMITH-GRID");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());run.PushRoom(new RestSiteRoom());
        var scene=GD.Load<PackedScene>("res://scenes/screens/card_selection/deck_upgrade_select_screen.tscn").Instantiate<Control>();
        var grid=scene.GetNode<NCardGrid>("%CardGrid");grid.GetParent().RemoveChild(grid);scene.Free();
        grid.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);root.AddChild(grid);await Tick();
        var rowsField=AccessTools.Field(typeof(NCardGrid),"_cardRows");
        try {
            var bookHost=new Control{Size=new(1920,1080)};root.AddChild(bookHost);
            MagicDeckStore.Write(player,MagicDeckState.CreatePrototypeLoadout());
            var book=new MagicBookScreen(bookHost,player,()=>null);
            for(int pass=0;pass<3;pass++){book.Open();await Tick();book.Close();}
            bookHost.Free();await Tick();
            foreach(string language in new[]{"zhs","eng"}){
                LocManager.Instance.SetLanguage(language);
                var cards=ModelDb.CardPool<FrierenCardPool>().AllCards.Where(c=>c.IsUpgradable&&c is not FactorCard and not MagicPropCard)
                    .Select(c=>run.CreateCard(c,player)).ToArray();
                foreach(var batch in cards.Chunk(10)){
                    GD.Print("SMITH_BATCH: "+string.Join(',',batch.Select(c=>c.Id.Entry)));
                    grid.SetCards(batch,PileType.None,[SortingOrders.Ascending]);await Tick();
                    var holders=((List<List<NGridCardHolder>>)rowsField.GetValue(grid)!).SelectMany(r=>r).ToArray();
                    Check(holders.Length==batch.Length&&holders.All(h=>h.GlobalPosition.Length()>100)&&
                        holders.Select(h=>h.GlobalPosition).Distinct().Count()==holders.Length,"原生网格完整排布 "+language+"/"+batch[0].Id);
                    foreach(var holder in holders){
                        holder.SetIsPreviewingUpgrade(true);holder.CardNode!.UpdateVisuals(PileType.None,CardPreviewMode.Normal);
                        Check(holder.CardNode.Model.IsUpgraded,"原生升级预览 "+holder.CardModel.Id);
                        holder.SetIsPreviewingUpgrade(false);
                    }
                }
            }
        }
        finally {
            grid.Free();await Tick();
            AccessTools.Property(typeof(NGame),nameof(NGame.Instance)).SetValue(null,null);
            RunManager.Instance.CleanUp();
            game.QueueFree();input.Free();controller.Free();patch.UnpatchAll(patch.Id);await Tick();
        }
        return count;
    }
}
