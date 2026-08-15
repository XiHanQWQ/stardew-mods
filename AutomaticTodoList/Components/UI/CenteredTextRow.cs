using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AutomaticTodoList.Components.UI;

internal class CenteredTextRow(string text, Vector2 position, int totalWidth, bool useWhiteText = false, bool drawShadow = true, bool drawUnderline = false, float textOpacity = 1f, bool largeFont = false)
{
    public void Draw(SpriteBatch b)
    {
        SpriteFont font = largeFont ? Game1.dialogueFont : Game1.smallFont;
        int textWidth = (int)font.MeasureString(text).X;
        int xOffset = (totalWidth - textWidth) / 2;

        Vector2 centeredPosition = new(position.X + xOffset, position.Y);

        Color color = (useWhiteText ? Color.White : Game1.textColor) * textOpacity;

        if (drawShadow)
        {
            Utility.drawTextWithShadow(b, text, font, centeredPosition, color);
        }
        else
        {
            b.DrawString(font, text, centeredPosition, color);
        }

        if (drawUnderline)
        {
            Vector2 textSize = font.MeasureString(text);
            Utility.drawLineWithScreenCoordinates(
                (int)centeredPosition.X, (int)(centeredPosition.Y + textSize.Y),
                (int)(centeredPosition.X + textSize.X), (int)(centeredPosition.Y + textSize.Y),
                b,
                color,
                thickness: 1
            );
        }
    }
}
