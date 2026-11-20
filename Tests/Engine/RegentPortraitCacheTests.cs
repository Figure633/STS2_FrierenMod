using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Cards;

public static class RegentPortraitCacheTests
{
    public static async Task<int> RunDetached(Node root)
    {
        // 正式版程序集配本机beta资源包时不实例化跨版本场景；直接检验原生纹理生命周期。
        var card = new NCard();
        var portrait = new TextureRect();
        var model = MegaCrit.Sts2.Core.Models.ModelDb.Card<FrierenMod.Scripts.Cards.Flow>().ToMutable();
        AccessTools.Property(typeof(NCard), nameof(NCard.Model)).SetValue(card, model);
        AccessTools.Field(typeof(NCard), "_portrait").SetValue(card, portrait);
        portrait.Texture = model.Portrait;
        try { return await Run(root, card); }
        finally { portrait.Free(); card.Free(); }
    }

    public static async Task<int> Run(Node root, NCard card)
    {
        int checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; GD.Print("PASS 储君卡图缓存：" + text); }
        var dll = System.Environment.GetEnvironmentVariable("FRIEREN_REGENT_ANIME_DLL")
            ?? throw new Exception("请设置 FRIEREN_REGENT_ANIME_DLL 为待核查的 RegentCardsAnimeRework.dll。");
        // 游戏自带 GodotSharp 的程序集版本与官方测试宿主不同；只在此隔离
        // 加载上下文复用宿主已载入的依赖，不改第三方 DLL 的引用或玩家文件。
        var context = new AssemblyLoadContext("Frieren.RegentPortraitRepro");
        context.Resolving += (_, name) => AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == name.Name);
        var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(dll));
        var type = assembly.GetType("regentCardsAnimeRework.PortraitPatch", throwOnError: true)!;
        var refresh = AccessTools.Method(type, "TryReplacePortrait");
        var normal = (IDictionary)AccessTools.Field(type, "DefaultPortraitStates").GetValue(null)!;
        var ancient = (IDictionary)AccessTools.Field(type, "DefaultAncientPortraitStates").GetValue(null)!;
        var portrait = (TextureRect)AccessTools.Field(typeof(NCard), "_portrait").GetValue(card)!;
        var fresh = portrait.Texture ?? card.Model!.Portrait;
        portrait.Texture = fresh;
        string path = fresh.ResourcePath;
        Check(!string.IsNullOrEmpty(path), "使用真实卡牌纹理 " + path);
        for (int trial = 1; trial <= 3; trial++) {
            normal.Clear(); ancient.Clear();
            var cache = new AssetCache();
            fresh = cache.GetTexture2D(path);
            portrait.Texture = fresh;
            refresh.Invoke(null, [card]);
            Check(normal.Count > 0 && GodotObject.IsInstanceValid(fresh), "首次显示正常并缓存原图 " + trial);
            // 独立原生 AssetCache 只管理这一张图，避免卸载测试宿主的其他资源。
            // 与切换场景时使用相同的 Dispose/CallDeferred 路径。
            cache.UnloadMissedCacheAssets();
            for (int frame = 0; frame < 3; frame++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(!GodotObject.IsInstanceValid(fresh), "原生资源清理释放缓存纹理 " + trial);
            bool failed = false;
            try { refresh.Invoke(null, [card]); }
            catch (TargetInvocationException e) when (e.InnerException is ObjectDisposedException) {
                failed = e.InnerException.StackTrace?.Contains("PortraitPatch.RestoreDefaultState") == true;
            }
            Check(failed, "再次刷新稳定触发日志中的同一失效纹理异常 " + trial);
            // 原生已重新加载有效图，第三方缓存仍会将失效旧图覆盖回来。
            fresh = cache.GetTexture2D(path); portrait.Texture = fresh;
            failed = false;
            try { refresh.Invoke(null, [card]); }
            catch (TargetInvocationException e) when (e.InnerException is ObjectDisposedException) { failed = true; }
            Check(failed, "仅重新加载卡图不能消除第三方旧缓存 " + trial);
            normal.Clear(); ancient.Clear(); portrait.Texture = fresh;
            refresh.Invoke(null, [card]);
            Check(GodotObject.IsInstanceValid(portrait.Texture), "清理第三方旧缓存后恢复 " + trial);
        }
        normal.Clear(); ancient.Clear();
        FrierenMod.Scripts.Presentation.RegentPortraitCompatibility.Install(assembly);
        for (int trial = 1; trial <= 3; trial++) {
            var cache = new AssetCache();
            fresh = cache.GetTexture2D(path); portrait.Texture = fresh;
            refresh.Invoke(null, [card]);
            cache.UnloadMissedCacheAssets();
            for (int frame = 0; frame < 3; frame++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(!GodotObject.IsInstanceValid(fresh), "兼容检查确实释放旧纹理 " + trial);
            fresh = cache.GetTexture2D(path); portrait.Texture = fresh;
            refresh.Invoke(null, [card]);
            Check(GodotObject.IsInstanceValid(portrait.Texture), "兼容处理后切场景刷新不再回填失效纹理 " + trial);
        }
        normal.Clear(); ancient.Clear();
        return checks;
    }
}
