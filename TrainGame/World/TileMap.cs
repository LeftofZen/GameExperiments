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
			for (var x = 0; x < width; x++)
			{
				for (var y = 0; y < height; y++)
				{
					Tiles[x, y] = new Tile();
				}
			}
		}
	}
}