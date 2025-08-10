using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using Shared;
using System;
using System.Collections.Generic;
using TrainGame.Track;
using TrainGame.Train;
using TrainGame.World;

namespace TrainGame
{
	public class TrainGame : ImGuiGame
	{
		private TileMap _tileMap;
		private Renderer _renderer;
		private Camera2D _camera;
		private PathFinding _pathFinding = new PathFinding();

		bool debugDrawing { get; set; }

		private const int TileWidth = 128;
		private const int TileHeight = 64;
		private MouseState _prevMouse;
		private List<Vehicle> _trains = new();
		private int _trainCounter = 1;
		private int _selectedTrain = -1;
		private TileType _selectedGroundType = TileType.Dirt;
		private TrackType _selectedTrackType = TrackType.Straight;
		private TrackDirection _selectedTrackDirection = TrackDirection.EastWest;
		private bool _placingTrack = true;

		public TrainGame()
		{
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
		}

		protected override void Initialize()
		{
			_camera = new Camera2D();
			_tileMap = new TileMap(20, 20); // Example size
			_renderer = new Renderer(SpriteBatch, TileWidth, TileHeight);
			base.Initialize();
		}

		protected override void LoadContent()
		{
			_renderer.LoadContent(Content);
		}

		protected override void Update(GameTime gameTime)
		{
			var keyboardState = Keyboard.GetState();
			var mouseState = Mouse.GetState();

			// Camera movement
			if (keyboardState.IsKeyDown(Keys.Up))
				_camera.Position += new Vector2(0, -_camera.MovementSpeed) * (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (keyboardState.IsKeyDown(Keys.Down))
				_camera.Position += new Vector2(0, _camera.MovementSpeed) * (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (keyboardState.IsKeyDown(Keys.Left))
				_camera.Position += new Vector2(-_camera.MovementSpeed, 0) * (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (keyboardState.IsKeyDown(Keys.Right))
				_camera.Position += new Vector2(_camera.MovementSpeed, 0) * (float)gameTime.ElapsedGameTime.TotalSeconds;

			// Camera zoom
			var previousZoom = _camera.Zoom;
			var mouseScreenPosition = new Vector2(mouseState.X, mouseState.Y);
			var mouseWorldPositionBeforeZoom = Vector2.Transform(mouseScreenPosition, Matrix.Invert(_camera.GetTransformMatrix()));

			var zoomChange = (mouseState.ScrollWheelValue - _prevMouse.ScrollWheelValue) / 1000f;
			_camera.Zoom = MathHelper.Clamp(_camera.Zoom + zoomChange, 0.5f, 2f);

			if (_camera.Zoom != previousZoom)
			{
				var mouseWorldPositionAfterZoom = Vector2.Transform(mouseScreenPosition, Matrix.Invert(_camera.GetTransformMatrix()));
				_camera.Position += mouseWorldPositionBeforeZoom - mouseWorldPositionAfterZoom;
			}

			var mouse = Mouse.GetState();
			if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
			{
				var (mouseTileX, mouseTileY) = _camera.ScreenToIsoTile(mouse.X, mouse.Y, _tileMap.Width, TileWidth, TileHeight);
				if (mouseTileX >= 0 && mouseTileY >= 0 && mouseTileX < _tileMap.Width && mouseTileY < _tileMap.Height)
				{
					if (_placingTrack)
					{
						var tile = _tileMap.Tiles[mouseTileX, mouseTileY];
						tile.Type = TileType.Track;
						tile.TrackType = _selectedTrackType;
						tile.TrackDirection = _selectedTrackDirection;
					}
					else
					{
						var tile = _tileMap.Tiles[mouseTileX, mouseTileY];
						tile.Type = _selectedGroundType;
						tile.TrackType = TrackType.None;
						tile.TrackDirection = TrackDirection.None;
					}

					RebuildTrackGraph();
				}
			}

			if (mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Released)
			{
				// Place train on track
				var (tileX, tileY) = _camera.ScreenToIsoTile(mouse.X, mouse.Y, _tileMap.Width, TileWidth, TileHeight);
				if (tileX >= 0 && tileY >= 0 && tileX < _tileMap.Width && tileY < _tileMap.Height)
				{
					if (_tileMap.Tiles[tileX, tileY].Type == TileType.Track && !_trains.Exists(t => t.TileX == tileX && t.TileY == tileY))
					{
						_trains.Add(new Vehicle(tileX, tileY, $"Train {_trainCounter++}"));
					}
				}
			}

			var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			// Train movement logic (smooth)
			foreach (var train in _trains)
			{
				if (train.State == TrainState.Moving)
				{
					if (!train.IsMovingBetweenTiles)
					{
						if (train.TileX == train.DestX && train.TileY == train.DestY)
						{
							train.State = TrainState.Stopped;
							continue;
						}
						// Move toward destination (simple Manhattan/adjacent step)
						var dx = train.DestX - train.TileX;
						var dy = train.DestY - train.TileY;
						var stepX = dx != 0 ? dx / Math.Abs(dx) : 0;
						var stepY = dy != 0 ? dy / Math.Abs(dy) : 0;
						var moved = false;
						if (stepX != 0 && IsTrack(train.TileX + stepX, train.TileY))
						{
							train.NextTileX = train.TileX + stepX;
							train.NextTileY = train.TileY;
							train.IsMovingBetweenTiles = true;
							train.MoveProgress = 0f;
							moved = true;
						}
						else if (stepY != 0 && IsTrack(train.TileX, train.TileY + stepY))
						{
							train.NextTileX = train.TileX;
							train.NextTileY = train.TileY + stepY;
							train.IsMovingBetweenTiles = true;
							train.MoveProgress = 0f;
							moved = true;
						}

						if (!moved)
						{
							train.State = TrainState.Stopped;
						}
					}
					else
					{
						// Animate movement
						train.MoveProgress += train.Speed * dt;
						if (train.MoveProgress >= 1f)
						{
							train.PosX = train.NextTileX;
							train.PosY = train.NextTileY;
							train.TileX = train.NextTileX;
							train.TileY = train.NextTileY;
							train.IsMovingBetweenTiles = false;
							train.MoveProgress = 0f;
						}
						else
						{
							train.PosX = MathHelper.Lerp(train.TileX, train.NextTileX, train.MoveProgress);
							train.PosY = MathHelper.Lerp(train.TileY, train.NextTileY, train.MoveProgress);
						}
					}
				}
				else
				{
					train.PosX = train.TileX;
					train.PosY = train.TileY;
				}
			}

			_prevMouse = mouse;
			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.SteelBlue);
			SpriteBatch.Begin(transformMatrix: _camera.GetTransformMatrix());
			//SpriteBatch.Begin();

			var mouseState = Mouse.GetState();
			var (highlightedTileX, highlightedTileY) = _camera.ScreenToIsoTile(mouseState.X, mouseState.Y, _tileMap.Width, TileWidth, TileHeight);

			// Draw tilemap
			for (var x = 0; x < _tileMap.Width; x++)
			{
				for (var y = 0; y < _tileMap.Height; y++)
				{
					var tile = _tileMap.Tiles[x, y];
					var (screenX, screenY) = _camera.IsoTileToScreen(x, y, _tileMap.Width, TileWidth, TileHeight);
					_renderer.DrawIsoTile(screenX, screenY, x, y, highlightedTileX == x && highlightedTileY == y, tile);
				}
			}

			// Draw trains
			for (var i = 0; i < _trains.Count; i++)
			{
				var train = _trains[i];
				var (screenX, screenY) = _camera.IsoTileToScreen((int)train.PosX, (int)train.PosY, _tileMap.Width, TileWidth, TileHeight);
				var color = (i == _selectedTrain) ? Color.Yellow : Color.Red;
				_renderer.DrawTrain(screenX, screenY, color);
			}

			if (debugDrawing)
			{
				DrawDebugGrid();
				DrawDebugAxes();
			}

			SpriteBatch.End();
			base.Draw(gameTime);
		}

		private void DrawDebugAxes()
		{
			SpriteBatch.FillRectangle(new RectangleF(-5, -5, 11, 11), Color.Black);
			SpriteBatch.DrawLine(Vector2.Zero, Vector2.UnitX * 16, Color.Red, 3);
			SpriteBatch.DrawLine(Vector2.Zero, Vector2.UnitY * 16, Color.Green, 3);

			SpriteBatch.DrawLine(Vector2.Zero, Vector2.UnitX * Graphics.PreferredBackBufferWidth, Color.Red, 1);
			SpriteBatch.DrawLine(Vector2.Zero, Vector2.UnitY * Graphics.PreferredBackBufferHeight, Color.Green, 1);
		}

		private void DrawDebugGrid()
		{
			const int gridSize = 32;
			for (var y = 0; y < Graphics.PreferredBackBufferHeight / gridSize; y++)
			{
				SpriteBatch.DrawLine(new Vector2(0, y * gridSize), new Vector2(Graphics.PreferredBackBufferWidth, y * gridSize), Color.CornflowerBlue, 1);
			}

			for (var x = 0; x < Graphics.PreferredBackBufferWidth / 32; x++)
			{
				SpriteBatch.DrawLine(new Vector2(x * gridSize, 0), new Vector2(x * gridSize, Graphics.PreferredBackBufferHeight), Color.CornflowerBlue, 1);
			}
		}

		protected override void DrawImGui(GameTime gameTime)
		{
			ImGui.Begin("Debug");
			if (ImGui.Button("Debug Drawing"))
			{
				debugDrawing = !debugDrawing;
			}
			ImGui.End();

			ImGui.Begin("Mouse Position");

			var mouseState = Mouse.GetState();
			ImGui.Text($"Screen Position: {mouseState.X}, {mouseState.Y}");

			var (tileX, tileY) = _camera.ScreenToIsoTile(mouseState.X, mouseState.Y, _tileMap.Width, TileWidth, TileHeight);
			ImGui.Text($"Game Position: {tileX}, {tileY}");

			ImGui.Text($"Camera Position:{_camera.Position}");
			ImGui.Text($"Camera Zoom:{_camera.Zoom}");
			ImGui.Text($"Camera Rotation:{_camera.Rotation}");

			ImGui.End();

			ImGui.Begin("Trains");
			for (var i = 0; i < _trains.Count; i++)
			{
				var train = _trains[i];
				var selected = i == _selectedTrain;
				if (ImGui.Selectable($"{train.Name} ({train.State})", selected))
				{
					_selectedTrain = i;
				}
			}

			if (_selectedTrain >= 0 && _selectedTrain < _trains.Count)
			{
				var train = _trains[_selectedTrain];
				ImGui.Begin($"Train Controls: {train.Name}");
				if (ImGui.Button(train.State == TrainState.Stopped ? "Start" : "Stop"))
				{
					train.State = train.State == TrainState.Stopped ? TrainState.Moving : TrainState.Stopped;
				}

				// Fix for CS0206: Use a local variable to modify the Speed property
				var speed = train.Speed;
				ImGui.SliderFloat("Max Speed", ref speed, 0.5f, 10f);
				train.Speed = speed;

				ImGui.Text($"Position: {train.TileX}, {train.TileY}");
				ImGui.Text($"Destination: {train.DestX}, {train.DestY}");

				// Set destination by clicking a track tile
				ImGui.Text("Set Destination: Click a track tile on the map");
				if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
				{
					var mouse = Mouse.GetState();
					var (isoTileX, isoTileY) = _camera.ScreenToIsoTile(mouse.X, mouse.Y, _tileMap.Width, TileWidth, TileHeight);
					if (isoTileX >= 0 && isoTileY >= 0 && isoTileX < _tileMap.Width && isoTileY < _tileMap.Height)
					{
						if (_tileMap.Tiles[isoTileX, isoTileY].Type == TileType.Track)
						{
							train.DestX = isoTileX;
							train.DestY = isoTileY;
						}
					}
				}

				ImGui.End();
			}

			ImGui.End();
			ImGui.Begin("Tile Placement");
			if (ImGui.RadioButton("Place Track", _placingTrack))
			{
				_placingTrack = true;
			}

			ImGui.SameLine();
			if (ImGui.RadioButton("Place Ground", !_placingTrack))
			{
				_placingTrack = false;
			}

			if (_placingTrack)
			{
				ImGui.Text("Track Type:");
				foreach (TrackType type in Enum.GetValues(typeof(TrackType)))
				{
					var label = type.ToString();
					if (ImGui.RadioButton(label, type == _selectedTrackType))
					{
						_selectedTrackType = type;
					}

					ImGui.SameLine();
				}

				ImGui.Text("Direction:");
				foreach (TrackDirection dir in Enum.GetValues(typeof(TrackDirection)))
				{
					var label = dir.ToString();
					if (ImGui.RadioButton(label, dir == _selectedTrackDirection))
					{
						_selectedTrackDirection = dir;
					}

					ImGui.SameLine();
				}
			}
			else
			{
				ImGui.Text("Ground Type:");
				foreach (TileType type in Enum.GetValues(typeof(TileType)))
				{
					if (type == TileType.Track)
					{
						continue; // Skip track type
					}

					var label = type.ToString();
					if (ImGui.RadioButton(label, type == _selectedGroundType))
					{
						_selectedGroundType = type;
					}

					ImGui.SameLine();
				}
			}

			ImGui.End();
		}

		private bool IsTrack(int x, int y)
		{
			return x >= 0 && y >= 0 && x < _tileMap.Width && y < _tileMap.Height && _tileMap.Tiles[x, y].Type == TileType.Track;
		}

		private void RebuildTrackGraph()
		{
			_pathFinding.RebuildTrackGraph(_tileMap);
		}
	}
}
