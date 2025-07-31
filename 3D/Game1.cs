using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.ImGuiNet;
using Shared.Components;

namespace _3D
{
	public class Game1 : Game
	{
		GraphicsDeviceManager _graphics;
		SpriteBatch _spriteBatch;
		ImGuiRenderer GuiRenderer;
		FpsComponent fpsComponent;

		// Terrain fields
		VertexPositionColorNormal[] vertices;
		int[] indices;
		BasicEffect _effect;

		// Camera
		private Camera3D _camera;

		// Terrain size
		TerrainGenParameters TerrainGenParams { get; set; } = new();
		TerrainMeshParameters TerrainMeshParams { get; set; } = new();

		public Game1()
		{
			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
			_graphics.PreferredBackBufferWidth = 1920;
			_graphics.PreferredBackBufferHeight = 1080;
		}

		protected override void Initialize()
		{
			_spriteBatch = new SpriteBatch(GraphicsDevice);
			_camera = new Camera3D(GraphicsDevice, new Vector3(256, 256, 256));

			GuiRenderer = new ImGuiRenderer(this);
			GuiRenderer.RebuildFontAtlas();

			fpsComponent = new FpsComponent(this, _spriteBatch);
			Components.Add(fpsComponent);

			base.Initialize();
		}

		protected override void LoadContent()
		{
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			GenerateTerrainMesh(GenerateSimplexHeightMap(TerrainGenParams));

			_effect = new BasicEffect(GraphicsDevice)
			{
				LightingEnabled = true,
				TextureEnabled = false,
				VertexColorEnabled = false
			};
			_effect.EnableDefaultLighting();
		}

		private float[,] GenerateSimplexHeightMap(TerrainGenParameters tgParams)
		{
			var noise = new SimplexNoise();
			var map = new float[tgParams.Width, tgParams.Height];

			// For normalization
			float min = float.MaxValue, max = float.MinValue;

			for (var z = 0; z < tgParams.Height; z++)
			{
				for (var x = 0; x < tgParams.Width; x++)
				{
					var frequency = tgParams.Frequency;
					var amplitude = tgParams.Amplitude;
					var noiseHeight = 0f;

					for (var o = 0; o < tgParams.Octaves; o++)
					{
						var nx = x * tgParams.Scale * frequency;
						var nz = z * tgParams.Scale * frequency;
						var value = (float)noise.Evaluate(nx, nz);

						noiseHeight += value * amplitude;

						amplitude *= tgParams.Persistence;
						frequency *= tgParams.Lacunarity;
					}

					// Track min/max for normalization
					if (noiseHeight < min)
					{
						min = noiseHeight;
					}

					if (noiseHeight > max)
					{
						max = noiseHeight;
					}

					map[x, z] = noiseHeight;
				}
			}

			// Normalize to [0,1] and apply heightScale
			for (var z = 0; z < tgParams.Height; z++)
			{
				for (var x = 0; x < tgParams.Width; x++)
				{
					var norm = (map[x, z] - min) / (max - min);
					map[x, z] = norm * tgParams.HeightScale;
				}
			}

			return map;
		}

		private void GenerateTerrainMesh(float[,] heightMap)
		{
			var width = heightMap.GetLength(0);
			var height = heightMap.GetLength(1);

			var minHeight = float.MaxValue;
			var maxHeight = float.MinValue;

			for (var z = 0; z < height; z++)
			{
				for (var x = 0; x < width; x++)
				{
					var h = heightMap[x, z];
					if (h < minHeight)
					{
						minHeight = h;
					}

					if (h > maxHeight)
					{
						maxHeight = h;
					}
				}
			}

			// Generate indices
			indices = new int[(width - 1) * (height - 1) * 6];
			var i = 0;
			for (var z = 0; z < height - 1; z++)
			{
				for (var x = 0; x < width - 1; x++)
				{
					var topLeft = x + (z * width);
					var topRight = x + 1 + (z * width);
					var bottomLeft = x + ((z + 1) * width);
					var bottomRight = x + 1 + ((z + 1) * width);

					// Counter-clockwise winding:
					indices[i++] = topLeft;
					indices[i++] = topRight;
					indices[i++] = bottomLeft;

					indices[i++] = topRight;
					indices[i++] = bottomRight;
					indices[i++] = bottomLeft;
				}
			}

			vertices = new VertexPositionColorNormal[width * height];

			var xOffset = width / 2f;
			var zOffset = height / 2f;

			for (var z = 0; z < height; z++)
			{
				for (var x = 0; x < width; x++)
				{
					vertices[x + (z * width)] = new VertexPositionColorNormal(
						new Vector3(x - xOffset, heightMap[x, z], z - zOffset),
						Color.Magenta, // Temporary, will be recalculated below
						Vector3.Up // Temporary, will be recalculated below
					);
				}
			}

			// Calculate normals
			for (var v = 0; v < vertices.Length; v++)
			{
				vertices[v].Normal = Vector3.Zero;
			}

			for (var n = 0; n < indices.Length / 3; n++)
			{
				var i1 = indices[n * 3];
				var i2 = indices[(n * 3) + 1];
				var i3 = indices[(n * 3) + 2];

				var side1 = vertices[i2].Position - vertices[i1].Position;
				var side2 = vertices[i3].Position - vertices[i1].Position;
				var normal = Vector3.Cross(side2, side1);

				vertices[i1].Normal += normal;
				vertices[i2].Normal += normal;
				vertices[i3].Normal += normal;
			}
			for (var v = 0; v < vertices.Length; v++)
			{
				vertices[v].Normal.Normalize();
			}

			// Now, color if normal is steeper than cliff angle
			var cliffAngle = MathHelper.ToRadians(90 - TerrainMeshParams.CliffAngle);

			for (var v = 0; v < vertices.Length; v++)
			{
				ref var currentVert = ref vertices[v];
				var t = (currentVert.Position.Y - minHeight) / (maxHeight - minHeight);
				Color heightColour;
				if (t < 0.2f)
				{
					heightColour = Color.Blue;
				}
				else if (t < 0.7f)
				{
					heightColour = Color.Green;
				}
				else if (t < 0.9f)
				{
					heightColour = Color.Gray;
				}
				else
				{
					heightColour = Color.White;
				}

				currentVert.Color = heightColour;

				var dot = Vector3.Dot(currentVert.Normal, Vector3.Up);
				if (dot < cliffAngle)
				{
					currentVert.Color = Color.Lerp(heightColour, Color.DarkGray, TerrainMeshParams.CliffAngleLerp);
				}
			}
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			{
				Exit();
			}

			_camera.HandleInput(gameTime);

			// update the mesh - super expensive
			bool genParamsChanged = _lastTerrainGenParams == null || TerrainGenParamsChanged(TerrainGenParams, _lastTerrainGenParams);
			bool meshParamsChanged = _lastTerrainMeshParams == null || TerrainMeshParamsChanged(TerrainMeshParams, _lastTerrainMeshParams);
			if (genParamsChanged || meshParamsChanged)
			{
				GenerateTerrainMesh(GenerateSimplexHeightMap(TerrainGenParams));
				_lastTerrainGenParams = CloneTerrainGenParams(TerrainGenParams);
				_lastTerrainMeshParams = CloneTerrainMeshParams(TerrainMeshParams);
			}

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Viewport = new Viewport(0, 0, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
			GraphicsDevice.DepthStencilState = DepthStencilState.Default; // Or a custom one that enables depth testing
			GraphicsDevice.BlendState = BlendState.Opaque; // Opaque for solid 3D objects
			GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise; // Or whatever your 3D culling is

			GraphicsDevice.Clear(Color.SteelBlue);

			_effect.View = _camera.ViewMatrix;
			_effect.Projection = _camera.ProjectionMatrix;
			_effect.World = Matrix.Identity;
			_effect.VertexColorEnabled = true;

			foreach (var pass in _effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				GraphicsDevice.DrawUserIndexedPrimitives(
					PrimitiveType.TriangleList,
					vertices,
					0,
					vertices.Length,
					indices,
					0,
					indices.Length / 3
				);
			}

			// --- Draw world axes ---
			var axisEffect = new BasicEffect(GraphicsDevice)
			{
				VertexColorEnabled = true,
				View = _camera.ViewMatrix,
				Projection = _camera.ProjectionMatrix,
				World = Matrix.Identity,
			};

			axisEffect.View = _camera.ViewMatrix;
			axisEffect.Projection = _camera.ProjectionMatrix;

			VertexPositionColor[] axisVerts =
			[
				// X axis (red)
				new(new Vector3(0, 0, 0), Color.Red),
				new(new Vector3(32, 0, 0), Color.Red),
				// Y axis (green)
				new(new Vector3(0, 0, 0), Color.Green),
				new(new Vector3(0, 32, 0), Color.Green),
				// Z axis (blue)
				new(new Vector3(0, 0, 0), Color.Blue),
				new(new Vector3(0, 0, 32), Color.Blue),
			];

			foreach (var pass in axisEffect.CurrentTechnique.Passes)
			{
				pass.Apply();
				GraphicsDevice.DrawUserPrimitives(
					PrimitiveType.LineList,
					axisVerts,
					0,
					3 // 3 lines
				);
			}

			DrawImGui(gameTime);

			base.Draw(gameTime);
		}

		private void DrawImGui(GameTime gameTime)
		{
			GuiRenderer.BeginLayout(gameTime);

			if (ImGui.Begin("3D Terrain Editor"))
			{
				// --- Camera Section ---
				if (ImGui.CollapsingHeader("Camera", ImGuiTreeNodeFlags.DefaultOpen))
				{
					ImGui.Text("Camera Position: " + _camera.Position);
					ImGui.Text("Camera Rotation: " + _camera.Rotation);

					// Field of View control (degrees for user, radians internally)
					var fovDegrees = MathHelper.ToDegrees(_camera.FieldOfView);
					if (ImGui.SliderFloat("Field of View", ref fovDegrees, 10f, 120f, "%.1f deg"))
					{
						_camera.FieldOfView = MathHelper.ToRadians(fovDegrees);
					}
				}

				// --- Terrain Section ---
				if (ImGui.CollapsingHeader("Terrain Generation", ImGuiTreeNodeFlags.DefaultOpen))
				{
					var freq = TerrainGenParams.Frequency;
					if (ImGui.SliderFloat("Frequency", ref freq, 0f, 5f))
					{
						TerrainGenParams.Frequency = freq;
					}

					var amp = TerrainGenParams.Amplitude;
					if (ImGui.SliderFloat("Amplitude", ref amp, 0f, 5f))
					{
						TerrainGenParams.Amplitude = amp;
					}

					var scale = TerrainGenParams.Scale;
					if (ImGui.SliderFloat("Scale", ref scale, 0.0001f, 0.01f, "%.5f"))
					{
						TerrainGenParams.Scale = scale;
					}

					var heightScale = TerrainGenParams.HeightScale;
					if (ImGui.SliderFloat("Height Scale", ref heightScale, 1f, 512f))
					{
						TerrainGenParams.HeightScale = heightScale;
					}

					var octaves = TerrainGenParams.Octaves;
					if (ImGui.SliderInt("Octaves", ref octaves, 1, 16))
					{
						TerrainGenParams.Octaves = octaves;
					}

					var persistence = TerrainGenParams.Persistence;
					if (ImGui.SliderFloat("Persistence", ref persistence, 0f, 1f))
					{
						TerrainGenParams.Persistence = persistence;
					}

					var lacunarity = TerrainGenParams.Lacunarity;
					if (ImGui.SliderFloat("Lacunarity", ref lacunarity, 1f, 4f))
					{
						TerrainGenParams.Lacunarity = lacunarity;
					}

					var width = TerrainGenParams.Width;
					if (ImGui.SliderInt("Width", ref width, 16, 2048))
					{
						TerrainGenParams.Width = width;
					}

					var height = TerrainGenParams.Height;
					if (ImGui.SliderInt("Height", ref height, 16, 2048))
					{
						TerrainGenParams.Height = height;
					}
				}

				if (ImGui.CollapsingHeader("Terrain Mesh", ImGuiTreeNodeFlags.DefaultOpen))
				{
					var cliffAngle = TerrainMeshParams.CliffAngle;
					if (ImGui.SliderInt("Cliff Angle", ref cliffAngle, 0, 90, "%d deg"))
					{
						TerrainMeshParams.CliffAngle = cliffAngle;
					}

					var cliffLerp = TerrainMeshParams.CliffAngleLerp;
					if (ImGui.SliderFloat("Cliff Angle Lerp", ref cliffLerp, 0f, 1f, "%.2f"))
					{
						TerrainMeshParams.CliffAngleLerp = cliffLerp;
					}
				}

				ImGui.End();
			}

			GuiRenderer.EndLayout();
		}

		// Add this helper method to compare TerrainGenParameters
		private bool TerrainGenParamsChanged(TerrainGenParameters a, TerrainGenParameters b)
		{
			if (a == null || b == null)
			{
				return true;
			}

			return a.Width != b.Width ||
				   a.Height != b.Height ||
				   a.Scale != b.Scale ||
				   a.HeightScale != b.HeightScale ||
				   a.Octaves != b.Octaves ||
				   a.Frequency != b.Frequency ||
				   a.Amplitude != b.Amplitude ||
				   a.Persistence != b.Persistence ||
				   a.Lacunarity != b.Lacunarity;
		}

		// Add this helper method to compare TerrainMeshParameters
		private bool TerrainMeshParamsChanged(TerrainMeshParameters a, TerrainMeshParameters b)
		{
			if (a == null || b == null)
			{
				return true;
			}

			return a.CliffAngle != b.CliffAngle || a.CliffAngleLerp != b.CliffAngleLerp;
		}

		// Add this helper to clone TerrainMeshParameters
		private TerrainMeshParameters CloneTerrainMeshParams(TerrainMeshParameters src)
		{
			return new TerrainMeshParameters
			{
				CliffAngle = src.CliffAngle,
				CliffAngleLerp = src.CliffAngleLerp
			};
		}

		// Add this field to your Game1 class:
		private TerrainMeshParameters _lastTerrainMeshParams = null;

		// Add this helper to clone TerrainGenParameters
		private TerrainGenParameters CloneTerrainGenParams(TerrainGenParameters src)
		{
			return new TerrainGenParameters
			{
				Width = src.Width,
				Height = src.Height,
				Scale = src.Scale,
				HeightScale = src.HeightScale,
				Octaves = src.Octaves,
				Frequency = src.Frequency,
				Amplitude = src.Amplitude,
				Persistence = src.Persistence,
				Lacunarity = src.Lacunarity
			};
		}

		// Add this field to your Game1 class:
		private TerrainGenParameters _lastTerrainGenParams = null;
	}
}
