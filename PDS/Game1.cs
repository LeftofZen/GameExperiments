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
		Iron,
		Coal
	}

	public class MaterialPoint
	{
		public Vector2 Position { get; set; }
		public MaterialType Material { get; set; }

		public MaterialPoint(Vector2 position, MaterialType material)
		{
			Position = position;
			Material = material;
		}
	}

	public class Game1 : Game
	{
		private readonly GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;

		private List<MaterialPoint> _points;
		private Delaunator delaunator;

		private readonly int _screenWidth = 1920;
		private readonly int _screenHeight = 1080;

		// Player fields
		private Vector2 _playerPosition;
		private Vector2 _playerVelocity; // <-- Add this
		private readonly float _playerSpeed = 200f; // pixels per second
		private readonly int _playerSize = 16;
		private readonly float _gravity = 800f; // pixels per second squared
		private bool _isOnGround = false; // <-- Ground check
		private const int PlayerCollisionMargin = 2;

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
				.ConvertAll(p =>
				{
					var material = MaterialType.Air;
					if (p.Y > 500)
					{
						// 'underground'
						material = (MaterialType)random.Next(Enum.GetValues<MaterialType>().Length - 1) + 1;
					}
					return new MaterialPoint(p, material);
				});

			var dPoints = _points.Select(x => new DelaunatorSharp.Point((int)x.Position.X, (int)x.Position.Y) as IPoint);
			delaunator = new Delaunator([.. dPoints]);

			_playerPosition = new Vector2(_screenWidth / 2, _screenHeight / 4);
			_playerVelocity = Vector2.Zero; // <-- Initialize velocity

			_previousMouseState = Mouse.GetState();

			//_points[0].Position = new Vector2(100, 100);
			//_points[0].Material = MaterialType.Air;

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

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			// Player movement (horizontal only)
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

			// Apply horizontal movement to velocity
			_playerVelocity.X = movement.X * _playerSpeed;

			// Apply gravity to vertical velocity
			_playerVelocity.Y += _gravity * dt;

			// Jump logic
			if (_isOnGround && state.IsKeyDown(Keys.Space))
			{
				_playerVelocity.Y = -200f; // Jump impulse (tweak as needed)
				_isOnGround = false;
			}

			Vector2 newPosition = _playerPosition;

			// --- Horizontal movement and collision ---
			newPosition.X += _playerVelocity.X * dt;
			Rectangle playerRectX = new Rectangle(
				(int)newPosition.X + PlayerCollisionMargin,
				(int)_playerPosition.Y + PlayerCollisionMargin,
				_playerSize - PlayerCollisionMargin * 2,
				_playerSize - PlayerCollisionMargin * 2);

			bool collidedX = false;
			delaunator.ForEachVoronoiCell(cell =>
			{
				var matPoint = _points[cell.Index];
				if (matPoint.Material == MaterialType.Air)
					return;
				var polyPoints = cell.Points.Select(x => new Vector2((float)x.X, (float)x.Y)).ToArray();
				if (polyPoints.Length < 3)
					return;
				if (PolygonIntersectsRectangle(polyPoints, playerRectX))
					collidedX = true;
			});
			if (collidedX)
			{
				newPosition.X = _playerPosition.X; // Cancel horizontal movement
				_playerVelocity.X = 0;
			}

			// --- Vertical movement and collision ---
			newPosition.Y += _playerVelocity.Y * dt;
			Rectangle playerRectY = new Rectangle(
				(int)newPosition.X + PlayerCollisionMargin,
				(int)newPosition.Y + PlayerCollisionMargin,
				_playerSize - PlayerCollisionMargin * 2,
				_playerSize - PlayerCollisionMargin * 2);

			bool collidedY = false;
			bool onGround = false;
			delaunator.ForEachVoronoiCell(cell =>
			{
				var matPoint = _points[cell.Index];
				if (matPoint.Material == MaterialType.Air)
					return;
				var polyPoints = cell.Points.Select(x => new Vector2((float)x.X, (float)x.Y)).ToArray();
				if (polyPoints.Length < 3)
					return;
				if (PolygonIntersectsRectangle(polyPoints, playerRectY))
				{
					collidedY = true;
					// Check if the player is landing on top of the cell
					if (_playerVelocity.Y >= 0)
						onGround = true;
				}
			});
			if (collidedY)
			{
				newPosition.Y = _playerPosition.Y; // Cancel vertical movement
				_playerVelocity.Y = 0;
			}

			// Clamp player to screen bounds (ground collision)
			if (newPosition.Y > _screenHeight - _playerSize)
			{
				newPosition.Y = _screenHeight - _playerSize;
				_playerVelocity.Y = 0;
				onGround = true;
			}
			if (newPosition.X < 0)
				newPosition.X = 0;
			if (newPosition.X > _screenWidth - _playerSize)
				newPosition.X = _screenWidth - _playerSize;

			_playerPosition = newPosition;
			_isOnGround = onGround || (_playerPosition.Y >= _screenHeight - _playerSize);

			// Mouse click detection (unchanged)
			var mouseState = Mouse.GetState();
			if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
			{
				Vector2 mousePos = mouseState.Position.ToVector2();
				int cellToRemove = -1;

				delaunator.ForEachVoronoiCell(cell =>
				{
					var polyPoints = cell.Points.Select(x => new Vector2((float)x.X, (float)x.Y)).ToArray();
					if (polyPoints.Length >= 3 && PointInPolygon(mousePos, polyPoints))
					{
						cellToRemove = cell.Index;
					}
				});

				if (cellToRemove >= 0 && cellToRemove < _points.Count)
				{
					//_points.RemoveAt(cellToRemove);
					_points[cellToRemove].Material = MaterialType.Air; // Set to air instead of removing

					var dPoints = _points.Select(x => new DelaunatorSharp.Point((int)x.Position.X, (int)x.Position.Y) as IPoint);
					delaunator = new Delaunator([.. dPoints]);
				}
			}
			else if (mouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
			{
				var random = new Random();
				_points.Add(new MaterialPoint(mouseState.Position.ToVector2(), (MaterialType)random.Next(Enum.GetValues<MaterialType>().Length)));

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
				_spriteBatch.DrawPolygon(Vector2.Zero, polygon, new Color(0f, 0f, 0f, 0.1f), 1f);
			});

			// Draw PDS points with color based on material
			foreach (var point in _points)
			{
				var color = MaterialTypeToColor(point.Material);
				var darker = new Color((int)(color.R * 0.8), (int)(color.G * 0.8), (int)(color.B * 0.8), 255);
				_spriteBatch.FillRectangle(new Rectangle((int)point.Position.X - 2, (int)point.Position.Y - 2, 4, 4), darker);
			}

			// Draw player
			_spriteBatch.FillRectangle(new Rectangle((int)_playerPosition.X, (int)_playerPosition.Y, _playerSize, _playerSize), Color.Red);
			_spriteBatch.DrawRectangle(new Rectangle((int)_playerPosition.X, (int)_playerPosition.Y, _playerSize, _playerSize), Color.Black);

			_spriteBatch.End();

			base.Draw(gameTime);
		}

		static Color MaterialTypeToColor(MaterialType materialType)
		{
			return materialType switch
			{
				MaterialType.Air => Color.LightBlue,
				MaterialType.Grass => Color.Green,
				MaterialType.Rock => Color.DarkGray,
				MaterialType.Sand => Color.Yellow,
				MaterialType.Gold => Color.Gold,
				MaterialType.Iron => Color.Gray,
				MaterialType.Coal => Color.Black,
				_ => Color.Red
			};
		}

		//private static bool PolygonIntersectsRectangle(Vector2[] polygon, Rectangle rect)
		//{
		//	if (polygon.Length < 3)
		//		return false; // Not a polygon
		//					  // Check if any vertex of the polygon is inside the rectangle
		//	foreach (var vertex in polygon)
		//	{
		//		if (rect.Contains(vertex))
		//			return true;
		//	}
		//	// Check if any edge of the polygon intersects with the rectangle edges
		//	for (int i = 0; i < polygon.Length; i++)
		//	{
		//		Vector2 p1 = polygon[i];
		//		Vector2 p2 = polygon[(i + 1) % polygon.Length];
		//		if (LineIntersectsRectangle(p1, p2, rect))
		//			return true;
		//	}
		//	return false; // No intersection found
		//}

		// old
		private static bool PolygonIntersectsRectangle(Vector2[] polygon, Rectangle rect)
		{
			// Convert rectangle to polygon
			Vector2[] rectVerts = new Vector2[]
			{
				new Vector2(rect.Left, rect.Top),
				new Vector2(rect.Right, rect.Top),
				new Vector2(rect.Right, rect.Bottom),
				new Vector2(rect.Left, rect.Bottom)
			};

			// Check for overlap using SAT (Separating Axis Theorem)
			return PolygonIntersectsPolygon(polygon, rectVerts);
		}

		private static bool PolygonIntersectsPolygon(Vector2[] polyA, Vector2[] polyB)
		{
			// Check all edges of both polygons
			foreach (var polygon in new[] { polyA, polyB })
			{
				for (int i = 0; i < polygon.Length; i++)
				{
					// Get the current edge
					Vector2 p1 = polygon[i];
					Vector2 p2 = polygon[(i + 1) % polygon.Length];

					// Get the axis perpendicular to the edge
					Vector2 axis = new Vector2(-(p2.Y - p1.Y), p2.X - p1.X);
					axis.Normalize();

					// Project both polygons onto the axis
					float minA, maxA, minB, maxB;
					ProjectPolygon(axis, polyA, out minA, out maxA);
					ProjectPolygon(axis, polyB, out minB, out maxB);

					// Check for overlap
					if (maxA < minB || maxB < minA)
						return false; // No collision
				}
			}
			return true; // Collision on all axes
		}

		private static void ProjectPolygon(Vector2 axis, Vector2[] polygon, out float min, out float max)
		{
			float dot = Vector2.Dot(axis, polygon[0]);
			min = max = dot;
			for (int i = 1; i < polygon.Length; i++)
			{
				dot = Vector2.Dot(axis, polygon[i]);
				if (dot < min) min = dot;
				if (dot > max) max = dot;
			}
		}

		private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
		{
			bool inside = false;
			int j = polygon.Length - 1;
			for (int i = 0; i < polygon.Length; j = i++)
			{
				if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
					(point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y + float.Epsilon) + polygon[i].X))
				{
					inside = !inside;
				}
			}
			return inside;
		}
	}
}
