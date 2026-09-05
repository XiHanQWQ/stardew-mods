using AutomaticTodoList.Components.UI;
using AutomaticTodoList.Engines;
using AutomaticTodoList.Models;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Events;
using System.Collections.Generic;

namespace AutomaticTodoList;

/// <summary>Manages the Automatic Todo List engines.</summary>
internal sealed class AutomaticTodoListManager
{

    /// <summary>The config for the mod.</summary>
    public ModConfig Config { get; set; }

    public Action SaveConfig { get; set; }

    private readonly Action<string, StardewModdingAPI.LogLevel> Log;

    private readonly HashSet<IEngine> engines = [];
    private AutomaticTodoListPanel automaticTodoListPanel = null!; // initialized in OnGameLaunched

    /// <summary>Preserved checked items from when the panel was last hidden.</summary>
    private List<ITodoItem> preservedCheckedItems = [];

    // Cached collections for GatherItems to avoid allocations on every frame
    private readonly List<ITodoItem> cachedAllItems = [];
    private readonly HashSet<string> cachedTextSet = new(StringComparer.Ordinal);
    private readonly List<ITodoItem> cachedMergedItems = [];

    /// <summary>Initializes a new instance of the <see cref="AutomaticTodoListManager"/> class.</summary>
    public AutomaticTodoListManager(ModConfig config, Action saveConfig, Action<string, StardewModdingAPI.LogLevel> log)
    {
        this.Config = config;
        this.SaveConfig = saveConfig;
        this.Log = log;

        this.InitEngines();
    }

    internal void OnGameLaunched(GameLaunchedEventArgs e)
    {
        this.automaticTodoListPanel = new(
            () => this.Config.VisibleItemCount,
            this.GatherItems,
            () => this.Config.PanelOpacity,
            () => this.Config.TextOpacity,
            () => this.Config.LargeFont,
            () => this.Config.ShowPanelBackground,
            () => this.Config.DrawShadowBackground,
            () => this.Config.ShadowBackgroundStrength,
            () => this.Config.DrawPanelShadow,
            () => this.Config.TextColor.Equals("white", StringComparison.OrdinalIgnoreCase),
            () => this.Config.DrawTextShadow,
            () => this.Config.DrawTextUnderline
        );
    }

    internal void OnDayStarted(DayStartedEventArgs e)
    {
        // clear preserved items from previous day
        this.preservedCheckedItems.Clear();

        // the engines only run while the panel is visible: hiding the panel stops all background scanning
        if (!this.Config.IsPanelVisible)
        {
            return;
        }

        foreach (IEngine engine in this.engines)
        {
            engine.OnDayStarted(e);
        }
    }

    internal void OnTimeChanged(TimeChangedEventArgs e)
    {
        // the engines only run while the panel is visible: hiding the panel stops all background scanning
        if (!this.Config.IsPanelVisible)
        {
            return;
        }

        foreach (IEngine engine in this.engines)
        {
            engine.OnTimeChanged(e);
        }
    }

    internal void OnOneSecondUpdateTicked(OneSecondUpdateTickedEventArgs e)
    {
        // the engines only run while the panel is visible: hiding the panel stops all background scanning
        if (!this.Config.IsPanelVisible)
        {
            return;
        }

        foreach (IEngine engine in this.engines)
        {
            engine.OnOneSecondUpdateTicked(e);
        }
    }

    internal void OnUpdateTicked(UpdateTickedEventArgs e)
    {
        if (this.Config.IsPanelVisible && this.automaticTodoListPanel is not null)
        {
            this.automaticTodoListPanel.UpdateScrollbarDrag(Mouse.GetState());
        }

        // the engines only run while the panel is visible: hiding the panel stops all background scanning
        if (!this.Config.IsPanelVisible)
        {
            return;
        }

        foreach (IEngine engine in this.engines)
        {
            engine.OnUpdateTicked(e);
        }
    }

    internal void OnMouseWheelScrolled(MouseWheelScrolledEventArgs e)
    {
        if (this.Config.IsPanelVisible && this.automaticTodoListPanel is not null)
        {
            this.automaticTodoListPanel.HandleMouseWheel(e.Delta);
        }
    }

    internal void OnRenderedHud(RenderedHudEventArgs e)
    {
        this.TryRenderPanel(e.SpriteBatch);
    }

    internal void OnButtonsChanged(ButtonsChangedEventArgs e)
    {
        if (this.Config.ToggleTodoListKeybind.JustPressed())
        {
            this.Config.IsPanelVisible = !this.Config.IsPanelVisible;
            SaveConfig();

            if (this.Config.IsPanelVisible)
            {
                // panel shown again: rebuild the todo list from scratch so it is populated right away
                foreach (IEngine engine in this.engines)
                {
                    engine.Reset();
                    if (engine.IsEnabled())
                    {
                        engine.UpdateItems();
                    }
                }
            }
            else
            {
                // panel hidden: save checked items if preserve option is enabled, then stop scanning
                if (this.Config.PreserveCompletedItems)
                {
                    // collect checked items from the merged list (GatherItems includes preserved items)
                    this.preservedCheckedItems = this.GatherItems()
                        .Where(item => item.IsChecked)
                        .ToList();
                }

                foreach (IEngine engine in this.engines)
                {
                    engine.Reset();
                }
            }
        }
    }

    internal void OnMenuChanged(MenuChangedEventArgs e)
    {
        // the engines only run while the panel is visible: hiding the panel stops all background scanning
        if (!this.Config.IsPanelVisible)
        {
            return;
        }

        foreach (IEngine engine in this.engines)
        {
            engine.OnMenuChanged(e);
        }
    }

    private void TryRenderPanel(SpriteBatch b)
    {
        if (this.Config.IsPanelVisible && this.automaticTodoListPanel is not null)
        {
            this.automaticTodoListPanel.Draw(b, this.Config.PanelPosition);
        }
    }

    private ICollection<ITodoItem> GatherItems()
    {
        // Clear and reuse cached collections to avoid allocations
        cachedAllItems.Clear();
        foreach (IEngine engine in this.engines)
        {
            if (engine.IsEnabled())
            {
                cachedAllItems.AddRange(engine.Items());
            }
        }

        // merge preserved checked items: replace matching unchecked items, or add new ones
        if (this.Config.PreserveCompletedItems && this.preservedCheckedItems.Count > 0)
        {
            cachedTextSet.Clear();
            foreach (ITodoItem item in cachedAllItems)
            {
                cachedTextSet.Add(item.Text());
            }

            cachedMergedItems.Clear();
            cachedMergedItems.Capacity = cachedAllItems.Count + this.preservedCheckedItems.Count;

            foreach (ITodoItem item in cachedAllItems)
            {
                ITodoItem? preserved = null;
                foreach (ITodoItem p in this.preservedCheckedItems)
                {
                    if (p.Text() == item.Text())
                    {
                        preserved = p;
                        break;
                    }
                }
                cachedMergedItems.Add(preserved ?? item);
            }

            foreach (ITodoItem preserved in this.preservedCheckedItems)
            {
                if (!cachedTextSet.Contains(preserved.Text()))
                {
                    cachedMergedItems.Add(preserved);
                }
            }

            cachedAllItems.Clear();
            cachedAllItems.AddRange(cachedMergedItems);
        }

        cachedAllItems.Sort((a, b) =>
        {
            int checkedComp = a.IsChecked.CompareTo(b.IsChecked);
            if (checkedComp != 0)
            {
                return checkedComp;
            }

            int priorityComp = a.Priority.CompareTo(b.Priority);
            if (priorityComp != 0)
            {
                return priorityComp;
            }

            return a.Text().CompareTo(b.Text());
        });

        return cachedAllItems;
    }

    public void InitEngines(bool forceReset = false)
    {
        if (this.engines.Count > 0 && !forceReset)
        {
            // we already initialized the engines
            return;
        }

        this.engines.Clear();

        this.engines.Add(new ActiveFestivalEngine(Log, () => this.Config.CheckFestivals));
        this.engines.Add(new BirthdayEngine(Log, () => this.Config.CheckBirthdays));
        this.engines.Add(new BulletinBoardEngine(Log, () => this.Config.CheckDailyQuestBulletinBoard));
        this.engines.Add(new GiftingEngine(Log, () => this.Config.CheckGiftingNPCs, () => this.Config.GiftingNPCsString));
        this.engines.Add(new HarvestableCropsEngine(Log, () => this.Config.CheckHarvestableCrops));
        this.engines.Add(new PassiveFestivalEngine(Log, () => this.Config.CheckFestivals));
        this.engines.Add(new QueenOfSauceEngine(Log, () => this.Config.CheckQueenOfSauce));
        this.engines.Add(new AnimalsEngine(Log, () => this.Config.CheckUnpettedAnimals));
        this.engines.Add(new PetEngine(Log, () => this.Config.CheckUnpettedAnimals));
        this.engines.Add(new ReadyMachinesEngine(Log, () => this.Config.CheckReadyMachines));
        this.engines.Add(new HarvestableCrabPotsEngine(Log, () => this.Config.CheckHarvestableCrabPots));
        this.engines.Add(new SpecialOrdersBoardEngine(Log, () => this.Config.CheckSpecialOrdersBoard));
        this.engines.Add(new ToolPickupEngine(Log, () => this.Config.CheckToolPickup));
        this.engines.Add(new TravelingMerchantEngine(Log, () => this.Config.CheckTravelingMerchant));
        this.engines.Add(new WaterableCropsEngine(Log, () => this.Config.CheckWaterableCrops));
    }
}
