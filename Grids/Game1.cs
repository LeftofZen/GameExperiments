using Experiments.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shared.Components;

namespace Experiments
{
	public class Game1 : Game
	{
		private GraphicsDeviceManager graphics;
		private SpriteBatch sb;
		private GridComponent gridComponent;
		private FpsComponent fpsComponent;
		private QuadTreeComponent quadTreeComponent;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";

			IsMouseVisible = true;

			// Set window size before the game starts
			graphics.PreferredBackBufferWidth = 1280;
			graphics.PreferredBackBufferHeight = 720;
			graphics.ApplyChanges();

			Window.AllowAltF4 = true;
			Window.Title = "Grid Experiments";
			Window.AllowUserResizing = true;
			//Window.IsBorderless = true;
		}

		protected override void Initialize()
		{
			sb = new SpriteBatch(GraphicsDevice);

			// TODO: Add your initialization logic here
			gridComponent = new GridComponent(this, sb, 32);
			//Components.Add(gridComponent);

			quadTreeComponent = new QuadTreeComponent(this, sb, new Rectangle(0, 0, 512, 512));
			Components.Add(quadTreeComponent);

			fpsComponent = new FpsComponent(this, sb);
			Components.Add(fpsComponent);

			base.Initialize();
		}

		protected override void LoadContent()
		{ }

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			{
				Exit();
			}

			// TODO: Add your update logic here

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.SteelBlue);
			base.Draw(gameTime);
		}
	}
}
