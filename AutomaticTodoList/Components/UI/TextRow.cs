using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AutomaticTodoList.Components.UI;

internal class TextRow(string text, Vector2 position, bool useWhiteText = false, bool drawShadow = true, bool drawUnderline = false)
{
    public void Draw(SpriteBatch b)
    {
        Color color = useWhiteText ? Color.White : Game1.textColor;

        if (drawShadow)
        {
            Utility.drawTextWithShadow(b, text, Game1.smallFont, position, color);
        }
        else
        {
            b.DrawString(Game1.smallFont, text, position, color);
        }

        if (drawUnderline)
        {
            Vector2 textSize = Game1.smallFont.MeasureString(text);
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
