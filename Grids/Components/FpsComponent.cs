using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharedContent;

namespace Experiments.Components
{
	public class FpsComponent : DrawableGameComponent
	{
		private readonly SpriteBatch _spriteBatch;
		private SpriteFont _font;
		private double _elapsed;
		private int _frames;
		private int _fps;

		public FpsComponent(Game game, SpriteBatch spriteBatch)
			: base(game)
		{
			_spriteBatch = spriteBatch;
		}

		protected override void LoadContent()
		{
			_font = Game.Content.Load<SpriteFont>($"Fonts\\{FontNames._Pixeltype}");
			base.LoadContent();
		}

		public override void Update(GameTime gameTime)
		{
			_elapsed += gameTime.ElapsedGameTime.TotalSeconds;
			_frames++;

			if (_elapsed >= 1.0)
			{
				_fps = _frames;
				_frames = 0;
				_elapsed = 0;
			}

			base.Update(gameTime);
		}

		public override void Draw(GameTime gameTime)
		{
			_spriteBatch.Begin();

			_spriteBatch.DrawStringLayer(_font, $"FPS: {_fps}", new Vector2(10, 10), Color.Yellow, Color.Black);

			_spriteBatch.End();

			base.Draw(gameTime);
		}
	}
}