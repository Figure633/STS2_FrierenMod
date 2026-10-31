using System.Text.Json;
using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Powers;

public sealed class FrierenTheSlayerPower : FrierenPower
{
    public int UpgradedChoices { get; set; }
    public override LocString Description { get { var text = base.Description; text.Add("Amount", Amount); text.Add("UpgradedChoices", UpgradedChoices); return text; } }
    private static HashSet<string>? _spellIds;

    // 固定读取中文名，英文客户端与联机客户端不会因显示语言不同得到不同选牌池。
    public static IEnumerable<CardModel> SpellPool()
    {
        if (_spellIds == null) {
            var names = new Dictionary<string,string>();
            foreach (var path in new[] { "res://localization/zhs/cards.json" }
                .Concat(ModManager.GetModdedLocTables("zhs", "cards.json"))) {
                using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
                if (file == null) continue;
                foreach (var pair in JsonSerializer.Deserialize<Dictionary<string,string>>(file.GetAsText())!) names[pair.Key] = pair.Value;
            }
            _spellIds = names.Where(p => p.Key.EndsWith(".title") && p.Value.Contains("魔法"))
                .Select(p => p.Key[..^6]).ToHashSet(StringComparer.Ordinal);
        }
        return ModelDb.AllCards.Where(c => _spellIds.Contains(c.Id.Entry) && !Mechanics.FrierenCardAvailability.IsRetired(c)
                && c is not FactorCard and not MagicPropCard and not CustomLegendarySpell and not Imagination and not AuraDispel
                    and not LegendaryCraftChoice and not MageStyleFinisher and not SecondDefensiveMagic)
            .OrderBy(c => c.Id.ToString(), StringComparer.Ordinal);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player || player.Creature.CombatState is not {} combat) return;
        for (int i = 0; i < Amount; i++) {
            var choices = SpellPool().Select(model => {
                var card = combat.CreateCard(model, player);
                if (i < UpgradedChoices && card.IsUpgradable) CardCmd.Upgrade(card);
                return card;
            }).ToArray();
            if (choices.Length == 0) continue;
            var selected = (await CardSelectCmd.FromSimpleGrid(context, choices, player,
                new CardSelectorPrefs(new LocString("frieren", "CARD_SELECT"), 1))).SingleOrDefault();
            if (selected != null) await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, player);
            foreach (var card in choices.Where(c => c != selected)) card.RemoveFromState();
        }
    }
}
