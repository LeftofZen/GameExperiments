using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;

namespace LLM_AI
{
	public class Game1 : ImGuiGame
	{
		private string _userInput = string.Empty;
		private bool _submitted = false;
		private byte[] _inputBuffer = new byte[256];

		public Game1()
		{
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
			// TODO: use this.Content to load your game content here
		}

		protected override void Update(GameTime gameTime)
		{
			// TODO: Add your update logic here

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			// TODO: Add your drawing code here

			base.Draw(gameTime);
		}

		void Process(string input)
		{
			// this uses the Phi-3 Model from Microsoft:
			// https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/blob/main/Phi-3-mini-4k-instruct-q4.gguf
		}

		protected override void DrawImGui(GameTime gameTime)
		{
			if (ImGui.Begin("LLM-AI", ImGuiWindowFlags.AlwaysAutoResize))
			{
				if (ImGui.InputText("Input", _inputBuffer, (uint)_inputBuffer.Length))
				{
					_userInput = System.Text.Encoding.UTF8.GetString(_inputBuffer).TrimEnd('\0');
				}

				if (ImGui.Button("Submit"))
				{
					_submitted = true;
					Process(_userInput);
				}

				if (ImGui.Button("Clear"))
				{
					_submitted = false;
					_userInput = string.Empty;
				}

				if (_submitted)
				{
					ImGui.Text($"You submitted: {_userInput}");
				}
			}
			ImGui.End();
		}
	}
}
