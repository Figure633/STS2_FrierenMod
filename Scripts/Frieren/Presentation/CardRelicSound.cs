using BaseLib.Audio;

namespace FrierenMod.Scripts.Presentation;

// 独立于BGM及成就提示音；复用BaseLib的原生音效总线和音量设置。
public static class CardRelicSound
{
    public static void Play(string file)
    {
        if (PresentationSettings.CardRelicSounds)
            ModAudio.PlaySoundGlobal("res://FrierenMod/audio/" + file);
    }
}
