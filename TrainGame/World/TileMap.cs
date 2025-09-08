using System;

namespace TrainGame.World
{
	public class TileMap
	{
		public int Width { get; }
		public int Height { get; }
		public Tile[,] Tiles { get; }

		public TileMap(int width, int height)
		{
			Width = width;
			Height = height;
			Tiles = new Tile[width, height];

			var numTerrainTypes = Enum.GetValues<TerrainType>().Length;

			for (var x = 0; x < width; x++)
			{
				for (var y = 0; y < height; y++)
				{
					var tile = new Tile();

					const float scaleFactor = 0.2f;
					var hashValue = (Math.Sin(x * scaleFactor) + Math.Cos(y * scaleFactor)) / 2 * numTerrainTypes;
					var terrain = ((hashValue % numTerrainTypes) + numTerrainTypes) % numTerrainTypes;

					tile.AddLayer(new TerrainLayer()
					{
						Terrain = (TerrainType)terrain,
						Height = (int)(Math.Abs(hashValue) * terrain * 4),
					});
					Tiles[x, y] = tile;
				}
			}
		}
	}
}