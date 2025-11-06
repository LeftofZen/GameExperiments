using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace _3DTrain
{
	public enum TileLayerType
	{
		Surface, Station, Track, Tree, Building, Vehicle
	}

	public class TileLayerItem
	{
		public TileLayerType Type { get; set; }
		public int Height { get; set; }
	}

	public class TileColumn
	{
		public List<TileLayerItem> Items { get; set; } = [];
	}

	public class Game1 : ImGuiGame
	{
		Camera3D _camera;
		Texture2D _tileset;

		// Heightmap and billboard rendering
		TileColumn[,] _heightmap;
		BillboardRenderer _billboardRenderer;
		BasicEffect _billboardEffect;

		// Heightmap generation parameters
		int _mapWidth = 32;
		int _mapHeight = 32;
		float _tileSize = 2f; // Size of each tile in 3D space
		float _tileHeightInWorld = 32f / 32f; // Height of one tile in world units (32 pixels = 1 unit)

		// Tileset configuration (assuming a tileset with multiple tiles)
		int _tilesetColumns = 16;  // Number of tiles across
		int _tilesetRows = 42;     // Number of tiles down
		int _tileWidth = 64;      // Width of each tile in pixels
		int _tileHeight = 64;     // Height of each tile in pixels (for one tile)

		// Grid settings
		bool _showGrid = true;

		public Game1()
		{
			Graphics.GraphicsProfile = GraphicsProfile.HiDef;
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
		}

		protected override void Initialize()
		{
			// Calculate center of the map
			var mapCenterX = _mapWidth * _tileSize / 2;
			var mapCenterZ = _mapHeight * _tileSize / 2;
			//var mapCenter = new Vector3(mapCenterX, 0, mapCenterZ);

			// Set up isometric camera position
			// For isometric view: 45° from horizontal, 45° looking down
			const float distance = 80f; // Distance from center
			var height = distance * (float)Math.Sin(Math.PI / 4); // 45° elevation
			var horizontalDistance = distance * (float)Math.Cos(Math.PI / 4);

			// Position camera at 45° angle (northeast direction in world space)
			var cameraX = mapCenterX + horizontalDistance * (float)Math.Cos(Math.PI / 4);
			var cameraZ = mapCenterZ + horizontalDistance * (float)Math.Sin(Math.PI / 4);
			var cameraPosition = new Vector3(cameraX, height, cameraZ);

			// Initialize camera looking at the center of the map
			_camera = new Camera3D(GraphicsDevice, cameraPosition);

			// Set to isometric projection mode
			_camera.CurrentProjectionMode = Camera3D.ProjectionMode.Orthographic;
			_camera.IsometricZoom = 50f; // Good starting zoom for isometric view
			_camera.OrthographicZoom = 60f;

			// Generate a simple heightmap
			GenerateHeightmap();

			// Initialize billboard renderer
			_billboardRenderer = new BillboardRenderer(GraphicsDevice);

			base.Initialize();
		}

		protected override void LoadContent()
		{
			_tileset = Content.Load<Texture2D>("Textures//iso_tileset");

			// Create effect for billboards
			_billboardEffect = new BasicEffect(GraphicsDevice)
			{
				TextureEnabled = true,
				VertexColorEnabled = true,
				LightingEnabled = false
			};
		}

		private void GenerateHeightmap()
		{
			// Generate a smooth heightmap using simple averaging
			_heightmap = new TileColumn[_mapWidth, _mapHeight];
			var random = new Random();

			// First pass: generate random base heights
			var baseHeights = new float[_mapWidth, _mapHeight];
			for (var z = 0; z < _mapHeight; z++)
			{
				for (var x = 0; x < _mapWidth; x++)
				{
					baseHeights[x, z] = (float)random.NextDouble() * 20f;
				}
			}

			// Second pass: smooth using average of neighbours (simple box blur)
			const int smoothingPasses = 1;
			for (var pass = 0; pass < smoothingPasses; pass++)
			{
				var smoothed = new float[_mapWidth, _mapHeight];
				for (var z = 0; z < _mapHeight; z++)
				{
					for (var x = 0; x < _mapWidth; x++)
					{
						float sum = 0;
						var count = 0;

						// Average with neighbours
						for (var dz = -1; dz <= 1; dz++)
						{
							for (var dx = -1; dx <= 1; dx++)
							{
								var nx = x + dx;
								var nz = z + dz;

								if (nx >= 0 && nx < _mapWidth && nz >= 0 && nz < _mapHeight)
								{
									sum += baseHeights[nx, nz];
									count++;
								}
							}
						}

						smoothed[x, z] = sum / count;
					}
				}
				baseHeights = smoothed;
			}

			// Convert to integer heights
			for (var z = 0; z < _mapHeight; z++)
			{
				for (var x = 0; x < _mapWidth; x++)
				{
					var height = Math.Clamp((int)Math.Round(baseHeights[x, z]), 1, 100);
					var column = new TileColumn() { Items = [new() { Type = TileLayerType.Surface, Height = height }] };
					if (x == 16 && z is 16 or 17 or 18)
					{
						// Place a station at the center
						column.Items.Add(new TileLayerItem { Type = TileLayerType.Station, Height = height });
					}
					else if (random.NextDouble() < 0.1)
					{
						// Randomly place some trees
						column.Items.Add(new TileLayerItem { Type = TileLayerType.Tree, Height = height });
					}

					_heightmap[x, z] = column;
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

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			// Setup 3D rendering state
			GraphicsDevice.DepthStencilState = DepthStencilState.Default;
			GraphicsDevice.BlendState = BlendState.AlphaBlend;
			GraphicsDevice.RasterizerState = RasterizerState.CullNone;
			GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

			// Draw the billboards
			DrawBillboardTiles();

			// Reset to 2D rendering state for screen-space overlay
			GraphicsDevice.DepthStencilState = DepthStencilState.None;
			GraphicsDevice.BlendState = BlendState.AlphaBlend;
			GraphicsDevice.RasterizerState = RasterizerState.CullNone;
			GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

			// Draw 2D screenspace isometric grid with black 1px lines
			if (_showGrid)
			{
				DrawIsometricGrid2D();
			}

			base.Draw(gameTime);
		}

		protected override void DrawImGui(GameTime gameTime)
		{
			if (ImGui.Begin("3D Train - Camera Controls"))
			{
				ImGui.Text($"Camera Position: {_camera.Position}");
				ImGui.Text($"Camera Rotation: {_camera.Rotation}");
				ImGui.Separator();

				// Projection Mode Selection
				ImGui.Text("Projection Mode:");
				if (ImGui.RadioButton("Perspective", _camera.CurrentProjectionMode == Camera3D.ProjectionMode.Perspective))
				{
					_camera.CurrentProjectionMode = Camera3D.ProjectionMode.Perspective;
				}
				if (ImGui.RadioButton("Isometric", _camera.CurrentProjectionMode == Camera3D.ProjectionMode.Isometric))
				{
					_camera.CurrentProjectionMode = Camera3D.ProjectionMode.Isometric;
				}
				if (ImGui.RadioButton("Orthographic", _camera.CurrentProjectionMode == Camera3D.ProjectionMode.Orthographic))
				{
					_camera.CurrentProjectionMode = Camera3D.ProjectionMode.Orthographic;
				}

				ImGui.Separator();

				// Field of View control (for perspective mode)
				if (_camera.CurrentProjectionMode == Camera3D.ProjectionMode.Perspective)
				{
					var fovDegrees = MathHelper.ToDegrees(_camera.FieldOfView);
					if (ImGui.SliderFloat("Field of View (deg)", ref fovDegrees, 10f, 120f))
					{
						_camera.FieldOfView = MathHelper.ToRadians(fovDegrees);
					}
				}
				// Zoom control for orthographic mode
				else if (_camera.CurrentProjectionMode == Camera3D.ProjectionMode.Orthographic)
				{
					var zoom = _camera.OrthographicZoom;
					if (ImGui.SliderFloat("Orthographic Zoom", ref zoom, 10f, 500f))
					{
						_camera.OrthographicZoom = zoom;
					}
				}
				// Zoom control for isometric mode
				else if (_camera.CurrentProjectionMode == Camera3D.ProjectionMode.Isometric)
				{
					var zoom = _camera.IsometricZoom;
					if (ImGui.SliderFloat("Isometric Zoom", ref zoom, 10f, 500f))
					{
						_camera.IsometricZoom = zoom;
					}
				}

				ImGui.Separator();

				// Map controls
				ImGui.Text("Map Settings:");
				ImGui.Text($"Map Size: {_mapWidth} x {_mapHeight}");
				ImGui.Text($"Tile Size: {_tileSize}");
				ImGui.Text($"Tile Height (world): {_tileHeightInWorld}");

				if (ImGui.Button("Regenerate Heightmap"))
				{
					GenerateHeightmap();
				}

				ImGui.Checkbox("Show Grid", ref _showGrid);

				ImGui.Separator();
				ImGui.Text("Controls:");
				ImGui.BulletText("IJKL - Move Camera");
				ImGui.BulletText("WASD - Rotate Camera");
				ImGui.BulletText("Space/? - Up/Down");
				ImGui.BulletText("Right Mouse - Mouse Look");
				ImGui.BulletText("Shift - Speed Boost");
			}
			ImGui.End();
		}

		private void DrawIsometricGrid2D()
		{
			SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp);

			var gridColor = Color.Black;

			// Project each grid point from 3D world space to 2D screen space
			var viewProjection = _camera.ViewMatrix * _camera.ProjectionMatrix;
			var viewport = GraphicsDevice.Viewport;

			// Calculate grid bounds in world space
			var minX = -(_mapWidth * _tileSize / 2);
			var maxX = (_mapWidth * _tileSize / 2);
			var minZ = -(_mapHeight * _tileSize / 2);
			var maxZ = (_mapHeight * _tileSize / 2);
			const float gridHeight = 0f;

			// Draw vertical lines (along Z axis)
			for (var x = 0; x <= _mapWidth; x++)
			{
				var worldX = (x * _tileSize) + minX;

				var worldPos1 = new Vector3(worldX, gridHeight, minZ);
				var worldPos2 = new Vector3(worldX, gridHeight, maxZ);

				var screenPos1 = ProjectToScreen(worldPos1, viewProjection, viewport);
				var screenPos2 = ProjectToScreen(worldPos2, viewProjection, viewport);

				if (screenPos1.HasValue && screenPos2.HasValue)
				{
					SpriteBatch.DrawLine(screenPos1.Value, screenPos2.Value, gridColor, 1);
				}
			}

			// Draw horizontal lines (along X axis)
			for (var z = 0; z <= _mapHeight; z++)
			{
				var worldZ = (z * _tileSize) + minZ;

				var worldPos1 = new Vector3(minX, gridHeight, worldZ);
				var worldPos2 = new Vector3(maxX, gridHeight, worldZ);

				var screenPos1 = ProjectToScreen(worldPos1, viewProjection, viewport);
				var screenPos2 = ProjectToScreen(worldPos2, viewProjection, viewport);

				if (screenPos1.HasValue && screenPos2.HasValue)
				{
					SpriteBatch.DrawLine(screenPos1.Value, screenPos2.Value, gridColor, 1);
				}
			}

			SpriteBatch.End();
		}

		private Vector2? ProjectToScreen(Vector3 worldPos, Matrix viewProjection, Viewport viewport)
		{
			// Transform world position to clip space
			var clipPos = Vector3.Transform(worldPos, viewProjection);

			// Check if point is behind camera (w <= 0)
			if (clipPos.Z <= 0 || clipPos.Z >= 1)
				return null;

			// Convert to NDC (normalized device coordinates)
			var ndcX = clipPos.X;
			var ndcY = clipPos.Y;

			// Convert NDC to screen coordinates
			var screenX = (ndcX + 1) * 0.5f * viewport.Width;
			var screenY = (1 - ndcY) * 0.5f * viewport.Height;

			return new Vector2(screenX, screenY);
		}

		private void DrawBillboardTiles()
		{
			// Set up the effect
			_billboardEffect.World = Matrix.Identity;
			_billboardEffect.View = _camera.ViewMatrix;
			_billboardEffect.Projection = _camera.ProjectionMatrix;
			_billboardEffect.Texture = _tileset;

			// Calculate the world size needed for a 64x64 pixel billboard
			var pixelSize = CalculatePixelSizeInWorldSpace();
			var billboardWorldWidth = 64 * pixelSize;
			var billboardWorldHeight = 64 * pixelSize;

			// Build billboard batch
			var billboards = new List<BillboardData>();

			for (var z = 0; z < _mapHeight; z++)
			{
				for (var x = 0; x < _mapWidth; x++)
				{
					// Get the height at this position
					var column = _heightmap[x, z].Items.OrderBy(x => x.Height).ToList();
					var topLand = column.Single(x => x.Type == TileLayerType.Surface).Height;

					// Calculate base 3D position
					var worldX = x * _tileSize - (_mapWidth * _tileSize / 2);
					var worldZ = z * _tileSize - (_mapHeight * _tileSize / 2);

					foreach (var layer in column)
					{
						if (layer.Type == TileLayerType.Surface)
						{
							// Draw a column of tiles from ground (0) to height
							for (var y = 0; y < layer.Height; y++)
							{
								var worldY = y * _tileHeightInWorld;
								var position = new Vector3(worldX, worldY, worldZ);

								// Select tile based on position in column
								int tileIndex;
								if (y == topLand - 1)
								{
									// Top tile - use grass/surface tile (index 33)
									tileIndex = 64;
								}
								else
								{
									// All other tiles in column - use tile index 7
									tileIndex = 7;
								}

								// Clamp tile index
								tileIndex = MathHelper.Clamp(tileIndex, 0, _tilesetColumns * _tilesetRows - 1);

								// Calculate UV coordinates for the selected tile
								var tileX = tileIndex % _tilesetColumns;
								var tileY = tileIndex / _tilesetColumns;

								var sourceRect = new Rectangle(
									tileX * _tileWidth,
									tileY * _tileHeight,
									_tileWidth,
									_tileHeight
								);

								billboards.Add(new BillboardData
								{
									Position = position,
									Size = new Vector2(billboardWorldWidth, billboardWorldHeight),
									SourceRectangle = sourceRect,
									TextureSize = new Vector2(_tileset.Width, _tileset.Height),
									Color = Color.White
								});
							}
						}
						else if (layer.Type == TileLayerType.Station)
						{
							// Render station at specified height
							var worldY = (layer.Height * _tileHeightInWorld);
							var position = new Vector3(worldX, worldY - 0.9f, worldZ); // custom height offset

							// Station tile index
							int tileIndex = 172;

							// Clamp tile index
							tileIndex = MathHelper.Clamp(tileIndex, 0, _tilesetColumns * _tilesetRows - 1);

							// Calculate UV coordinates for the selected tile
							var tileX = tileIndex % _tilesetColumns;
							var tileY = tileIndex / _tilesetColumns;

							var sourceRect = new Rectangle(
								tileX * _tileWidth,
								tileY * _tileHeight,
								_tileWidth,
								_tileHeight
							);

							var customTileHeight = _tileHeight / 2;
							billboards.Add(new BillboardData
							{
								Position = position,
								Size = new Vector2(billboardWorldWidth, billboardWorldHeight),
								SourceRectangle = sourceRect,
								TextureSize = new Vector2(_tileset.Width, _tileset.Height),
								Color = Color.White
							});
						}
						else if (layer.Type == TileLayerType.Tree)
						{
							// Render tree at specified height
							var worldY = layer.Height * _tileHeightInWorld;
							var position = new Vector3(worldX, worldY, worldZ);

							// Tree tile index is 12
							int tileIndex = 12;

							// Clamp tile index
							tileIndex = MathHelper.Clamp(tileIndex, 0, _tilesetColumns * _tilesetRows - 1);

							// Calculate UV coordinates for the selected tile
							var tileX = tileIndex % _tilesetColumns;
							var tileY = tileIndex / _tilesetColumns;

							var sourceRect = new Rectangle(
								tileX * _tileWidth,
								tileY * _tileHeight,
								_tileWidth,
								_tileHeight
							);

							billboards.Add(new BillboardData
							{
								Position = position,
								Size = new Vector2(billboardWorldWidth, billboardWorldHeight),
								SourceRectangle = sourceRect,
								TextureSize = new Vector2(_tileset.Width, _tileset.Height),
								Color = Color.White
							});
						}
						else if (layer.Type == TileLayerType.Track)
						{
							// Render track at specified height
							var worldY = layer.Height * _tileHeightInWorld;
							var position = new Vector3(worldX, worldY, worldZ);

							// Track tile index - you can specify this
							int tileIndex = 0; // TODO: Define track tile index

							// Clamp tile index
							tileIndex = MathHelper.Clamp(tileIndex, 0, _tilesetColumns * _tilesetRows - 1);

							// Calculate UV coordinates for the selected tile
							var tileX = tileIndex % _tilesetColumns;
							var tileY = tileIndex / _tilesetColumns;

							var sourceRect = new Rectangle(
								tileX * _tileWidth,
								tileY * _tileHeight,
								_tileWidth,
								_tileHeight
							);

							billboards.Add(new BillboardData
							{
								Position = position,
								Size = new Vector2(billboardWorldWidth, billboardWorldHeight),
								SourceRectangle = sourceRect,
								TextureSize = new Vector2(_tileset.Width, _tileset.Height),
								Color = Color.White
							});
						}
						else if (layer.Type == TileLayerType.Building)
						{
							// Render building at specified height
							var worldY = layer.Height * _tileHeightInWorld;
							var position = new Vector3(worldX, worldY, worldZ);

							// Building tile index - you can specify this
							int tileIndex = 0; // TODO: Define building tile index

							// Clamp tile index
							tileIndex = MathHelper.Clamp(tileIndex, 0, _tilesetColumns * _tilesetRows - 1);

							// Calculate UV coordinates for the selected tile
							var tileX = tileIndex % _tilesetColumns;
							var tileY = tileIndex / _tilesetColumns;

							var sourceRect = new Rectangle(
								tileX * _tileWidth,
								tileY * _tileHeight,
								_tileWidth,
								_tileHeight
							);

							billboards.Add(new BillboardData
							{
								Position = position,
								Size = new Vector2(billboardWorldWidth, billboardWorldHeight),
								SourceRectangle = sourceRect,
								TextureSize = new Vector2(_tileset.Width, _tileset.Height),
								Color = Color.White
							});
						}
						else if (layer.Type == TileLayerType.Vehicle)
						{
							// Render vehicle at specified height
							var worldY = layer.Height * _tileHeightInWorld;
							var position = new Vector3(worldX, worldY, worldZ);

							// Vehicle tile index - you can specify this
							int tileIndex = 0; // TODO: Define vehicle tile index

							// Clamp tile index
							tileIndex = MathHelper.Clamp(tileIndex, 0, _tilesetColumns * _tilesetRows - 1);

							// Calculate UV coordinates for the selected tile
							var tileX = tileIndex % _tilesetColumns;
							var tileY = tileIndex / _tilesetColumns;

							var sourceRect = new Rectangle(
								tileX * _tileWidth,
								tileY * _tileHeight,
								_tileWidth,
								_tileHeight
							);

							billboards.Add(new BillboardData
							{
								Position = position,
								Size = new Vector2(billboardWorldWidth, billboardWorldHeight),
								SourceRectangle = sourceRect,
								TextureSize = new Vector2(_tileset.Width, _tileset.Height),
								Color = Color.White
							});
						}
					}
				}
			}

			// Check if we're in orthographic or isometric mode
			var isOrthographic = _camera.CurrentProjectionMode == Camera3D.ProjectionMode.Orthographic
				|| _camera.CurrentProjectionMode == Camera3D.ProjectionMode.Isometric;

			// Render all billboards
			_billboardRenderer.Draw(billboards, _billboardEffect, _camera.Position, _camera.Rotation, isOrthographic);
		}

		/// <summary>
		/// Calculates how large a world-space unit needs to be to equal 1 pixel on screen
		/// </summary>
		private float CalculatePixelSizeInWorldSpace()
		{
			float screenHeight = GraphicsDevice.Viewport.Height;

			if (_camera.CurrentProjectionMode == Camera3D.ProjectionMode.Orthographic)
			{
				// In orthographic mode, the zoom value represents the view height in world units
				// So pixels per world unit = screen height / (zoom * 2)
				var worldHeight = _camera.OrthographicZoom;
				return worldHeight / screenHeight;
			}
			else if (_camera.CurrentProjectionMode == Camera3D.ProjectionMode.Isometric)
			{
				// Same for isometric
				var worldHeight = _camera.IsometricZoom;
				return worldHeight / screenHeight;
			}
			else
			{
				// For perspective, this is more complex and depends on distance
				// For now, use a default value
				return 0.1f;
			}
		}
	}
}
