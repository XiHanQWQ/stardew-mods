using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace AutomaticTodoList;

public sealed class ModConfig
{
    /**
     * System settings, set by the mod internally.
     */

    public bool IsPanelVisible { get; set; } = true;

    /**
     * User settings, changable by the player in-game using the Generic Mod Config Menu.
     */

    public KeybindList ToggleTodoListKeybind { get; set; } = KeybindList.Parse($"{SButton.LeftShift} + {SButton.L}");

    public Vector2 PanelPosition { get; set; } = new(10, 80);

    public int VisibleItemCount { get; set; } = 10;

    public bool CheckBirthdays { get; set; } = true;

    public bool CheckFestivals { get; set; } = true;

    public bool CheckHarvestableCrops { get; set; } = true;

    public bool CheckWaterableCrops { get; set; } = true;

    // pets that still need to be petted
    public bool CheckUnpettedAnimals { get; set; } = true;

    public bool CheckReadyMachines { get; set; } = true;

    public bool CheckToolPickup { get; set; } = true;

    public bool CheckDailyQuestBulletinBoard { get; set; } = true;

    public bool CheckSpecialOrdersBoard { get; set; } = true;

    public bool CheckTravelingMerchant { get; set; } = true;

    public bool CheckQueenOfSauce { get; set; } = true;

    public bool CheckGiftingNPCs { get; set; } = false;

    public string GiftingNPCsString { get; set; } = "";

    /// <summary>The opacity of the panel background, 0&nbsp;=&nbsp;fully transparent, 1&nbsp;=&nbsp;fully opaque.</summary>
    public float PanelOpacity { get; set; } = 1f;

    /// <summary>The opacity of the panel text, 0&nbsp;=&nbsp;fully transparent, 1&nbsp;=&nbsp;fully opaque.</summary>
    public float TextOpacity { get; set; } = 1f;

    /// <summary>Whether to use the large dialogue font for the panel text instead of the small font.</summary>
    public bool LargeFont { get; set; } = false;

    /// <summary>The color of the panel text: "black" or "white".</summary>
    public string TextColor { get; set; } = "black";

    /// <summary>Whether to draw a shadow behind the panel text.</summary>
    public bool DrawTextShadow { get; set; } = true;

    /// <summary>Whether to draw an underline beneath each list item.</summary>
    public bool DrawTextUnderline { get; set; } = false;

    /// <summary>Whether to draw the panel's background box. When false, only the text is drawn.</summary>
    public bool ShowPanelBackground { get; set; } = true;

    /// <summary>Whether to draw a soft dark shadow behind the panel when the background box is disabled.</summary>
    public bool DrawShadowBackground { get; set; } = false;

    /// <summary>The strength of the shadow background, as a percentage from 0 (none) to 100 (very dark).</summary>
    public int ShadowBackgroundStrength { get; set; } = 20;

    /// <summary>Whether to draw a shadow behind the panel's background box.</summary>
    public bool DrawPanelShadow { get; set; } = true;
}
