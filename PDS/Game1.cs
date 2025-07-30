using DelaunatorSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace PDS
{
	public enum MaterialType
	{
		Air,
		Grass,
		Rock,
		Sand,
		Gold,
		Iron
	}

	public struct MaterialPoint
	{
		public Vector2 Position;
		public MaterialType Material;

		public MaterialPoint(Vector2 position, MaterialType material)
		{
			Position = position;
			Material = material;
		}
	}

	public class Game1 : Game
	{
		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;

		private List<MaterialPoint> _points;
		private DelaunatorSharp.Delaunator delaunator;

		private int _screenWidth = 1920;
		private int _screenHeight = 1080;

		// Player fields
		private Vector2 _playerPosition;
		private float _playerSpeed = 200f; // pixels per second
		private int _playerSize = 16;

		private MouseState _previousMouseState;

		public Game1()
		{
			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
			Window.AllowUserResizing = true;
			_graphics.PreferredBackBufferWidth = _screenWidth;
			_graphics.PreferredBackBufferHeight = _screenHeight;
		}

		protected override void Initialize()
		{
			// Assign random material for demonstration
			var random = new Random();
			_points = PoissonDiscSampler.GeneratePoints(20, _screenWidth, _screenHeight, 30)
				.ConvertAll(p => new MaterialPoint(p, (MaterialType)random.Next(Enum.GetValues<MaterialType>().Length)));

			var dPoints = _points.Select(x => new DelaunatorSharp.Point((int)x.Position.X, (int)x.Position.Y) as IPoint);
			delaunator = new Delaunator([.. dPoints]);

			_playerPosition = new Vector2(_screenWidth / 2, _screenHeight / 2);

			_previousMouseState = Mouse.GetState();

			base.Initialize();
		}

		protected override void LoadContent()
		{
			_spriteBatch = new SpriteBatch(GraphicsDevice);
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			{
				Exit();
			}

			// Player movement
			var state = Keyboard.GetState();
			var movement = Vector2.Zero;

			if (state.IsKeyDown(Keys.Left) || state.IsKeyDown(Keys.A))
			{
				movement.X -= 1;
			}

			if (state.IsKeyDown(Keys.Right) || state.IsKeyDown(Keys.D))
			{
				movement.X += 1;
			}

			if (state.IsKeyDown(Keys.Up) || state.IsKeyDown(Keys.W))
			{
				movement.Y -= 1;
			}

			if (state.IsKeyDown(Keys.Down) || state.IsKeyDown(Keys.S))
			{
				movement.Y += 1;
			}

			if (movement != Vector2.Zero)
			{
				movement.Normalize();
				_playerPosition += movement * _playerSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;

				_playerPosition.X = Math.Clamp(_playerPosition.X, 0, _screenWidth - _playerSize);
				_playerPosition.Y = Math.Clamp(_playerPosition.Y, 0, _screenHeight - _playerSize);
			}

			// Mouse click detection
			var mouseState = Mouse.GetState();
			if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
			{
				const int distanceToClick = 20;
				// Check for nearby point
				//var pointToRemove = _points.FirstOrDefault(p => Vector2.Distance(_playerPosition, p.Position) <= distanceToClick);
				var pointToRemove = _points.FirstOrDefault(p => Vector2.Distance(mouseState.Position.ToVector2(), p.Position) <= distanceToClick);

				if (!pointToRemove.Equals(default(MaterialPoint)))
				{
					_points.Remove(pointToRemove);

					// Recompute Delaunay
					var dPoints = _points.Select(x => new DelaunatorSharp.Point((int)x.Position.X, (int)x.Position.Y) as IPoint);
					delaunator = new Delaunator([.. dPoints]);
				}
			}
			else if (mouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
			{
				// Add a new point with a random material
				var random = new Random();
				_points.Add(new MaterialPoint(mouseState.Position.ToVector2(), (MaterialType)random.Next(Enum.GetValues<MaterialType>().Length)));

				// Recompute Delaunay
				var dPoints = _points.Select(x => new DelaunatorSharp.Point((int)x.Position.X, (int)x.Position.Y) as IPoint);
				delaunator = new Delaunator([.. dPoints]);
			}
			_previousMouseState = mouseState;

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.SteelBlue);

			// --- 1. Collect all triangles for all Voronoi cells ---
			var vertices = new List<VertexPositionColor>();
			var indices = new List<short>();
			short vertexOffset = 0;

			delaunator.ForEachVoronoiCell(cell =>
			{
				var point = _points[cell.Index];
				var color = MaterialTypeToColor(point.Material);

				var polyPoints = cell.Points.Select(x => new Vector2((float)x.X, (float)x.Y)).ToArray();
				if (polyPoints.Length < 3)
					return;

				// Add vertices for this cell
				foreach (var v in polyPoints)
					vertices.Add(new VertexPositionColor(new Vector3(v, 0), color));

				// Triangulate as a fan (convex)
				for (int i = 1; i < polyPoints.Length - 1; i++)
				{
					indices.Add((short)vertexOffset);
					indices.Add((short)(vertexOffset + i));
					indices.Add((short)(vertexOffset + i + 1));
				}
				vertexOffset += (short)polyPoints.Length;
			});

			// --- 2. Draw all triangles in one call ---
			if (vertices.Count >= 3)
			{
				var basicEffect = new BasicEffect(GraphicsDevice)
				{
					VertexColorEnabled = true,
					Projection = Matrix.CreateOrthographicOffCenter(0, _screenWidth, _screenHeight, 0, 0, 1)
				};

				foreach (var pass in basicEffect.CurrentTechnique.Passes)
				{
					pass.Apply();
					GraphicsDevice.DrawUserIndexedPrimitives(
						PrimitiveType.TriangleList,
						vertices.ToArray(),
						0,
						vertices.Count,
						indices.ToArray(),
						0,
						indices.Count / 3
					);
				}
			}

			// --- 3. Draw overlays (edges, points, player) as before ---
			_spriteBatch.Begin();

			// Draw Delaunay edges
			//delaunator.ForEachTriangleEdge(edge =>
			//{
			//	_spriteBatch.DrawLine(
			//		new Vector2((float)edge.P.X, (float)edge.P.Y),
			//		new Vector2((float)edge.Q.X, (float)edge.Q.Y),
			//		Color.Red,
			//		1);
			//});

			// Draw Voronoi outlines
			delaunator.ForEachVoronoiCell((cell) =>
			{
				var point = _points[cell.Index];
				var color = MaterialTypeToColor(point.Material);

				var polyPoints = cell.Points.Select(x => new Vector2((float)x.X, (float)x.Y)).ToArray();
				var polygon = new MonoGame.Extended.Shapes.Polygon([.. polyPoints]);
				_spriteBatch.DrawPolygon(Vector2.Zero, polygon, color, 2);
			});

			// Draw PDS points with color based on material
			foreach (var point in _points)
			{
				var color = MaterialTypeToColor(point.Material);
				var darker = new Color((int)(color.R * 0.8), (int)(color.G * 0.8), (int)(color.B * 0.8), 255);
				_spriteBatch.FillRectangle(new Rectangle((int)point.Position.X - 2, (int)point.Position.Y - 2, 4, 4), darker);
			}

			// Draw player
			//_spriteBatch.FillRectangle(new Rectangle((int)_playerPosition.X, (int)_playerPosition.Y, _playerSize, _playerSize), Color.LimeGreen);

			_spriteBatch.End();

			base.Draw(gameTime);
		}

		static Color MaterialTypeToColor(MaterialType materialType)
		{
			return materialType switch
			{
				MaterialType.Air => Color.LightGray,
				MaterialType.Grass => Color.Green,
				MaterialType.Rock => Color.Gray,
				MaterialType.Sand => Color.Yellow,
				MaterialType.Gold => Color.Gold,
				MaterialType.Iron => Color.Orange,
				_ => Color.Black
			};
		}
	}
}
