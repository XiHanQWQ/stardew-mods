using AutomaticTodoList.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

internal class TodoItemTextRow(ITodoItem item, Vector2 position, bool useWhiteText = false, bool drawShadow = true, bool drawUnderline = false, float textOpacity = 1f, bool largeFont = false)
{
    private static readonly Lazy<Texture2D> lazyPixel = new(() =>
    {
        Texture2D pixel = new(Game1.graphics.GraphicsDevice, 1, 1);
        pixel.SetData([Color.White]);
        return pixel;
    });

    /// <summary>A blank pixel which can be colorized and stretched to draw geometric shapes.</summary>
    public static Texture2D Pixel => lazyPixel.Value;

    public void Draw(SpriteBatch b)
    {
        SpriteFont font = largeFont ? Game1.dialogueFont : Game1.smallFont;
        Color textColor = (useWhiteText ? Color.White : (item.IsChecked ? Color.DarkSlateGray : Color.Black)) * textOpacity;
        string text = item.Text();

        if (drawShadow)
        {
            Utility.drawTextWithShadow(b, text, font, position, textColor);
        }
        else
        {
            b.DrawString(font, text, position, textColor);
        }

        Vector2 textSize = font.MeasureString(text);

        if (item.IsChecked)
        {
            // strikethrough for completed items
            b.Draw(
                Pixel,
                new Rectangle(
                    (int)position.X,
                    (int)(position.Y + textSize.Y / 2),
                    (int)textSize.X,
                    1
                ),
                textColor
            );
        }
        else if (drawUnderline)
        {
            // underline for the text-underline style
            b.Draw(
                Pixel,
                new Rectangle(
                    (int)position.X,
                    (int)(position.Y + textSize.Y),
                    (int)textSize.X,
                    1
                ),
                textColor
            );
        }
    }
}
