using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Experiments
{
	public static class SpriteBatchExtensions
	{
		public static void DrawStringLayer(this SpriteBatch sb, SpriteFont spriteFont, string text, Vector2 position, Color foreColour, Color backColour)
		{
			sb.DrawString(spriteFont, text, position + Vector2.One, backColour);
			sb.DrawString(spriteFont, text, position, foreColour);
		}

		public static void DrawStringLayer(this SpriteBatch sb, SpriteFont spriteFont, string text, Vector2 position)
		{
			sb.DrawString(spriteFont, text, position + Vector2.One, Color.Black);
			sb.DrawString(spriteFont, text, position, Color.White);
		}
	}
}
