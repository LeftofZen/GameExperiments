using Microsoft.Xna.Framework;

namespace TrainGame.Train
{

	public class Vehicle
	{
		public int TileX { get; set; }
		public int TileY { get; set; }
		public int DestX { get; set; }
		public int DestY { get; set; }
		public float Speed { get; set; } = 3f; // tiles per second
		public TrainState State { get; set; } = TrainState.Stopped;
		public string Name { get; set; }

		// For smooth movement
		public float PosX { get; set; }
		public float PosY { get; set; }
		public int NextTileX { get; set; }
		public int NextTileY { get; set; }
		public bool IsMovingBetweenTiles { get; set; } = false;
		public float MoveProgress { get; set; } = 0f;

		public Vehicle(int tileX, int tileY, string name)
		{
			TileX = tileX;
			TileY = tileY;
			DestX = tileX;
			DestY = tileY;
			Name = name;
			PosX = tileX;
			PosY = tileY;
			NextTileX = tileX;
			NextTileY = tileY;
		}
	}
}
