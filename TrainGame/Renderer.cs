using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using SharedContent;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainGame.Track;
using TrainGame.World;

namespace TrainGame
{
	public class Renderer
	{
		private SpriteFont _font;
		private SpriteBatch SpriteBatch;
		private int TileWidth { get; init; }
		private int TileHeight { get; init; }

		public Renderer(SpriteBatch spriteBatch, int tileWidth, int tileHeight)
		{
			SpriteBatch = spriteBatch;
			TileWidth = tileWidth;
			TileHeight = tileHeight;
		}

		public void LoadContent(ContentManager content)
		{
			_font = content.Load<SpriteFont>($"Fonts\\{FontNames._ari_w9500}");
		}

		public static void FillPolygon(SpriteBatch spriteBatch, Vector2[] vertices, Color color)
		{
			// 1. Create a 1x1 pixel texture if it doesn't exist.
			// This is the core workaround to draw a solid color with SpriteBatch.
			//if (_pixelTexture == null)
			//{
			//	_pixelTexture = new Texture2D(graphicsDevice, 1, 1);
			//	_pixelTexture.SetData(new[] { Color.White });
			//}

			// 2. Find the min and max Y coordinates to define the scanline range.
			var minY = vertices[0].Y;
			var maxY = vertices[0].Y;
			foreach (var vertex in vertices)
			{
				if (vertex.Y < minY)
				{
					minY = vertex.Y;
				}

				if (vertex.Y > maxY)
				{
					maxY = vertex.Y;
				}
			}

			// 3. Iterate through each scanline (horizontal line) from top to bottom.
			// We use Math.Ceiling and Math.Floor to ensure we cover all pixels.
			for (var y = (int)Math.Ceiling(minY); y <= (int)Math.Floor(maxY); y++)
			{
				var intersections = new List<float>();

				// 4. Find all intersections of the current scanline with the polygon's edges.
				for (var i = 0; i < vertices.Length; i++)
				{
					var p1 = vertices[i];
					var p2 = vertices[(i + 1) % vertices.Length]; // Connect to the next vertex, wrapping around

					// Check if the scanline intersects the edge.
					if (p1.Y <= y && p2.Y > y || p2.Y <= y && p1.Y > y)
					{
						// Calculate the X-coordinate of the intersection point.
						var xIntersection = ((y - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y)) + p1.X;
						intersections.Add(xIntersection);
					}
				}

				// 5. Sort the intersections by their X-coordinate.
				intersections.Sort();

				// 6. Draw horizontal lines between each pair of intersection points.
				// This is what "fills" the polygon.
				for (var i = 0; i < intersections.Count - 1; i += 2)
				{
					var x1 = intersections[i];
					var x2 = intersections[i + 1];

					// The width of the line to draw.
					var width = (int)Math.Floor(x2) - (int)Math.Ceiling(x1);

					// Draw a rectangle using our single-pixel texture.
					// The rectangle represents a filled horizontal line.
					if (width > 0)
					{
						spriteBatch.FillRectangle(new Rectangle((int)Math.Ceiling(x1), y, width, 1), color);
						//spriteBatch.Draw(_pixelTexture, );
					}
				}
			}
		}

		public void DrawIsoTile(int screenX, int screenY, int tileX, int tileY, bool isHighlight, Tile tile = null)
		{
			var height = (tile.Layers.SingleOrDefault(x => x is TerrainLayer) as TerrainLayer).Height;
			foreach (var layer in tile.Layers)
			{
				switch (layer.Kind)
				{
					case LayerKind.Terrain:
					{
						var terrain = layer as TerrainLayer;
						var color = terrain.Terrain switch
						{
							TerrainType.Grass => Color.DarkOliveGreen,
							TerrainType.Grass2 => Color.DarkKhaki,
							TerrainType.Sand => Color.Moccasin,
							TerrainType.Dirt => Color.Sienna,
							TerrainType.Rock => Color.Gray,
							TerrainType.Ice => Color.PaleTurquoise,
							TerrainType.Gravel => Color.DarkGray,
							_ => Color.Magenta
						};

						// Draw a diamond shape for the isometric tile
						var poly = new Vector2[]
						{
							new(screenX, screenY + (TileHeight / 2)),
							new(screenX + (TileWidth / 2), screenY),
							new(screenX + TileWidth, screenY + (TileHeight / 2)),
							new(screenX + (TileWidth / 2), screenY + TileHeight)
						};

						// draw left cliff edge
						var leftEdge = new Vector2[]
						{
							poly[0],
							poly[0] - new Vector2(0, height),
							poly[3] - new Vector2(0, height),
							poly[3],
						};
						FillPolygon(SpriteBatch, leftEdge, Color.DarkSlateGray);

						// draw right cliff edge
						var rightEdge = new Vector2[]
						{
							poly[2],
							poly[3],
							poly[3] - new Vector2(0, height),
							poly[2] - new Vector2(0, height)
						};
						FillPolygon(SpriteBatch, rightEdge, Color.SlateGray);

						// adjust for tile height
						for (var i = 0; i < poly.Length; ++i)
						{
							poly[i].Y -= height;
						}
						FillPolygon(SpriteBatch, poly, color);

						// Draw outline
						var outlineColor = isHighlight ? Color.Yellow : Color.Black;
						SpriteBatch.DrawPolygon(Vector2.Zero, poly, outlineColor, 1f);

						break;
					}

					case LayerKind.Track:
					{
						if (layer is TrackLayer track && track.TrackType != TrackType.None)
						{
							DrawTrackOverlay(screenX, screenY - height, track.TrackType, track.TrackDirection);
						}

						break;
					}
					case LayerKind.Prop:
						break;
				}
			}

			// draw tile debug info
			SpriteBatch.DrawString(_font, $"({tileX},{tileY})", new Vector2(screenX + (TileWidth / 4), screenY + (TileHeight / 4)), Color.Black, 0f, Vector2.Zero, 0.25f, SpriteEffects.None, 0f);
		}

		public void DrawTrackOverlay(int screenX, int screenY, TrackType trackType, TrackDirection dir)
		{
			var overlayColor = Color.DarkSlateGray;
			// center of tile
			var cx = screenX + (TileWidth / 2);
			var cy = screenY + (TileHeight / 2);

			// diamond corner points
			var top = new Vector2(screenX + (TileWidth / 2), screenY);
			var right = new Vector2(screenX + TileWidth, screenY + (TileHeight / 2));
			var bottom = new Vector2(screenX + (TileWidth / 2), screenY + TileHeight);
			var left = new Vector2(screenX, screenY + (TileHeight / 2));

			// connector lerp factor to move endpoints slightly inside the tile
			const float inward = 0.85f;
			Vector2 Connector(Vector2 corner) => (new Vector2(cx, cy) * (1 - inward)) + (corner * inward);

			var topP = Connector(top);
			var rightP = Connector(right);
			var bottomP = Connector(bottom);
			var leftP = Connector(left);

			void DrawConn(Vector2 a, Vector2 b)
			{
				DrawLine(new Point((int)a.X, (int)a.Y), new Point((int)b.X, (int)b.Y), overlayColor, Math.Max(2, TileWidth / 20));
			}

			switch (trackType)
			{
				case TrackType.Straight:
					if (dir == TrackDirection.EastWest)
					{
						DrawConn(leftP, rightP);
					}
					else if (dir == TrackDirection.NorthSouth)
					{
						DrawConn(topP, bottomP);
					}
					break;
				case TrackType.Diagonal:
					if (dir == TrackDirection.NorthEast)
					{
						DrawConn(topP, rightP);
					}
					else if (dir == TrackDirection.SouthWest)
					{
						DrawConn(bottomP, leftP);
					}
					else if (dir == TrackDirection.NorthWest)
					{
						DrawConn(topP, leftP);
					}
					else if (dir == TrackDirection.SouthEast)
					{
						DrawConn(bottomP, rightP);
					}
					break;
				case TrackType.CurveSmall:
					// map curve directions to connectors where possible
					if (dir == TrackDirection.NE_to_E)
					{
						DrawCurveBetween(topP, rightP, overlayColor, Math.Max(2, TileWidth / 20));
					}
					else if (dir == TrackDirection.E_to_SE)
					{
						DrawCurveBetween(rightP, bottomP, overlayColor, Math.Max(2, TileWidth / 20));
					}
					else if (dir == TrackDirection.SE_to_S || dir == TrackDirection.SE_to_S)
					{
						DrawCurveBetween(bottomP, rightP, overlayColor, Math.Max(2, TileWidth / 20));
					}
					else
					{
						DrawCurve(cx, cy, (int)(TileWidth * 0.25f), dir, overlayColor, Math.Max(2, TileWidth / 20));
					}

					break;
				case TrackType.CurveLarge:
					DrawCurve(cx, cy, (int)(TileWidth * 0.33f), dir, overlayColor, Math.Max(2, TileWidth / 20));
					break;
				case TrackType.CurveXL_Half:
					DrawCurve(cx, cy, (int)(TileWidth * 0.45f), dir, overlayColor, Math.Max(2, TileWidth / 20), 0.5f);
					break;
				case TrackType.Intersection:
					DrawConn(leftP, rightP);
					DrawConn(topP, bottomP);
					break;
			}
		}

		private void DrawCurveBetween(Vector2 a, Vector2 b, Color color, int thickness)
		{
			// Simple poly arc between two connector points using center as pivot
			var cx = (int)((a.X + b.X) / 2);
			var cy = (int)((a.Y + b.Y) / 2);
			// approximate by interpolating points along quadratic Bezier with control at center
			var center = new Vector2(cx, cy);
			var segments = 8;
			for (var i = 0; i < segments; i++)
			{
				var t0 = i / (float)segments;
				var t1 = (i + 1) / (float)segments;
				var p0 = QuadraticBezier(a, center, b, t0);
				var p1 = QuadraticBezier(a, center, b, t1);
				DrawLine(new Point((int)p0.X, (int)p0.Y), new Point((int)p1.X, (int)p1.Y), color, thickness);
			}
		}

		private static Vector2 QuadraticBezier(Vector2 a, Vector2 c, Vector2 b, float t)
		{
			var u = 1 - t;
			return (u * u * a) + (2 * u * t * c) + (t * t * b);
		}

		public void DrawLine(Point p1, Point p2, Color color, int thickness = 2)
		{
			var dx = p2.X - p1.X;
			var dy = p2.Y - p1.Y;
			var len = (int)Math.Sqrt((dx * dx) + (dy * dy));
			if (len > 0)
			{
				SpriteBatch.DrawLine(p1.X, p1.Y, p2.X, p2.Y, color, thickness, 0);
			}
		}

		public void DrawCurve(int cx, int cy, int radius, TrackDirection dir, Color color, int thickness, float arcFraction = 1f)
		{
			// Draw a simple arc for curve visualization, radius clamped to tile
			radius = Math.Min(radius, (TileWidth / 2) - 2);
			float startAngle = 0f, endAngle = 0f;
			switch (dir)
			{
				case TrackDirection.NE_to_E:
					startAngle = -MathF.PI / 2; endAngle = 0f; break;
				case TrackDirection.E_to_SE:
					startAngle = 0f; endAngle = MathF.PI / 2; break;
				case TrackDirection.SE_to_S:
					startAngle = MathF.PI / 2; endAngle = MathF.PI; break;
				case TrackDirection.S_to_SW:
					startAngle = MathF.PI; endAngle = 1.5f * MathF.PI; break;
				case TrackDirection.SW_to_W:
					startAngle = 1.5f * MathF.PI; endAngle = 2f * MathF.PI; break;
				case TrackDirection.W_to_NW:
					startAngle = MathF.PI; endAngle = 1.5f * MathF.PI; break;
				case TrackDirection.NW_to_N:
					startAngle = MathF.PI; endAngle = 1.5f * MathF.PI; break;
				case TrackDirection.N_to_NE:
					startAngle = -MathF.PI / 2; endAngle = 0f; break;
				default:
					startAngle = 0f; endAngle = MathF.PI / 2; break;
			}

			var segments = 12;
			var arc = (endAngle - startAngle) * arcFraction;
			for (var i = 0; i < segments; i++)
			{
				var t0 = startAngle + (arc * (i / (float)segments));
				var t1 = startAngle + (arc * ((i + 1) / (float)segments));
				var p0 = new Point(cx + (int)(MathF.Cos(t0) * radius), cy + (int)(MathF.Sin(t0) * radius));
				var p1 = new Point(cx + (int)(MathF.Cos(t1) * radius), cy + (int)(MathF.Sin(t1) * radius));
				DrawLine(p0, p1, color, thickness);
			}
		}

		public void DrawTrain(int screenX, int screenY, Color color)
		{
			// Draw a simple circle or square for the train
			var size = TileHeight / 2;
			SpriteBatch.DrawRectangle(new Rectangle(screenX + (TileWidth / 2) - (size / 2), screenY + (TileHeight / 2) - (size / 2), size, size), color);
		}
	}
}
