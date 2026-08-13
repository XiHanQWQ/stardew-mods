using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AutomaticTodoList.Components.UI;

internal class CenteredTextRow(string text, Vector2 position, int totalWidth, bool useWhiteText = false, bool drawShadow = true, bool drawUnderline = false)
{
    public void Draw(SpriteBatch b)
    {
        int textWidth = (int)Game1.smallFont.MeasureString(text).X;
        int xOffset = (totalWidth - textWidth) / 2;

        Vector2 centeredPosition = new(position.X + xOffset, position.Y);

        Color color = useWhiteText ? Color.White : Game1.textColor;

        if (drawShadow)
        {
            Utility.drawTextWithShadow(b, text, Game1.smallFont, centeredPosition, color);
        }
        else
        {
            b.DrawString(Game1.smallFont, text, centeredPosition, color);
        }

        if (drawUnderline)
        {
            Vector2 textSize = Game1.smallFont.MeasureString(text);
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
