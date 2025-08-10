using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using SharedContent;
using System;
using TrainGame.Track;
using TrainGame.World;

namespace TrainGame
{
	public class Renderer
	{
		private SpriteFont _font;
		private SpriteBatch SpriteBatch;
		private const int TileWidth = 64;
		private const int TileHeight = 32;

		public Renderer(SpriteBatch spriteBatch)
		{
			SpriteBatch = spriteBatch;
		}

		public void LoadContent(ContentManager content)
		{
			_font = content.Load<SpriteFont>($"Fonts\\{FontNames._ari_w9500}");
		}

		public void DrawIsoTile(int screenX, int screenY, int tileX, int tileY, bool isHighlight, Tile tile = null)
		{
			var color = tile.Type switch
			{
				TileType.Empty => Color.Red,
				TileType.Sand => Color.SandyBrown,
				TileType.Dirt => new Color(139, 69, 19),
				TileType.Rock => Color.Gray,
				TileType.Ice => Color.AliceBlue,
				TileType.Gravel => Color.DarkGray,
				TileType.Track => Color.LightGray, // base color, track overlay will be drawn
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

			// Draw filled diamond (approximate with two triangles)
			SpriteBatch.FillRectangle(new Rectangle(screenX + (TileWidth / 4), screenY + (TileHeight / 4), TileWidth / 2, TileHeight / 2), color * 0.7f);

			// Draw outline
			var outlineColor = isHighlight ? Color.Yellow : Color.Black;
			SpriteBatch.DrawPolygon(Vector2.Zero, poly, outlineColor, 1f);

			// Draw track overlay if present
			if (tile != null && tile.Type == TileType.Track && tile.TrackType != TrackType.None)
			{
				DrawTrackOverlay(screenX, screenY, tile.TrackType, tile.TrackDirection);
			}

			// draw tile debug info
			SpriteBatch.DrawString(_font, $"({tileX},{tileY})", new Vector2(screenX + (TileWidth / 4), screenY + (TileHeight / 4)), Color.Black, 0f, Vector2.Zero, 0.25f, SpriteEffects.None, 0f);
		}

		public void DrawTrackOverlay(int screenX, int screenY, TrackType trackType, TrackDirection dir)
		{
			var overlayColor = Color.DarkSlateGray;
			var cx = screenX + (TileWidth / 2);
			var cy = screenY + (TileHeight / 2);
			var len = TileWidth / 2;
			var thick = 6;
			// Simple visualizations for demo
			switch (trackType)
			{
				case TrackType.Straight:
					if (dir == TrackDirection.EastWest)
					{
						DrawLine(new Point(cx - (len / 2), cy), new Point(cx + (len / 2), cy), overlayColor, thick);
					}
					else if (dir == TrackDirection.NorthSouth)
					{
						DrawLine(new Point(cx, cy - (len / 2)), new Point(cx, cy + (len / 2)), overlayColor, thick);
					}

					break;
				case TrackType.Diagonal:
					if (dir == TrackDirection.NorthEast || dir == TrackDirection.SouthWest)
					{
						DrawLine(new Point(cx - (len / 2), cy + (len / 2)), new Point(cx + (len / 2), cy - (len / 2)), overlayColor, thick);
					}
					else if (dir == TrackDirection.NorthWest || dir == TrackDirection.SouthEast)
					{
						DrawLine(new Point(cx - (len / 2), cy - (len / 2)), new Point(cx + (len / 2), cy + (len / 2)), overlayColor, thick);
					}

					break;
				case TrackType.CurveSmall:
					DrawCurve(cx, cy, len / 2, dir, overlayColor, thick);
					break;
				case TrackType.CurveLarge:
					DrawCurve(cx, cy, len, dir, overlayColor, thick);
					break;
				case TrackType.CurveXL_Half:
					DrawCurve(cx, cy, len + (TileWidth / 4), dir, overlayColor, thick, 0.5f);
					break;
				case TrackType.Intersection:
					DrawLine(new Point(cx - (len / 2), cy), new Point(cx + (len / 2), cy), overlayColor, thick);
					DrawLine(new Point(cx, cy - (len / 2)), new Point(cx, cy + (len / 2)), overlayColor, thick);
					break;
			}
		}

		public void DrawLine(Point p1, Point p2, Color color, int thickness = 2)
		{
			var dx = p2.X - p1.X;
			var dy = p2.Y - p1.Y;
			var len = (int)Math.Sqrt((dx * dx) + (dy * dy));
			if (len > 0)
			{
				var angle = (float)Math.Atan2(dy, dx);
				SpriteBatch.DrawLine(p1.X, p1.Y, p2.X, p2.Y, color, thickness, 0);
			}
		}

		public void DrawCurve(int cx, int cy, int radius, TrackDirection dir, Color color, int thickness, float arcFraction = 1f)
		{
			// Draw a simple arc for curve visualization
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

		public void HighlightTileUnderCursor(int mouseX, int mouseY, Camera2D camera, TileMap tileMap)
		{
			// Convert mouse position to isometric tile coordinates
			var (tileX, tileY) = camera.ScreenToIsoTile(mouseX, mouseY, tileMap.Width, TileWidth, TileHeight);

			// Check if the tile is within bounds
			if (tileX >= 0 && tileY >= 0 && tileX < tileMap.Width && tileY < tileMap.Height)
			{
				// Get the screen position of the tile
				var (screenX, screenY) = camera.IsoTileToScreen(tileX, tileY, tileMap.Width, TileWidth, TileHeight);

				// Draw a semi-transparent highlight over the tile
				SpriteBatch.DrawRectangle(new Rectangle(screenX, screenY, TileWidth, TileHeight), Color.Yellow * 0.5f);
			}
		}
	}
}
