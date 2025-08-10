using TrainGame.Track;

namespace TrainGame.World
{
	public class Tile
	{
		public TileType Type { get; set; } = TileType.Empty;
		public TrackType TrackType { get; set; } = TrackType.None;
		public TrackDirection TrackDirection { get; set; } = TrackDirection.None;
		public bool IsIntersection { get; set; } = false;
	}
}