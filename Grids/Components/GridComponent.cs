using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace Experiments.Components
{

	public class GridComponent : DrawableGameComponent
	{
		private readonly SpriteBatch _spriteBatch;
		private readonly int _gridSize;
		private readonly GraphicsDevice _graphicsDevice;

		public GridComponent(Game game, SpriteBatch spriteBatch, int gridSize)
			: base(game)
		{
			_spriteBatch = spriteBatch;
			_gridSize = gridSize;
			_graphicsDevice = game.GraphicsDevice;
		}

		public override void Draw(GameTime gameTime)
		{
			var w = _graphicsDevice.PresentationParameters.BackBufferWidth;
			var h = _graphicsDevice.PresentationParameters.BackBufferHeight;

			_spriteBatch.Begin();

			for (var x = 0; x < w; x += _gridSize)
			{
				for (var y = 0; y < h; y += _gridSize)
				{
					_spriteBatch.DrawRectangle(new Rectangle(x, y, _gridSize, _gridSize), Color.Black, 0.5f);
				}
			}

			_spriteBatch.End();

			base.Draw(gameTime);
		}
	}
}