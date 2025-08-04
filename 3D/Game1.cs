using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shared;
using Shared.Components;
using System.Collections.Generic;

namespace _3D
{
	public class Game1 : ImGuiGame
	{
		// Terrain fields
		float[,] heightmap;
		VertexPositionColorNormal[] terrainVertices;
		int[] terrainIndices;
		Vector3[,] terrainNormals;
		BasicEffect terrainEffect;
		BasicEffect treeEffect;

		Effect basicShader;

		// Camera
		Camera3D _camera;

		// Terrain size
		TerrainGenParameters _lastTerrainGenParams;
		TerrainGenParameters TerrainGenParams { get; } = new();

		TerrainMeshParameters _lastTerrainMeshParams;
		TerrainMeshParameters TerrainMeshParams { get; } = new();

		TreeGeneratorParameters _lastTreeGenParams;
		TreeGeneratorParameters TreeGenParams { get; } = new();

		List<TreeInstance> Trees = new();

		// Lighting controls
		Vector3 terrainSpecularColor = new(0.2f, 0.2f, 0.2f);
		float terrainSpecularPower = 8f;
		Vector3 treeSpecularColor = new(0.2f, 0.2f, 0.2f);
		float treeSpecularPower = 8f;
		Vector3 ambientLightColor = new(0.2f, 0.2f, 0.2f);
		Vector3 terrainDiffuseColor = new(0.8f, 0.8f, 0.8f);
		Vector3 terrainEmissiveColor = new(0.0f, 0.0f, 0.0f);
		Vector3 treeDiffuseColor = new(0.8f, 0.8f, 0.8f);
		Vector3 treeEmissiveColor = new(0.0f, 0.0f, 0.0f);

		public Game1()
		{
			Graphics.GraphicsProfile = GraphicsProfile.HiDef;
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
		}

		protected override void Initialize()
		{
			_camera = new Camera3D(GraphicsDevice, new Vector3(256, 256, 256));
			base.Initialize();
		}

		protected override void LoadContent()
		{
			terrainEffect = new BasicEffect(GraphicsDevice)
			{
				LightingEnabled = true,
				TextureEnabled = false,
				VertexColorEnabled = false
			};
			terrainEffect.EnableDefaultLighting();

			treeEffect = new BasicEffect(GraphicsDevice)
			{
				LightingEnabled = true,
				TextureEnabled = false,
				VertexColorEnabled = false
			};
			treeEffect.EnableDefaultLighting();

			// Load the custom shader
			basicShader = Content.Load<Effect>("PixelShader");
		}

		void RegenerateMap(TerrainGenParameters terrainParams)
		{
			heightmap = TerrainHeightmapGenerator.GenerateSimplexHeightMap(terrainParams);
			TerrainMeshGenerator.GenerateTerrainMesh(
				heightmap,
				TerrainMeshParams,
				out terrainVertices,
				out terrainIndices,
				out terrainNormals
			);

			RegenerateTrees(TreeGenParams);
		}

		void RegenerateTrees(TreeGeneratorParameters treeParams)
			=> Trees = [.. TreeGenerator.GenerateTrees(heightmap, terrainNormals, treeParams)];

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			{
				Exit();
			}

			_camera.HandleInput(gameTime);

			// update the mesh - super expensive
			var genParamsChanged = _lastTerrainGenParams == null || TerrainGenParamsChanged(TerrainGenParams, _lastTerrainGenParams);
			var meshParamsChanged = _lastTerrainMeshParams == null || TerrainMeshParamsChanged(TerrainMeshParams, _lastTerrainMeshParams);
			var treeParamsChanged = _lastTreeGenParams == null || TreeGenParamsChanged(TreeGenParams, _lastTreeGenParams);

			if (genParamsChanged || meshParamsChanged)
			{
				RegenerateMap(TerrainGenParams);
				_lastTerrainGenParams = CloneTerrainGenParams(TerrainGenParams);
				_lastTerrainMeshParams = CloneTerrainMeshParams(TerrainMeshParams);
				_lastTreeGenParams = CloneTreeGenParams(TreeGenParams); // keep in sync
			}
			else if (treeParamsChanged)
			{
				RegenerateTrees(TreeGenParams);
				_lastTreeGenParams = CloneTreeGenParams(TreeGenParams);
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

			DrawTerrain();
			//DrawTerrainPixelShader();

			DrawTrees();
			DrawAxes();

			base.Draw(gameTime);
		}

		protected override void DrawImGui(GameTime gameTime)
		{
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

				// In DrawImGui, replace ImGui.ColorEdit3 calls to use System.Numerics.Vector3
				if (ImGui.CollapsingHeader("Lighting", ImGuiTreeNodeFlags.DefaultOpen))
				{
					// --- Terrain Lighting ---
					var numericsTerrainSpecularColor = ToNumerics(terrainSpecularColor);
					if (ImGui.ColorEdit3("Terrain Specular", ref numericsTerrainSpecularColor))
					{
						terrainSpecularColor = new Vector3(numericsTerrainSpecularColor.X, numericsTerrainSpecularColor.Y, numericsTerrainSpecularColor.Z);
					}
					ImGui.SliderFloat("Terrain Specular Power", ref terrainSpecularPower, 1f, 64f);

					var numericsTerrainDiffuseColor = ToNumerics(terrainDiffuseColor);
					if (ImGui.ColorEdit3("Terrain Diffuse", ref numericsTerrainDiffuseColor))
					{
						terrainDiffuseColor = new Vector3(numericsTerrainDiffuseColor.X, numericsTerrainDiffuseColor.Y, numericsTerrainDiffuseColor.Z);
					}

					var numericsTerrainEmissiveColor = ToNumerics(terrainEmissiveColor);
					if (ImGui.ColorEdit3("Terrain Emissive", ref numericsTerrainEmissiveColor))
					{
						terrainEmissiveColor = new Vector3(numericsTerrainEmissiveColor.X, numericsTerrainEmissiveColor.Y, numericsTerrainEmissiveColor.Z);
					}

					// --- Tree Lighting ---
					var numericsTreeSpecularColor = ToNumerics(treeSpecularColor);
					if (ImGui.ColorEdit3("Tree Specular", ref numericsTreeSpecularColor))
					{
						treeSpecularColor = new Vector3(numericsTreeSpecularColor.X, numericsTreeSpecularColor.Y, numericsTreeSpecularColor.Z);
					}
					ImGui.SliderFloat("Tree Specular Power", ref treeSpecularPower, 1f, 64f);

					var numericsTreeDiffuseColor = ToNumerics(treeDiffuseColor);
					if (ImGui.ColorEdit3("Tree Diffuse", ref numericsTreeDiffuseColor))
					{
						treeDiffuseColor = new Vector3(numericsTreeDiffuseColor.X, numericsTreeDiffuseColor.Y, numericsTreeDiffuseColor.Z);
					}

					var numericsTreeEmissiveColor = ToNumerics(treeEmissiveColor);
					if (ImGui.ColorEdit3("Tree Emissive", ref numericsTreeEmissiveColor))
					{
						treeEmissiveColor = new Vector3(numericsTreeEmissiveColor.X, numericsTreeEmissiveColor.Y, numericsTreeEmissiveColor.Z);
					}

					// --- Ambient ---
					var numericsAmbientLightColor = ToNumerics(ambientLightColor);
					if (ImGui.ColorEdit3("Ambient Light", ref numericsAmbientLightColor))
					{
						ambientLightColor = new Vector3(numericsAmbientLightColor.X, numericsAmbientLightColor.Y, numericsAmbientLightColor.Z);
					}
				}

				// --- Tree Generation ---
				if (ImGui.CollapsingHeader("Tree Generation", ImGuiTreeNodeFlags.DefaultOpen))
				{
					var treeCount = TreeGenParams.TreeCount;
					if (ImGui.SliderInt("Tree Count", ref treeCount, 0, 10000))
					{
						TreeGenParams.TreeCount = treeCount;
					}

					var minH = TreeGenParams.TreeHeightLimitMin;
					var maxH = TreeGenParams.TreeHeightLimitMax;
					if (ImGui.SliderFloat("Tree Height Min", ref minH, 0f, 256f))
					{
						TreeGenParams.TreeHeightLimitMin = minH;
					}

					if (ImGui.SliderFloat("Tree Height Max", ref maxH, 0f, 256f))
					{
						TreeGenParams.TreeHeightLimitMax = maxH;
					}

					var trunkBaseMin = TreeGenParams.TrunkBaseRadiusMin;
					var trunkBaseMax = TreeGenParams.TrunkBaseRadiusMax;
					if (ImGui.SliderFloat("Trunk Base Radius Min", ref trunkBaseMin, 0.01f, 2f))
					{
						TreeGenParams.TrunkBaseRadiusMin = trunkBaseMin;
					}

					if (ImGui.SliderFloat("Trunk Base Radius Max", ref trunkBaseMax, 0.01f, 2f))
					{
						TreeGenParams.TrunkBaseRadiusMax = trunkBaseMax;
					}

					var trunkSegMin = TreeGenParams.TrunkSegmentsMin;
					var trunkSegMax = TreeGenParams.TrunkSegmentsMax;
					if (ImGui.SliderInt("Trunk Segments Min", ref trunkSegMin, 3, 32))
					{
						TreeGenParams.TrunkSegmentsMin = trunkSegMin;
					}

					if (ImGui.SliderInt("Trunk Segments Max", ref trunkSegMax, 3, 32))
					{
						TreeGenParams.TrunkSegmentsMax = trunkSegMax;
					}

					var trunkRingMin = TreeGenParams.TrunkRingsMin;
					var trunkRingMax = TreeGenParams.TrunkRingsMax;
					if (ImGui.SliderInt("Trunk Rings Min", ref trunkRingMin, 1, 32))
					{
						TreeGenParams.TrunkRingsMin = trunkRingMin;
					}

					if (ImGui.SliderInt("Trunk Rings Max", ref trunkRingMax, 1, 32))
					{
						TreeGenParams.TrunkRingsMax = trunkRingMax;
					}

					// --- Add these for BranchCount and LeafCount ---
					var branchCountMin = TreeGenParams.BranchCountMin;
					var branchCountMax = TreeGenParams.BranchCountMax;
					if (ImGui.SliderInt("Branch Count Min", ref branchCountMin, 1, 64))
					{
						TreeGenParams.BranchCountMin = branchCountMin;
					}

					if (ImGui.SliderInt("Branch Count Max", ref branchCountMax, 1, 64))
					{
						TreeGenParams.BranchCountMax = branchCountMax;
					}

					var leafCountMin = TreeGenParams.LeafCountMin;
					var leafCountMax = TreeGenParams.LeafCountMax;
					if (ImGui.SliderInt("Leaf Count Min", ref leafCountMin, 1, 128))
					{
						TreeGenParams.LeafCountMin = leafCountMin;
					}

					if (ImGui.SliderInt("Leaf Count Max", ref leafCountMax, 1, 128))
					{
						TreeGenParams.LeafCountMax = leafCountMax;
					}
				}

			}
			ImGui.End();
		}

		private void DrawTerrain()
		{
			terrainEffect.View = _camera.ViewMatrix;
			terrainEffect.Projection = _camera.ProjectionMatrix;
			terrainEffect.World = Matrix.Identity;
			terrainEffect.VertexColorEnabled = true;
			terrainEffect.SpecularColor = new Vector3(terrainSpecularColor.X, terrainSpecularColor.Y, terrainSpecularColor.Z);
			terrainEffect.SpecularPower = terrainSpecularPower;
			terrainEffect.AmbientLightColor = ambientLightColor;
			terrainEffect.DiffuseColor = terrainDiffuseColor;
			terrainEffect.EmissiveColor = terrainEmissiveColor;

			foreach (var pass in terrainEffect.CurrentTechnique.Passes)
			{
				pass.Apply();
				GraphicsDevice.DrawUserIndexedPrimitives(
					PrimitiveType.TriangleList,
					terrainVertices,
					0,
					terrainVertices.Length,
					terrainIndices,
					0,
					terrainIndices.Length / 3
				);
			}
		}

		private void DrawTerrainPixelShader()
		{
			// Set shader parameters
			basicShader.Parameters["WorldViewProjection"].SetValue(
				Matrix.Identity * _camera.ViewMatrix * _camera.ProjectionMatrix);

			foreach (var pass in basicShader.CurrentTechnique.Passes)
			{
				pass.Apply();
				GraphicsDevice.DrawUserIndexedPrimitives(
					PrimitiveType.TriangleList,
					terrainVertices,
					0,
					terrainVertices.Length,
					terrainIndices,
					0,
					terrainIndices.Length / 3
				);
			}
		}

		private void DrawTrees()
		{
			treeEffect.View = _camera.ViewMatrix;
			treeEffect.Projection = _camera.ProjectionMatrix;
			treeEffect.World = Matrix.Identity;
			treeEffect.VertexColorEnabled = true;
			treeEffect.SpecularColor = new Vector3(treeSpecularColor.X, treeSpecularColor.Y, treeSpecularColor.Z);
			treeEffect.SpecularPower = treeSpecularPower;
			treeEffect.AmbientLightColor = ambientLightColor;
			treeEffect.DiffuseColor = treeDiffuseColor;
			treeEffect.EmissiveColor = treeEmissiveColor;

			foreach (var pass in treeEffect.CurrentTechnique.Passes)
			{
				pass.Apply();
				foreach (var tree in Trees)
				{
					GraphicsDevice.DrawUserIndexedPrimitives(
						PrimitiveType.TriangleList,
						tree.Mesh.Vertices,
						0,
						tree.Mesh.Vertices.Length,
						tree.Mesh.Indices,
						0,
						tree.Mesh.Indices.Length / 3
					);
				}
			}
		}

		private void DrawAxes()
		{
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
		}

		static bool TerrainGenParamsChanged(TerrainGenParameters a, TerrainGenParameters b)
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

		static bool TerrainMeshParamsChanged(TerrainMeshParameters a, TerrainMeshParameters b)
		{
			if (a == null || b == null)
			{
				return true;
			}

			return a.CliffAngle != b.CliffAngle || a.CliffAngleLerp != b.CliffAngleLerp;
		}

		static TerrainMeshParameters CloneTerrainMeshParams(TerrainMeshParameters src)
		{
			return new TerrainMeshParameters
			{
				CliffAngle = src.CliffAngle,
				CliffAngleLerp = src.CliffAngleLerp
			};
		}

		static TerrainGenParameters CloneTerrainGenParams(TerrainGenParameters src)
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

		static bool TreeGenParamsChanged(TreeGeneratorParameters a, TreeGeneratorParameters b)
		{
			if (a == null || b == null)
			{
				return true;
			}

			return a.TreeCount != b.TreeCount ||
				a.TreeHeightLimitMin != b.TreeHeightLimitMin ||
				a.TreeHeightLimitMax != b.TreeHeightLimitMax ||
				a.TrunkBaseRadiusMin != b.TrunkBaseRadiusMin ||
				a.TrunkBaseRadiusMax != b.TrunkBaseRadiusMax ||
				a.TrunkSegmentsMin != b.TrunkSegmentsMin ||
				a.TrunkSegmentsMax != b.TrunkSegmentsMax ||
				a.TrunkRingsMin != b.TrunkRingsMin ||
				a.TrunkRingsMax != b.TrunkRingsMax ||
				a.BranchCountMin != b.BranchCountMin ||
				a.BranchCountMax != b.BranchCountMax ||
				a.LeafCountMin != b.LeafCountMin ||
				a.LeafCountMax != b.LeafCountMax;
		}

		static TreeGeneratorParameters CloneTreeGenParams(TreeGeneratorParameters src)
		{
			return new TreeGeneratorParameters
			{
				TreeCount = src.TreeCount,
				TreeHeightLimitMin = src.TreeHeightLimitMin,
				TreeHeightLimitMax = src.TreeHeightLimitMax,
				TrunkBaseRadiusMin = src.TrunkBaseRadiusMin,
				TrunkBaseRadiusMax = src.TrunkBaseRadiusMax,
				TrunkSegmentsMin = src.TrunkSegmentsMin,
				TrunkSegmentsMax = src.TrunkSegmentsMax,
				TrunkRingsMin = src.TrunkRingsMin,
				TrunkRingsMax = src.TrunkRingsMax,
				BranchCountMin = src.BranchCountMin,
				BranchCountMax = src.BranchCountMax,
				LeafCountMin = src.LeafCountMin,
				LeafCountMax = src.LeafCountMax
			};
		}

		private static System.Numerics.Vector3 ToNumerics(Vector3 v)
			=> new(v.X, v.Y, v.Z);
	}
}
