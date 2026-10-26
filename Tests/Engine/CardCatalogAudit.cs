using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using System.Text.Json;

// 从实际模型导出费用、类型和原生格式化正文，用于核对作者工作簿，不改写工作簿。
public static class CardCatalogAudit
{
    public static int Run()
    {
        LocManager.Instance.SetLanguage("zhs");
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var rows = new List<object>();
        foreach (var type in typeof(Mana).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(CardModel).IsAssignableFrom(t)).OrderBy(t => t.Name))
        foreach (bool upgraded in new[] { false, true }) {
            var card = (CardModel)ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();
            card.Owner = player;
            if (upgraded) {
                if (card.MaxUpgradeLevel == 0) continue;
                card.UpgradeInternal(); card.FinalizeUpgradeInternal();
            }
            rows.Add(new { model = type.Name, id = card.Id.Entry, name = card.Title, upgraded,
                cost = card.EnergyCost.CostsX ? "X" : card.EnergyCost.GetWithModifiers(CostModifiers.Local).ToString(),
                type = card.Type.ToString(), rarity = card.Rarity.ToString(), text = card.GetDescriptionForPile(PileType.None) });
        }
        File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/card-catalog-current.json"),
            JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
        return rows.Count;
    }
}
