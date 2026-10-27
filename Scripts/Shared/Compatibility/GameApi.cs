using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.ValueProps;
using System.Reflection;

namespace FrierenMod.Scripts.Shared.Compatibility;

// 以正式版公共类型编译；只在这里解析两版不同的命令签名。
public static class GameApi
{
    private static readonly VariableMethod DamageOne = DamageMethod(typeof(Creature));
    private static readonly VariableMethod DamageMany = DamageMethod(typeof(IEnumerable<Creature>));
    private static VariableMethod DamageMethod(Type target) => new(
        (typeof(CreatureCmd), "Damage", [typeof(PlayerChoiceContext), target, typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay)], [0,1,2,3,4,5,6]),
        (typeof(CreatureCmd), "Damage", [typeof(PlayerChoiceContext), target, typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel)], [0,1,2,3,4,5]));

    public static Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext context, Creature target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? card, CardPlay? play = null) =>
        DamageOne.Invoke<Task<IEnumerable<DamageResult>>>(null,context,target,amount,props,dealer,card,play)!;
    public static Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext context, IEnumerable<Creature> targets, decimal amount,
        ValueProp props, Creature? dealer, CardModel? card, CardPlay? play = null) =>
        DamageMany.Invoke<Task<IEnumerable<DamageResult>>>(null,context,targets,amount,props,dealer,card,play)!;

    private static readonly VariableMethod LoseBlockMethod = new(
        (typeof(CreatureCmd), "LoseBlock", [typeof(PlayerChoiceContext),typeof(Creature),typeof(decimal),typeof(Creature)], [0,1,2,3]),
        (typeof(CreatureCmd), "LoseBlock", [typeof(Creature),typeof(decimal)], [1,2]));
    public static Task LoseBlock(PlayerChoiceContext context, Creature target, decimal amount, Creature? remover) =>
        LoseBlockMethod.Invoke<Task>(null,context,target,amount,remover)!;

    // 参数相同但返回值由 Task 改为 Task<CardPileAddResult?>，也会改变 CLR 方法签名。
    private static readonly VariableMethod ExhaustMethod = new((typeof(CardCmd),"Exhaust",
        [typeof(PlayerChoiceContext),typeof(CardModel),typeof(bool),typeof(bool)],[0,1,2,3]));
    public static Task Exhaust(PlayerChoiceContext context, CardModel card, bool causedByEthereal=false, bool skipVisuals=false) =>
        ExhaustMethod.Invoke<Task>(null,context,card,causedByEthereal,skipVisuals)!;

    public static ulong Seed(object rng) => Convert.ToUInt64(AccessTools.Property(rng.GetType(), "Seed").GetValue(rng));
    private static readonly ConstructorInfo? NewRng = typeof(MegaCrit.Sts2.Core.Random.Rng).GetConstructor([typeof(ulong)]);
    private static readonly ConstructorInfo? OldRng = typeof(MegaCrit.Sts2.Core.Random.Rng).GetConstructor([typeof(uint),typeof(int)]);
    public static MegaCrit.Sts2.Core.Random.Rng CreateRng(ulong seed) => (MegaCrit.Sts2.Core.Random.Rng)
        (NewRng!=null ? NewRng.Invoke([seed]) : OldRng!.Invoke([unchecked((uint)seed),0]));

    public static readonly Type ExhaustType = AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Vfx.Cards.NCardExhaustVfx")
        ?? AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Vfx.Cards.NExhaustVfx")
        ?? throw new MissingMemberException("找不到原生消耗特效");
    public static readonly MethodInfo ExhaustCreate = AccessTools.Method(ExhaustType,"Create");
    private static readonly MethodInfo? ExhaustPlay = AccessTools.Method(ExhaustType,"PlayAnimation");
    public static bool ExhaustOwnsCard => ExhaustPlay != null;
    public static bool IsExhaust(Node node) => ExhaustType.IsInstanceOfType(node);
    public static CanvasItem? CreateExhaust(NCard card) => (CanvasItem?)ExhaustCreate.Invoke(null,[card]);
    public static void PlayExhaust(CanvasItem effect)
    {
        if (ExhaustPlay == null) return; // 正式版在 _Ready 中自动播放，卡牌仍归调用方。
        NDebugAudioManager.Instance?.Play("card_exhaust.mp3");
        _ = MegaCrit.Sts2.Core.Helpers.TaskHelper.RunSafely((Task)ExhaustPlay.Invoke(effect,null)!);
    }
}
