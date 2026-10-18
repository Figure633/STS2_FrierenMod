using BaseLib.Utils;
using BaseLib.Utils.NodeFactories;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Presentation;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Localization;

public static class PresentationTests
{
    private static bool InteractiveAudio(ref bool __result) { __result = false; return false; }
    public static async Task<int> Run(Node root, Player player)
    {
        int count = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            GD.Print("PASS " + name); count++;
        }
        async Task Frames(int n)
        {
            for (int i = 0; i < n; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        var frieren = (Frieren)player.Character;
        // 正式游戏在 ModManager.Initialize 后执行这个 BaseLib 注册回调。
        frieren.RegisterSceneConversions();
        var container = new Control { Name = "PresentationTests", Size = new Vector2(1920, 1080) };
        root.AddChild(container);
        var background = GD.Load<PackedScene>(frieren.CustomCharacterSelectBg).Instantiate<Control>();
        container.AddChild(background);
        Check(background.GetNode<TextureRect>("Portrait").Texture.GetWidth() == 1920, "选人背景使用一代原图");
        var shade = new ColorRect { Color = new Color(0.03f, 0.04f, 0.06f, 0.8f), Size = container.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
        container.AddChild(shade);

        var visuals = frieren.CreateVisuals();
        visuals.Position = new Vector2(680, 750);
        container.AddChild(visuals);
        var meshes = visuals.FindChildren("*", "Polygon2D", true, false).Cast<Polygon2D>().ToArray();
        Check(meshes.Length == 9 && visuals.Bounds.Size.Y == 330 && !visuals.HasSpineAnimation, "BaseLib 实际角色场景转换：9 层原生网格与碰撞框");
        var anim = visuals.GetNode<AnimationPlayer>("AnimationPlayer");
        var hair = visuals.GetNode<Polygon2D>("Visuals/Body/1Hair");
        anim.Play("Idle"); anim.Seek(0, true);
        var initial = hair.Polygon.ToArray();
        anim.Seek(2, true);
        Check(!initial.SequenceEqual(hair.Polygon), "旧骨骼与顶点形变实际驱动待机动画");
        foreach (string state in new[] { "Attack", "Cast", "Hit" })
        {
            Check(CustomAnimation.PlayCustomAnimation(visuals, state), "原生动作入口 " + state);
            anim.Advance(0.6);
            await Frames(2);
            Check(anim.CurrentAnimation == "Idle", state + " 完成后回到待机");
        }
        CustomAnimation.PlayCustomAnimation(visuals, "Dead"); anim.Seek(0.1, true);
        Check(visuals.GetNode<Sprite2D>("Visuals/Corpse").Visible && !visuals.GetNode<Node2D>("Visuals/Body").Visible, "死亡使用原尸体图");
        CustomAnimation.PlayCustomAnimation(visuals, "Idle"); anim.Seek(0, true);
        Check(!visuals.GetNode<Sprite2D>("Visuals/Corpse").Visible && visuals.GetNode<Node2D>("Visuals/Body").Visible, "恢复待机清除死亡画面");

        var merchant = NodeFactory<NMerchantCharacter>.CreateFromScene(frieren.CustomMerchantAnimPath);
        merchant.Position = new Vector2(1080, 750);
        container.AddChild(merchant);
        merchant.PlayAnimation("relaxed_loop", true);
        Check(CustomAnimation.HasCustomAnimation(merchant), "商店使用芙莉莲动画并通过原生入口");
        var rest = NodeFactory<NRestSiteCharacter>.CreateFromScene(frieren.CustomRestSiteAnimPath);
        AccessTools.Property(typeof(NRestSiteCharacter), "Player").SetValue(rest, player);
        rest.Position = new Vector2(1430, 650);
        container.AddChild(rest);
        Check(rest.Hitbox.Size.X == 300, "完整营地坐姿与命中框尺寸");
        Check(rest.Hitbox.MouseFilter == Control.MouseFilterEnum.Stop &&
            rest.Hitbox.GetSignalConnectionList(Control.SignalName.MouseEntered).Count > 0,
            "营地角色命中框接收鼠标，保留原生治疗目标悬停入口");
        var energy = NodeFactory<NEnergyCounter>.CreateFromScene(frieren.CustomEnergyCounterPath);
        AccessTools.Field(typeof(NEnergyCounter), "_player").SetValue(energy, player);
        energy.Position = new Vector2(520, 850);
        container.AddChild(energy);
        energy._Process(0.1);
        Check(energy.GetNode<Control>("%RotationLayers").GetChildren().All(n => n is Control), "能量球原生旋转、数字和失能刷新");
        var icon = frieren.Icon;
        icon.Position = new Vector2(400, 850);
        container.AddChild(icon);
        Check(frieren.CharacterSelectIcon.GetWidth() == 200 && frieren.MapMarker.GetWidth() == 64, "头像、选人按钮和地图标记均替换");

        Check(frieren.CustomAttackSfx.Length == 0 && frieren.CustomCastSfx.Length == 0,
            "杀人魔法语音不再绑定通用攻击、施法或强化动作");
        foreach (string trigger in new[] { "Attack", "Cast", "PowerUp" }) {
            Task task = Task.CompletedTask;
            Check(!FrierenCombatSound.Prefix(player.Creature, trigger, 0, ref task),
                "仅接管芙莉莲通用动作声音：" + trigger);
            await task;
        }
        Task untouched = Task.CompletedTask;
        Check(FrierenCombatSound.Prefix(player.Creature, "Hit", 0, ref untouched),
            "受击等其他动作仍走原生声音入口");
        var mapImage = frieren.MapMarker.GetImage();
        if (mapImage.IsCompressed()) mapImage.Decompress();
        Check(mapImage.GetPixel(0, 0).A == 0 && mapImage.GetUsedRect().Size.X > 30,
            "地图标记保留透明边缘且缩小后主体可见");
        var mapPreview = new TextureRect { Texture = frieren.MapMarker, Position = new Vector2(1360, 880),
            Size = new Vector2(64, 64), MouseFilter = Control.MouseFilterEnum.Ignore };
        container.AddChild(mapPreview);
        foreach (string file in DirAccess.GetFilesAt("res://FrierenMod/audio").Where(f => f.EndsWith(".mp3") || f.EndsWith(".ogg")))
        {
            var stream = GD.Load<AudioStream>(FrierenMusic.Folder + file);
            Check(stream.GetLength() > 0 && stream.InstantiatePlayback() != null, "音频解码 " + file);
        }
        var tracks = new HashSet<string>();
        for (int act = 0; act < 3; act++)
        {
            foreach (uint seed in new uint[] { 0, 1 })
                tracks.Add(FrierenMusic.ResolveTrack(act, RoomType.Monster, true, seed));
            tracks.Add(FrierenMusic.ResolveTrack(act, RoomType.Boss, true, 0));
        }
        tracks.Add(FrierenMusic.ResolveTrack(0, RoomType.Elite, true, 0));
        tracks.Add(FrierenMusic.ResolveTrack(0, RoomType.Shop, false, 0));
        tracks.Add(FrierenMusic.ResolveTrack(0, RoomType.RestSite, false, 0));
        Check(tracks.Count == 12 && tracks.All(p => ResourceLoader.Exists(p)), "三章双曲、首领、精英、商店、营地共 12 条音乐路由");
        Check(FrierenMusic.ResolveTrack(0, RoomType.Boss, false, 0) == FrierenMusic.ResolveTrack(0, RoomType.Monster, false, 0), "战斗结束恢复章节曲");
        var musicRoot = new Node(); container.AddChild(musicRoot);
        var session = new FrierenMusic.MusicSession(musicRoot);
        string first = tracks.First();
        session.Play(first);
        await root.ToSignal(root.GetTree().CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
        var players = musicRoot.GetChildren().OfType<AudioStreamPlayer>().ToArray();
        var active = players.Single(p => p.Playing);
        float position = active.GetPlaybackPosition();
        session.Play(first);
        Check(active.Playing && active.GetPlaybackPosition() >= position, "重复刷新同曲不重播");
        var originalVolume = SaveManager.Instance.SettingsSave.VolumeBgm;
        SaveManager.Instance.SettingsSave.VolumeBgm = 0;
        session.UpdateVolume();
        Check(players.All(p => p.VolumeLinear < 0.0001f), "BGM 静音滑块生效");
        SaveManager.Instance.SettingsSave.VolumeBgm = originalVolume;
        session.Play(tracks.Skip(1).First());
        await root.ToSignal(root.GetTree().CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
        Check(players.Count(p => p.Playing) == 1, "音乐淡入淡出后只保留一条播放流");
        session.Stop();
        Check(players.All(p => !p.Playing && p.Stream == null), "离开跑局停止并释放音频");
        session.Play(FrierenMusic.Folder + "Frieren_DeathStinger.mp3", loop: false);
        Check(players.Any(p => p.Stream is AudioStreamMP3 { Loop: false } mp3 && mp3.GetLength() > 80)
            && !FrierenDeathSoundPatch.Prefix(player), "死亡长曲按结算音乐单次播放，不走死亡音效");
        session.Stop();

        var run = (RunState)player.RunState;
        // 仅解除音乐控制器的非交互短路；TestMode 仍开启，玩家存档继续使用内存实现。
        var audioTestHarmony = new Harmony("frieren.tests.interactive_audio");
        var nonInteractive = AccessTools.PropertyGetter(typeof(MegaCrit.Sts2.Core.Helpers.NonInteractiveMode), "IsActive");
        audioTestHarmony.Patch(nonInteractive, prefix: new HarmonyMethod(typeof(PresentationTests), nameof(InteractiveAudio)));
        run.PushRoom(new RestSiteRoom());
        var controller = new NRunMusicController();
        var proxy = new Node { Name = "Proxy" };
        proxy.SetScript(GD.Load<Script>("res://music_proxy.gd"));
        controller.AddChild(proxy);
        controller.SetRunState(run);
        container.AddChild(controller);
        controller.UpdateMusic();
        controller.UpdateTrack();
        await root.ToSignal(root.GetTree().CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
        var routed = controller.GetChildren().OfType<AudioStreamPlayer>().ToArray();
        Check(routed.Count(p => p.Playing) == 1 && proxy.Get("stopped").AsInt32() > 0, "真实 NRunMusicController 补丁抑制原曲叠音");
        PresentationSettings.OriginalMusic = false;
        await root.ToSignal(root.GetTree().CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);
        Check(routed.All(p => !p.Playing) && proxy.Get("started").AsInt32() >= 2, "关闭一代音乐开关立即恢复原生曲目");
        PresentationSettings.OriginalMusic = true;
        await root.ToSignal(root.GetTree().CreateTimer(0.8), SceneTreeTimer.SignalName.Timeout);
        Check(routed.Count(p => p.Playing) == 1, "重新启用开关恢复一代音乐");
        controller.PlayCustomMusic("event:/test/boss");
        Check(proxy.Get("stopped").AsInt32() >= 3, "首领原生自定义音乐入口仍保持单路音乐");
        run.PushRoom(new EventRoom(MegaCrit.Sts2.Core.Models.ModelDb.Event<MegaCrit.Sts2.Core.Models.Events.TheArchitect>()));
        int nativeStarts = proxy.Get("started").AsInt32();
        controller.UpdateMusic();
        await root.ToSignal(root.GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
        Check(routed.All(p => !p.Playing) && proxy.Get("started").AsInt32() > nativeStarts,
            "进入建筑师阶段停止一代事件音乐，恢复二代原生音乐");
        run.PopCurrentRoom();
        controller.StopMusic();
        await root.ToSignal(root.GetTree().CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);
        Check(routed.All(p => !p.Playing), "StopMusic 后计时器不会意外重启 BGM");
        audioTestHarmony.Unpatch(nonInteractive, HarmonyPatchType.All, audioTestHarmony.Id);
        run.PopCurrentRoom();
        Check(LocString.GetIfExists("settings_ui", "FRIERENMOD-ORIGINAL_MUSIC.title") != null,
            "模组音乐配置具有本地化标题");
        var architectVfx = player.Character.GetArchitectAttackVfx();
        Check(architectVfx.Count > 0 && architectVfx.All(path =>
            GD.Load<PackedScene>("res://scenes/" + path + ".tscn")?.CanInstantiate() == true),
            "建筑师互动复用的二代魔法命中特效均可加载");

        if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
        {
            await Frames(5);
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/presentation.png"));
        }
        container.QueueFree();
        await Frames(2);
        return count;
    }
}
