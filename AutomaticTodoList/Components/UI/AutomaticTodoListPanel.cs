using Microsoft.Xna.Framework;
using AutomaticTodoList.Models;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AutomaticTodoList.Components.UI;

/// <summary>Manages the Automatic Todo List Panel UI.</summary>
/// <remarks>Initializes a new instance of the <see cref="AutomaticTodoListPanel"/> class.</remarks>
internal class AutomaticTodoListPanel(
    Func<int> visibleItemCount,
    Func<ICollection<ITodoItem>> getItems,
    Func<float> getOpacity,
    Func<bool> getShowBackground,
    Func<bool> getDrawPanelShadow,
    Func<bool> getUseWhiteText,
    Func<bool> getDrawShadow,
    Func<bool> getDrawUnderline
)
{
    /// <summary>The panel title, resolved each draw so the current game language is always used.</summary>
    private string TitleText => I18n.Panel_Title();

    private const int GutterLength = 4 * Game1.pixelZoom;

    private const int LineSpacing = 2;

    private const int ScrollbarWidth = 24;

    /// <summary>The height of the scrollbar thumb (12px sprite at 4x scale), matching GenericModConfigMenu.</summary>
    private const int ScrollbarThumbHeight = 48;

    private static readonly SpriteFont Font = Game1.smallFont;

    /// <summary>How many todo items are currently scrolled past (above the viewport).</summary>
    private int scrollIndex = 0;

    /// <summary>Whether the player is currently dragging the scrollbar thumb.</summary>
    private bool isDraggingScrollbar = false;

    private Rectangle scrollbarTrackRect;

    /// <summary>The on-screen bounds of the panel, used to check whether the cursor is over it.</summary>
    private Rectangle panelBounds;

    /// <summary>Scroll the list up or down by one item when the mouse wheel is used over the panel.</summary>
    public void HandleMouseWheel(int delta)
    {
        if (!panelBounds.Contains(Mouse.GetState().Position))
        {
            return;
        }

        int maxScroll = GetMaxScroll();
        if (maxScroll <= 0)
        {
            return;
        }

        int newScrollIndex = Math.Clamp(scrollIndex + (delta > 0 ? -1 : 1), 0, maxScroll);
        if (newScrollIndex != scrollIndex)
        {
            scrollIndex = newScrollIndex;
            Game1.playSound("shwip");
        }
    }

    /// <summary>Update the scrollbar drag state from the current mouse state.</summary>
    public void UpdateScrollbarDrag(MouseState mouse)
    {
        if (mouse.LeftButton == ButtonState.Pressed)
        {
            if (this.isDraggingScrollbar)
            {
                int maxScroll = GetMaxScroll();
                int trackHeight = this.scrollbarTrackRect.Height - 40;
                if (maxScroll > 0 && trackHeight > 0)
                {
                    int progress = mouse.Position.Y - this.scrollbarTrackRect.Y - 20;
                    ScrollToIndex((int)Math.Round(progress / (float)trackHeight * maxScroll));
                }
            }
            else if (this.scrollbarTrackRect.Contains(mouse.Position))
            {
                this.isDraggingScrollbar = true;
            }
        }
        else
        {
            this.isDraggingScrollbar = false;
        }
    }

    private void ScrollToIndex(int index)
    {
        int maxScroll = GetMaxScroll();
        int newScrollIndex = Math.Clamp(index, 0, maxScroll);
        if (newScrollIndex != scrollIndex)
        {
            scrollIndex = newScrollIndex;
            Game1.playSound("shiny4");
        }
    }

    public void Draw(SpriteBatch b, Vector2 position)
    {
        var allItems = getItems();
        int capacity = visibleItemCount();

        int maxScroll = Math.Max(0, allItems.Count - capacity);
        if (scrollIndex > maxScroll)
        {
            scrollIndex = maxScroll;
        }

        var renderedItems = allItems.Skip(scrollIndex).Take(capacity).ToList();
        bool showScrollbar = maxScroll > 0;
        bool showOverflowIndicator = allItems.Count > scrollIndex + capacity;

        // find the longest text, which determines the width of the panel
        int maxTextWidth = (int)Font.MeasureString(TitleText).X;
        foreach (ITodoItem item in renderedItems)
        {
            int todoItemWidth = (int)Font.MeasureString(item.Text()).X;
            if (todoItemWidth > maxTextWidth)
            {
                maxTextWidth = todoItemWidth;
            }
        }

        int numRows =
            1 + // the title row
            renderedItems.Count + // the todo items
            (showOverflowIndicator ? 1 : 0); // the overflow indicator

        // leave room for the scrollbar on the right when it is shown
        int contentWidth = maxTextWidth + (showScrollbar ? ScrollbarWidth + GutterLength : 0);

        // remember the on-screen bounds of the panel (for mouse wheel scrolling)
        int lineHeight = (int)Font.MeasureString(TitleText).Y;
        int contentHeight = 4 + numRows * (lineHeight + LineSpacing);
        panelBounds = new Rectangle(
            (int)position.X,
            (int)position.Y,
            contentWidth + GutterLength * 2,
            contentHeight + GutterLength * 2
        );

        // draw the surrounding box (with configured opacity)
        DrawTextureBox(b, position, contentWidth, numRows, out Vector2 titlePosition);

        // draw the title text and dividing line
        DrawTitleTextAndDividingLine(b, titlePosition, maxTextWidth, out Vector2 todoItemPosition);

        // draw the todo items
        DrawTodoItems(b, todoItemPosition, renderedItems, out Vector2 overflowIndicatorPosition);

        // draw the overflow indicator
        if (showOverflowIndicator)
        {
            DrawOverflowIndicator(b, overflowIndicatorPosition, allItems.Count - scrollIndex - capacity, getUseWhiteText(), getDrawShadow(), getDrawUnderline());
        }

        // draw the scrollbar
        if (showScrollbar)
        {
            DrawScrollbar(b, titlePosition, contentWidth, numRows, capacity, allItems.Count);
        }
    }

    private int GetMaxScroll()
    {
        return Math.Max(0, getItems().Count - visibleItemCount());
    }

    private void DrawTextureBox(SpriteBatch b, Vector2 position, int maxTextWidth, int numRows, out Vector2 nextContentPosition)
    {
        // if the background box is disabled, draw only the text without any padding
        if (!getShowBackground())
        {
            nextContentPosition = position;
            return;
        }

        // assume the size of each text line
        int lineHeight = (int)Font.MeasureString(TitleText).Y;

        // find the dimensions of the content inside the panel
        Vector2 contentDimensions = new(
            maxTextWidth,
            4 + // the dividing line between the title and the items
            numRows * (lineHeight + LineSpacing) // each todo item + title text
        );

        // add the border dimensions
        Vector2 dimensions = contentDimensions + new Vector2(GutterLength * 2, GutterLength * 2);

        // compute the background color, applying opacity from config
        float opacity = MathHelper.Clamp(getOpacity(), 0f, 1f);
        Color backgroundColor = Color.White * opacity;

        // draw the texture box
        IClickableMenu.drawTextureBox(
            b,
            Game1.menuTexture,
            new Rectangle(0, 256, 60, 60), // not sure what these numbers end up meaning, if anything
            (int)position.X,
            (int)position.Y,
            (int)dimensions.X,
            (int)dimensions.Y,
            backgroundColor,
            drawShadow: getDrawPanelShadow()
        );
        nextContentPosition = new Vector2(position.X + GutterLength, position.Y + GutterLength);
    }
    private void DrawTitleTextAndDividingLine(SpriteBatch b, Vector2 position, int totalWidth, out Vector2 nextContentPosition)
    {
        // the title never gets an underline, even when the text-underline style is enabled
        CenteredTextRow titleRow = new(TitleText, position, totalWidth, getUseWhiteText(), getDrawShadow(), drawUnderline: false);
        titleRow.Draw(b);

        var dividerPosition = new Vector2(position.X, position.Y + (int)Font.MeasureString(TitleText).Y + LineSpacing);

        Utility.drawLineWithScreenCoordinates(
            (int)dividerPosition.X, (int)dividerPosition.Y,
            (int)dividerPosition.X + totalWidth, (int)dividerPosition.Y,
            b,
            getUseWhiteText() ? Color.White : Game1.textColor,
            thickness: 1
        );

        nextContentPosition = new Vector2(dividerPosition.X, (int)dividerPosition.Y + 4 + LineSpacing);
    }

    private void DrawTodoItems(SpriteBatch b, Vector2 position, ICollection<ITodoItem> items, out Vector2 nextContentPosition)
    {
        Vector2 currentPosition = position;

        foreach (ITodoItem item in items.Take(visibleItemCount()))
        {
            TodoItemTextRow itemRow = new(item, currentPosition, getUseWhiteText(), getDrawShadow(), getDrawUnderline());
            itemRow.Draw(b);
            currentPosition.Y += (int)Font.MeasureString(item.Text()).Y + LineSpacing;
        }

        nextContentPosition = currentPosition;
    }

    private static void DrawOverflowIndicator(SpriteBatch b, Vector2 position, int numRemaining, bool useWhiteText, bool drawShadow, bool drawUnderline)
    {
        TextRow overflowIndicatorRow = new(I18n.Panel_OverflowIndicator(numRemaining), position, useWhiteText, drawShadow, drawUnderline);
        overflowIndicatorRow.Draw(b);
    }

    private void DrawScrollbar(SpriteBatch b, Vector2 titlePosition, int contentWidth, int numRows, int capacity, int totalItems)
    {
        int lineHeight = (int)Font.MeasureString(TitleText).Y;
        int contentHeight = 4 + numRows * (lineHeight + LineSpacing);

        // the scrollable area spans from below the dividing line to the bottom of the content
        int trackTop = (int)titlePosition.Y + lineHeight + LineSpacing + 4 + LineSpacing;
        int trackBottom = (int)titlePosition.Y + contentHeight - LineSpacing;
        if (trackBottom - trackTop <= ScrollbarThumbHeight)
        {
            return;
        }

        int trackX = (int)titlePosition.X + (contentWidth - ScrollbarWidth);
        scrollbarTrackRect = new Rectangle(trackX, trackTop, ScrollbarWidth, trackBottom - trackTop);

        int maxScroll = totalItems - capacity;
        float progress = maxScroll > 0 ? scrollIndex / (float)maxScroll : 0;
        int thumbY = trackTop + (int)((trackBottom - trackTop - ScrollbarThumbHeight) * progress);

        float opacity = MathHelper.Clamp(getOpacity(), 0f, 1f);

        // when the background box is hidden, use plain rectangles instead of the menu sprites
        if (!getShowBackground())
        {
            b.Draw(TodoItemTextRow.Pixel, scrollbarTrackRect, Color.Black * (0.4f * opacity));
            b.Draw(
                TodoItemTextRow.Pixel,
                new Rectangle(trackX, thumbY, ScrollbarWidth, ScrollbarThumbHeight),
                Color.White * (0.6f * opacity)
            );
            return;
        }

        // draw the track and thumb, using the same sprites as GenericModConfigMenu
        IClickableMenu.drawTextureBox(
            b,
            Game1.mouseCursors,
            new Rectangle(403, 383, 6, 6),
            trackX,
            trackTop,
            ScrollbarWidth,
            trackBottom - trackTop,
            Color.White * opacity,
            4f,
            drawShadow: false
        );
        b.Draw(
            Game1.mouseCursors,
            new Vector2(trackX, thumbY),
            new Rectangle(435, 463, 6, 12),
            Color.White * opacity,
            0f,
            Vector2.Zero,
            4f,
            SpriteEffects.None,
            0.77f
        );
    }
}
