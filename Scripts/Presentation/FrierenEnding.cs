using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using FrierenMod.Scripts.Characters;

namespace FrierenMod.Scripts.Presentation;

[HarmonyPatch(typeof(NGameOverScreen), nameof(NGameOverScreen._Ready))]
public static class FrierenEnding
{
    public static void Postfix(NGameOverScreen __instance, RunState ____runState)
    {
        // 建筑师属于二代当前的阶段结算，保留原生背景和声音，不播放一代终幕。
        if (!FrierenMusic.Applies(____runState) || ____runState.CurrentRoom?.IsVictoryRoom == true) return;
        NRunMusicController.Instance?.StopMusic();
        if (PresentationSettings.OriginalMusic)
        {
            var music = new FrierenMusic.MusicSession(__instance);
            music.Play(FrierenMusic.Folder + "Frieren_DeathStinger.mp3", loop: false);
        }
    }
}

// 一代 DeathStinger 是 83 秒结算配乐，并非死亡瞬间音效；在结算页按 BGM 音量播放。
[HarmonyPatch(typeof(SfxCmd), nameof(SfxCmd.PlayDeath), new Type[] { typeof(Player) })]
public static class FrierenDeathSoundPatch
{
    [HarmonyPrefix]
    public static bool Prefix(Player player) => player.Character is not Frieren;
}
