using System;
using System.Collections.Generic;
using System.Linq;
using TrainGame.Track;

namespace TrainGame.World
{
	public enum LayerKind
	{
		Terrain,
		Track,
		Prop
	}

	public abstract class TileLayer
	{
		public LayerKind Kind { get; }
		protected TileLayer(LayerKind kind) => Kind = kind;

		public override string ToString()
			=> $"{Kind}";
	}

	public class TerrainLayer : TileLayer
	{
		public TerrainType Terrain { get; set; } = TerrainType.Grass;
		public TerrainLayer() : base(LayerKind.Terrain) { }

		public int Height { get; set; }

		public override string ToString()
			=> $"{Kind} - {Terrain}";
	}

	public class TrackLayer : TileLayer
	{
		public TrackType TrackType { get; set; } = TrackType.None;
		public TrackDirection TrackDirection { get; set; } = TrackDirection.EastWest;
		public bool IsIntersection { get; set; }

		public TrackLayer() : base(LayerKind.Track) { }

		public override string ToString()
			=> $"{Kind} - {TrackType} - {TrackDirection}";
	}

	public class PropLayer : TileLayer
	{
		public string Id { get; set; }
		public PropLayer(string id) : base(LayerKind.Prop) => Id = id;
	}

	public class Tile
	{
		public List<TileLayer> Layers { get; private set; } = [];

		public void AddLayer(TileLayer layer)
		{
			if (layer == null)
			{
				return;
			}

			// Don't add duplicate single-instance layers (like Track)
			if (layer is TrackLayer ntl && Layers.Any(l => l is TrackLayer tl && tl.TrackType == ntl.TrackType))
			{
				// don't add duplicate track pieces
				return;
			}

			Layers.Add(layer);
		}

		public bool RemoveLayer(TileLayer layer)
			=> layer != null && Layers.Remove(layer);

		public void ClearLayers()
			=> Layers.Clear();

		public IEnumerable<TrackLayer> TrackLayers
			=> Layers.OfType<TrackLayer>();

		public bool HasTrack()
			=> TrackLayers.Any(x => x.TrackType != TrackType.None);
	}
}