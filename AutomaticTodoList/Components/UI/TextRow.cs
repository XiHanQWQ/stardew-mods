using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AutomaticTodoList.Components.UI;

internal class TextRow(string text, Vector2 position, bool useWhiteText = false, bool drawShadow = true, bool drawUnderline = false, float textOpacity = 1f, bool largeFont = false)
{
    public void Draw(SpriteBatch b)
    {
        SpriteFont font = largeFont ? Game1.dialogueFont : Game1.smallFont;
        Color color = (useWhiteText ? Color.White : Game1.textColor) * textOpacity;

        if (drawShadow)
        {
            Utility.drawTextWithShadow(b, text, font, position, color);
        }
        else
        {
            b.DrawString(font, text, position, color);
        }

        if (drawUnderline)
        {
            Vector2 textSize = font.MeasureString(text);
            Utility.drawLineWithScreenCoordinates(
                (int)position.X, (int)(position.Y + textSize.Y),
                (int)(position.X + textSize.X), (int)(position.Y + textSize.Y),
                b,
                color,
                thickness: 1
            );
        }
    }
}
