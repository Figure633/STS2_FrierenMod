using BaseLib.Abstracts;
using FrierenMod.Scripts.Characters;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Events;

// 随机事件共用原生选项、卡牌获得动画与结束页；不绕过事件同步器。
public abstract class FrierenStoryEvent : CustomEventModel
{
    public override bool IsAllowed(IRunState runState) => Encounters.FrierenEncounterEligibility.IsFrierenRun(runState);
    protected abstract string[] Portraits { get; }
    public override string CustomInitialPortraitPath => PortraitPath(Portraits[0]);
    private static string PortraitPath(string name) => $"res://FrierenMod/images/events/{name}.png";
    public override IEnumerable<string> GetAssetPaths(IRunState runState) => base.GetAssetPaths(runState).Concat(Portraits.Select(PortraitPath));
    protected EventOption Choice(string key, Func<Task> action, bool available = true) =>
        new(this, available ? action : null, $"{Id.Entry}.pages.INITIAL.options.{key}");
    protected void Finish(string page, int portrait)
    {
        SetEventFinished(PageDescription(page));
        ShowPortrait(portrait);
    }
    protected void ShowPortrait(int index)
    {
        if (LocalContext.IsMe(Owner!) && NEventRoom.Instance != null)
            NEventRoom.Instance.SetPortrait(ResourceLoader.Load<Texture2D>(PortraitPath(Portraits[index])));
    }
    protected async Task AddCard<T>() where T : CardModel
    {
        var result = await CardPileCmd.Add(Owner!.RunState.CreateCard<T>(Owner!), PileType.Deck);
        CardCmd.PreviewCardPileAdd(result, 1.2f, CardPreviewStyle.EventLayout);
    }
}
