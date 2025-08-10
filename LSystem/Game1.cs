using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;
using System.Collections.Generic;
using System;

namespace LSystem
{
	public record LSystemParameters(int Iterations, float Angle, string Constants, string Axiom, params string[] Rules);

	public class Game1 : ImGuiGame
	{
		private string _userInput = string.Empty;
		private bool _submitted = false;
		private byte[] _inputBuffer = new byte[256];

		LSystemParameters Params { get; set; } = new LSystemParameters(4, 60f, "F", "F", "F->F+F--F+F");

		public Game1()
		{
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
		}

		protected override void Initialize()
		{
			base.Initialize();
		}

		protected override void LoadContent()
		{
			// TODO: use this.Content to load your game content here
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			if (_submitted && !string.IsNullOrWhiteSpace(_userInput))
			{
				DrawLSystem(_userInput);
			}

			base.Draw(gameTime);
		}

		protected override void DrawImGui(GameTime gameTime)
		{
			if (ImGui.Begin("L-System", ImGuiWindowFlags.AlwaysAutoResize))
			{
				//if (ImGui.InputInt("Iterations", ref Params.Iterations, 1, 10))
				//{
				//	// Update iterations if changed
				//}

				if (ImGui.InputText("Input", _inputBuffer, (uint)_inputBuffer.Length))
				{
					_userInput = System.Text.Encoding.UTF8.GetString(_inputBuffer).TrimEnd('\0');
				}

				if (ImGui.Button("Submit"))
				{
					_submitted = true;
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

		/// <summary>
		/// Draws a visual representation of the L-system described in the input string.
		/// Example input: "F;F->F+F--F+F"
		/// </summary>
		private void DrawLSystem(string lsystem)
		{
			// Parse axiom and rules
			var parts = lsystem.Split(';');
			if (parts.Length < 2) return;
			string axiom = parts[0].Trim();
			var rules = new Dictionary<char, string>();
			foreach (var rule in parts[1..])
			{
				var r = rule.Split("->");
				if (r.Length == 2)
				{
					char key = r[0].Trim()[0];
					string value = r[1].Trim();
					rules[key] = value;
				}
			}

			// Generate L-system string (iterate 4 times)
			string current = axiom;
			int iterations = 4;
			for (int i = 0; i < iterations; i++)
			{
				var next = new System.Text.StringBuilder();
				foreach (char c in current)
				{
					if (rules.ContainsKey(c))
						next.Append(rules[c]);
					else
						next.Append(c);
				}
				current = next.ToString();
			}

			// Turtle graphics: draw lines
			var center = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
			float angle = -90f; // Upwards
			float step = 10f;
			float turn = 60f;
			var stack = new Stack<(Vector2 pos, float ang)>();
			Vector2 pos = center;

			var verts = new List<VertexPositionColor>();
			var colors = new List<Color> { Color.White, Color.Yellow, Color.Green, Color.Red, Color.Blue };
			Color color = colors[0];
			foreach (char c in current)
			{
				if (c == 'F')
				{
					Vector2 newPos = pos + new Vector2((float)Math.Cos(Math.PI * angle / 180.0) * step, (float)Math.Sin(Math.PI * angle / 180.0) * step);
					verts.Add(new VertexPositionColor(new Vector3(pos, 0), color));
					verts.Add(new VertexPositionColor(new Vector3(newPos, 0), color));
					pos = newPos;
				}
				else if (c == '+')
				{
					angle += turn;
				}
				else if (c == '-')
				{
					angle -= turn;
				}
				else if (c == '[')
				{
					stack.Push((pos, angle));
				}
				else if (c == ']')
				{
					if (stack.Count > 0)
					{
						(var p, var a) = stack.Pop();
						pos = p;
						angle = a;
					}
				}
			}

			if (verts.Count > 1)
			{
				var effect = new BasicEffect(GraphicsDevice)
				{
					VertexColorEnabled = true,
					World = Matrix.Identity,
					View = Matrix.Identity,
					Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 0, 1)
				};
				foreach (var pass in effect.CurrentTechnique.Passes)
				{
					pass.Apply();
					GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, verts.ToArray(), 0, verts.Count / 2);
				}
			}
		}
	}
}
