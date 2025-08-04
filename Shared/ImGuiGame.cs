using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shared.Components;
using Shared.ImGuiNet;

namespace Shared
{
	public class ImGuiGame : Game
	{
		public GraphicsDeviceManager Graphics { get; private set; }
		public SpriteBatch SpriteBatch { get; private set; }

		public ImGuiRenderer GuiRenderer { get; private set; }

		private FpsComponent fpsComponent;

		public ImGuiGame()
		{
			Content.RootDirectory = "Content";
			IsMouseVisible = true;

			Graphics = new GraphicsDeviceManager(this);

			// Set window size before the game starts
			Graphics.PreferredBackBufferWidth = 1280;
			Graphics.PreferredBackBufferHeight = 720;
			Graphics.ApplyChanges();

			Window.AllowAltF4 = true;
			Window.Title = "Base Game";
			Window.AllowUserResizing = true;

			SpriteBatch = new SpriteBatch(GraphicsDevice);
		}

		protected override void Initialize()
		{

			GuiRenderer = new ImGuiRenderer(this);
			GuiRenderer.RebuildFontAtlas();
			fpsComponent = new FpsComponent(this, SpriteBatch);
			Components.Add(fpsComponent);

			base.Initialize();
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			{
				Exit();
			}

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GuiRenderer.BeginLayout(gameTime);
			DrawImGui(gameTime);
			GuiRenderer.EndLayout();

			base.Draw(gameTime);
		}

		protected virtual void DrawImGui(GameTime gameTime)
		{ }
	}
}
