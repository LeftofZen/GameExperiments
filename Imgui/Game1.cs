using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.ImGuiNet;

using Vec2 = System.Numerics.Vector2;
using Vec3 = System.Numerics.Vector3;
using Vec4 = System.Numerics.Vector4;

namespace Imgui
{
	public class Game1 : Game
	{
		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;

		ImGuiRenderer GuiRenderer;
		bool WasResized = false;

		public Game1()
		{
			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;

			_graphics.PreferredBackBufferWidth = 1024;
			_graphics.PreferredBackBufferHeight = 768;

			Window.AllowUserResizing = true; // true;
			Window.ClientSizeChanged += delegate { WasResized = true; };
		}

		protected override void Initialize()
		{
			// TODO: Add your initialization logic here
			GuiRenderer = new ImGuiRenderer(this);
			GuiRenderer.RebuildFontAtlas();

			base.Initialize();
		}

		protected override void LoadContent()
		{
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			// TODO: use this.Content to load your game content here
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			// Update ImGuiRenderer
			//GuiRenderer.Update(gameTime);

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			// Begin ImGui frame
			GuiRenderer.BeginLayout(gameTime);

			// Example ImGui UI
			ImGui.Text("Hello, ImGui!");

			if (ImGui.BeginMainMenuBar())
			{
				if (ImGui.BeginMenu("File"))
				{
					ShowExampleMenuFile();
					ImGui.EndMenu();
				}
				if (ImGui.BeginMenu("Edit"))
				{
					if (ImGui.MenuItem("Undo", "CTRL+Z")) { }
					if (ImGui.MenuItem("Redo", "CTRL+Y", false, false)) { }
					ImGui.Separator();
					if (ImGui.MenuItem("Cut", "CTRL+X")) { }
					if (ImGui.MenuItem("Copy", "CTRL+C")) { }
					if (ImGui.MenuItem("Paste", "CTRL+V")) { }
					ImGui.EndMenu();
				}
				ImGui.EndMainMenuBar();
			}


			// End ImGui frame
			GuiRenderer.EndLayout();

			base.Draw(gameTime);
		}

		private void ShowExampleMenuFile()
		{
			ImGui.MenuItem("(demo menu)", null, false, false);
			if (ImGui.MenuItem("New")) { }
			if (ImGui.MenuItem("Open", "Ctrl+O")) { }
			if (ImGui.MenuItem("Open Recent"))
			{
				ImGui.MenuItem("fish_hat.c");
				ImGui.MenuItem("fish_hat.inl");
				ImGui.MenuItem("fish_hat.h");
				if (ImGui.MenuItem("More.."))
				{
					ImGui.MenuItem("Hello");
					ImGui.MenuItem("Sailor");
					if (ImGui.BeginMenu("Recurse.."))
					{
						ShowExampleMenuFile();
						ImGui.EndMenu();
					}
					ImGui.EndMenu();
				}
				ImGui.EndMenu();
			}
			if (ImGui.MenuItem("Save", "Ctrl+S")) { }
			if (ImGui.MenuItem("Save As ..")) { }

			ImGui.Separator();
			if (ImGui.BeginMenu("Options"))
			{
				bool enabled = true;
				ImGui.MenuItem("Enabled", "", enabled);
				ImGui.BeginChild("child", new Vec2(0, 60), ImGuiChildFlags.Borders);
				for (int i = 0; i < 10; i++)
				{
					ImGui.Text(string.Format("Scrolling Text {0}", i));
				}
				ImGui.EndChild();
				float f = 0.5f;
				int n = 0;
				ImGui.SliderFloat("Value", ref f, 0.0f, 1.0f);
				ImGui.InputFloat("Input", ref f, 0.1f);
				ImGui.Combo("Combo", ref n, "Yes\0No\0Maybe\0\0");
				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Colors"))
			{
				float sz = ImGui.GetTextLineHeight();
				//ImGui.Text(((int)ImGuiCol.COUNT).ToString()); //Test
				for (int i = 0; i < (int)ImGuiCol.COUNT; i++)
				{
					string name = ImGui.GetStyleColorName((ImGuiCol)i);
					Vec2 p = ImGui.GetCursorScreenPos();
					ImGui.GetWindowDrawList().AddRectFilled(p, new Vec2(p.X + sz, p.Y + sz), ImGui.GetColorU32((ImGuiCol)i));
					ImGui.Dummy(new Vec2(sz, sz));
					ImGui.SameLine();
					ImGui.MenuItem(name);
				}
				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Options")) //Append!
			{
				bool b = true;
				ImGui.Checkbox("SomeOption", ref b);
				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Disabled", false)) { } //Disabled
			if (ImGui.MenuItem("Checked", null, true)) { }
			if (ImGui.MenuItem("Quit", "Alt+F4"))
			{
				Exit();
			}
		}
	}
}
