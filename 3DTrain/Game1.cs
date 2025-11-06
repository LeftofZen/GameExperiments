using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shared;

namespace _3DTrain
{
	public class Game1 : ImGuiGame
	{
		Camera3D _camera;
		Texture2D _tileset;

		public Game1()
		{
			Graphics.GraphicsProfile = GraphicsProfile.HiDef;
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
		}

		protected override void Initialize()
		{
			// TODO: Add your initialization logic here
			_camera = new Camera3D(GraphicsDevice, new Vector3(256, 256, 256));
			base.Initialize();
		}

		protected override void LoadContent()
		{
			// TODO: use this.Content to load your game content here
			_tileset = Content.Load<Texture2D>("Textures//iso_tileset");
		}

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
			GraphicsDevice.Clear(Color.CornflowerBlue);

			SpriteBatch.Begin();
			SpriteBatch.Draw(_tileset, new Vector2(0, 0), Color.White);
			SpriteBatch.End();

			base.Draw(gameTime);
		}
	}
}
