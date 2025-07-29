using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharedContent;
using System.Collections.Generic;
using System.Linq;

namespace Fonts
{
	public class ColourPalette
	{
		public Color Highlight { get; set; }
		public Color Light { get; set; }
		public Color Mid { get; set; }
		public Color Dark { get; set; }
		public Color Shadow { get; set; }
	}

	public class Game1 : Game
	{
		SpriteBatch _spriteBatch;
		readonly GraphicsDeviceManager _graphics;
		readonly Dictionary<string, SpriteFont> _fonts = [];
		readonly RasterizerState _rasterizerState = new() { ScissorTestEnable = true };

		bool DrawAllFonts { get; set; }
		readonly IEnumerable<string> DrawFonts =
		[
			FontNames._fs_pixel_sans_unicode_regular,
			//FontNames._MedodicaRegular,
			//FontNames._PixelOperator,
			FontNames._Pixeltype,
			FontNames._Reactor7
		];

		readonly ColourPalette Palette = new()
		{
			Highlight = new Color(247, 222, 0, 255),
			Light = new Color(252, 180, 38, 255),
			Mid = new Color(228, 230, 230, 255),
			Dark = new Color(90, 90, 90, 255),
			Shadow = new Color(57, 57, 57, 255),
		};

		float Scale = 1f;
		int ColumnWidth = 1024;
		Vector2 CameraPosition = Vector2.Zero;
		const int CameraSpeed = 10;

		public Game1()
		{
			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = 1920,
				PreferredBackBufferHeight = 1080
			};

			Window.AllowUserResizing = true;
			Window.Title = "Font Demonstration";

			Content.RootDirectory = "Content";
			IsMouseVisible = true;
		}

		protected override void Initialize()
		{
			// TODO: Add your initialization logic here

			base.Initialize();
		}

		protected override void LoadContent()
		{
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			foreach (var fontName in typeof(FontNames).GetFields())
			{
				if (fontName.GetValue(null) is string fontValue)
				{
					_fonts.Add(fontValue, Content.Load<SpriteFont>($"Fonts\\{fontValue}"));
				}
			}

			// TODO: use this.Content to load your game content here
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			{
				Exit();
			}

			// scale
			if (Keyboard.GetState().IsKeyDown(Keys.OemPlus) || Keyboard.GetState().IsKeyDown(Keys.Add))
			{
				Scale *= 1.005f;
			}
			else if (Keyboard.GetState().IsKeyDown(Keys.OemMinus) || Keyboard.GetState().IsKeyDown(Keys.Subtract))
			{
				Scale /= 1.005f;
			}

			// column width
			if (Keyboard.GetState().IsKeyDown(Keys.OemComma))
			{
				ColumnWidth -= 5;
			}
			else if (Keyboard.GetState().IsKeyDown(Keys.OemPeriod))
			{
				ColumnWidth += 5;
			}

			// camera
			if (Keyboard.GetState().IsKeyDown(Keys.Left))
			{
				CameraPosition.X += CameraSpeed;
			}
			else if (Keyboard.GetState().IsKeyDown(Keys.Right))
			{
				CameraPosition.X -= CameraSpeed;
			}

			if (Keyboard.GetState().IsKeyDown(Keys.Up))
			{
				CameraPosition.Y += CameraSpeed;
			}
			else if (Keyboard.GetState().IsKeyDown(Keys.Down))
			{
				CameraPosition.Y -= CameraSpeed;
			}

			// options
			if (Keyboard.GetState().IsKeyDown(Keys.F1))
			{
				DrawAllFonts = !DrawAllFonts;
			}

			Window.Title = $"Font Demonstration - Scale: {Scale:F2} - ColumnWidth: {ColumnWidth}";

			// TODO: Add your update logic here

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Palette.Shadow);

			// Create a translation matrix from CameraPosition
			var cameraTransform = Matrix.CreateTranslation(new Vector3(CameraPosition, 0f));

			_spriteBatch.Begin(
				SpriteSortMode.Immediate,
				samplerState: SamplerState.PointClamp,
				rasterizerState: _rasterizerState,
				transformMatrix: cameraTransform);

			var width = Window.ClientBounds.Width;
			var height = Window.ClientBounds.Height;

			const int spacing = 8;
			var currX = spacing;
			var currY = spacing;
			var index = 0;

			var fontsToDraw = DrawAllFonts ? _fonts : _fonts.Where(x => DrawFonts.Contains(x.Key));

			foreach (var font in fontsToDraw)
			{
				var fontSize = font.Value.MeasureString(font.Key) * Scale;

				if (currY + fontSize.Y + spacing > height)
				{
					currX += ColumnWidth + spacing;
					currY = spacing;
				}

				_spriteBatch.GraphicsDevice.ScissorRectangle = new Rectangle(currX + (int)CameraPosition.X, currY + (int)CameraPosition.Y, ColumnWidth, (int)fontSize.Y);

				var pos = new Vector2(currX, currY);
				const string printableAsciiChars = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

				// shadow
				_spriteBatch.DrawString(
					font.Value,
					$"{font.Key} - {printableAsciiChars}",
					pos - (Vector2.One * Scale),
					Palette.Dark,
					0f,
					Vector2.Zero,
					Scale,
					SpriteEffects.None,
					0);

				// highlight
				_spriteBatch.DrawString(
					font.Value,
					$"{font.Key} - {printableAsciiChars}",
					pos,
					Palette.Mid,
					0f,
					Vector2.Zero,
					Scale,
					SpriteEffects.None,
					0);

				currY += (int)fontSize.Y + spacing;
				index++;
			}

			_spriteBatch.End();

			base.Draw(gameTime);
		}
	}
}
