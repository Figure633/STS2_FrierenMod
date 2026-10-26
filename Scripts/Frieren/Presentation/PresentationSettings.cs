using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace FrierenMod.Scripts.Presentation;

public sealed class PresentationSettings : SimpleModConfig
{
    public static bool OriginalMusic { get; set; } = true;
    public static bool Achievements { get; set; } = true;
    public static bool AllowOtherActTwoBosses { get; set; } = false;

    public override void SetupConfigUI(Control optionContainer)
    {
        GenerateOptionsForAllProperties(optionContainer);
        optionContainer.AddChild(CreateButton("BackLibrary", "BackLibraryOpen", () => {
            if (NGame.Instance is { } root) UI.SlotBackLibrary.Open(root);
        }));
        AddRestoreDefaultsButton(optionContainer);
        SetupFocusNeighbors(optionContainer);
    }
}
