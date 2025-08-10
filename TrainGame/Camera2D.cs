using Microsoft.Xna.Framework;

namespace TrainGame
{
	public class Camera2D
	{
		public Vector2 Position { get; set; } = Vector2.Zero;
		public float Zoom { get; set; } = 1f;
		public float Rotation { get; set; } = 0f;

		public Matrix GetTransformMatrix()
		{
			return
				Matrix.CreateTranslation(new Vector3(-Position, 0)) *
				Matrix.CreateRotationZ(Rotation) *
				Matrix.CreateScale(Zoom, Zoom, 1);
		}

		public (int, int) ScreenToIsoTile(int screenX, int screenY, int tileMapWidth, int tileWidth, int tileHeight)
		{
			// Transform screen coordinates to world coordinates considering camera position and zoom
			var worldPosition = Vector2.Transform(new Vector2(screenX, screenY), Matrix.Invert(GetTransformMatrix()));

			// Adjust world coordinates to center the isometric grid
			var fx = worldPosition.X - (tileMapWidth * tileWidth / 2f);
			var fy = worldPosition.Y;

			// Convert world coordinates to isometric tile coordinates
			var tileY = (int)((fy / tileHeight) - (fx / tileWidth));
			var tileX = (int)((fx / tileWidth) + (fy / tileHeight));

			return (tileX, tileY);
		}

		public (int, int) IsoTileToScreen(int tileX, int tileY, int tileMapWidth, int tileWidth, int tileHeight)
		{
			// Convert isometric tile coordinates to screen coordinates
			var screenX = ((tileX - tileY) * (tileWidth / 2)) + (tileMapWidth * tileWidth / 2);
			var screenY = (tileX + tileY) * (tileHeight / 2);
			return (screenX, screenY);
		}

		public (int, int) IsoTileToScreenF(float tileX, float tileY, int tileMapWidth, int tileWidth, int tileHeight)
		{
			var screenX = (int)(((tileX - tileY) * (tileWidth / 2)) + (tileMapWidth * tileWidth / 2));
			var screenY = (int)((tileX + tileY) * (tileHeight / 2));
			return (screenX, screenY);
		}
	}
}