using System;
using System.Collections.Generic;
using System.Linq;
using TrainGame.Track;

namespace TrainGame.World
{
	public class PathFinding
	{
		public class TrackNode
		{
			public int X, Y;
			public List<TrackEdge> Edges = new();
		}

		public class TrackEdge
		{
			public TrackNode From, To;
			public List<(int x, int y)> PathTiles = new(); // The tiles this edge covers
			public float Cost;
		}

		private Dictionary<(int, int), TrackNode> _trackNodes = new();
		private List<TrackEdge> _trackEdges = new();

		public void RebuildTrackGraph(TileMap tileMap)
		{
			_trackNodes.Clear();
			_trackEdges.Clear();
			// 1. Find all intersections and endpoints
			for (var x = 0; x < tileMap.Width; x++)
			{
				for (var y = 0; y < tileMap.Height; y++)
				{
					var tile = tileMap.Tiles[x, y];
					//if (tile.HasTrack())
					//{
					//	var connections = CountTrackConnections(tileMap, x, y);
					//	var tl = tile.TrackLayers;
					//	if (tl == null)
					//	{
					//		if (connections > 2)
					//		{
					//			tl = new TrackLayer { IsIntersection = true };
					//			tile.AddLayer(tl);
					//		}
					//	}
					//	else
					//	{
					//		tl.IsIntersection = connections > 2;
					//	}

					//	if ((tl != null && tl.IsIntersection) || connections == 1)
					//	{
					//		var node = new TrackNode { X = x, Y = y };
					//		_trackNodes[(x, y)] = node;
					//	}
					//}
				}
			}

			// 2. Build edges between nodes (walk straight uninterrupted track sections)
			foreach (var node in _trackNodes.Values)
			{
				foreach (var dir in GetTrackDirections(tileMap, node.X, node.Y))
				{
					int nx = node.X, ny = node.Y;
					List<(int, int)> path = new() { (nx, ny) };
					while (true)
					{
						(nx, ny) = StepInDirection(nx, ny, dir);
						if (nx < 0 || ny < 0 || nx >= tileMap.Width || ny >= tileMap.Height)
						{
							break;
						}

						if (!tileMap.Tiles[nx, ny].HasTrack())
						{
							break;
						}

						path.Add((nx, ny));
						if (_trackNodes.ContainsKey((nx, ny)))
						{
							var edge = new TrackEdge { From = node, To = _trackNodes[(nx, ny)], PathTiles = new(path), Cost = path.Count };
							node.Edges.Add(edge);
							_trackEdges.Add(edge);
							break;
						}

						if (path.Count > 1000)
						{
							break; // safety
						}
					}
				}
			}
		}

		public List<(int x, int y)> FindPath((int x, int y) start, (int x, int y) goal)
		{
			if (!_trackNodes.ContainsKey(start) || !_trackNodes.ContainsKey(goal))
			{
				return null;
			}

			var openSet = new SortedSet<(float, TrackNode)>(Comparer<(float, TrackNode)>.Create((a, b) => a.Item1 != b.Item1 ? a.Item1.CompareTo(b.Item1) : a.Item2.GetHashCode().CompareTo(b.Item2.GetHashCode())));
			var cameFrom = new Dictionary<TrackNode, TrackNode>();
			var gScore = new Dictionary<TrackNode, float>();
			var fScore = new Dictionary<TrackNode, float>();

			var startNode = _trackNodes[start];
			var goalNode = _trackNodes[goal];

			gScore[startNode] = 0;
			fScore[startNode] = Heuristic(startNode, goalNode);
			openSet.Add((fScore[startNode], startNode));

			while (openSet.Count > 0)
			{
				var current = openSet.Min.Item2;
				if (current == goalNode)
				{
					// Reconstruct path
					var path = new List<(int x, int y)>();
					var node = current;
					while (cameFrom.ContainsKey(node))
					{
						path.Add((node.X, node.Y));
						node = cameFrom[node];
					}

					path.Add((startNode.X, startNode.Y));
					path.Reverse();
					return path;
				}

				openSet.Remove(openSet.Min);

				foreach (var edge in current.Edges)
				{
					var neighbor = edge.To;
					var tentativeG = gScore[current] + edge.Cost;
					if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
					{
						cameFrom[neighbor] = current;
						gScore[neighbor] = tentativeG;
						fScore[neighbor] = tentativeG + Heuristic(neighbor, goalNode);
						openSet.Add((fScore[neighbor], neighbor));
					}
				}
			}

			return null;
		}

		private float Heuristic(TrackNode a, TrackNode b)
		{
			return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
		}

		private int CountTrackConnections(TileMap tileMap, int x, int y)
		{
			var count = 0;
			foreach (var dir in GetTrackDirections(tileMap, x, y))
			{
				var (nx, ny) = StepInDirection(x, y, dir);
				if (nx >= 0 && ny >= 0 && nx < tileMap.Width && ny < tileMap.Height && tileMap.Tiles[nx, ny].HasTrack())
				{
					count++;
				}
			}

			return count;
		}

		private IEnumerable<TrackDirection> GetTrackDirections(TileMap tileMap, int x, int y)
		{
			var tile = tileMap.Tiles[x, y];
			var tls = tile.TrackLayers;

			foreach (var tl in tls)
			{

				switch (tl.TrackType)
				{
					case TrackType.Straight:
						if (tl.TrackDirection == TrackDirection.EastWest)
						{
							yield return TrackDirection.EastWest;
							yield return TrackDirection.EastWest; // both directions
						}

						if (tl.TrackDirection == TrackDirection.NorthSouth)
						{
							yield return TrackDirection.NorthSouth;
							yield return TrackDirection.NorthSouth;
						}

						break;
					case TrackType.Diagonal:
						if (tl.TrackDirection == TrackDirection.NorthEast) { yield return TrackDirection.NorthEast; }

						if (tl.TrackDirection == TrackDirection.SouthWest) { yield return TrackDirection.SouthWest; }

						if (tl.TrackDirection == TrackDirection.NorthWest) { yield return TrackDirection.NorthWest; }

						if (tl.TrackDirection == TrackDirection.SouthEast) { yield return TrackDirection.SouthEast; }

						break;
					case TrackType.CurveSmall:
					case TrackType.CurveLarge:
					case TrackType.CurveXL_Half:
						// For curves, use the direction as entry/exit
						yield return tl.TrackDirection;
						break;
					case TrackType.Intersection:
						// All four cardinal directions
						yield return TrackDirection.EastWest;
						yield return TrackDirection.NorthSouth;
						break;
				}
			}
		}

		private (int, int) StepInDirection(int x, int y, TrackDirection dir)
		{
			switch (dir)
			{
				case TrackDirection.EastWest: return (x + 1, y);
				case TrackDirection.NorthSouth: return (x, y - 1);
				case TrackDirection.NorthEast: return (x + 1, y - 1);
				case TrackDirection.SouthWest: return (x - 1, y + 1);
				case TrackDirection.NorthWest: return (x - 1, y - 1);
				case TrackDirection.SouthEast: return (x + 1, y + 1);
				// For curves, treat as moving to the next logical tile (simplified)
				case TrackDirection.NE_to_E: return (x + 1, y);
				case TrackDirection.E_to_SE: return (x + 1, y + 1);
				case TrackDirection.SE_to_S: return (x, y + 1);
				case TrackDirection.S_to_SW: return (x - 1, y + 1);
				case TrackDirection.SW_to_W: return (x - 1, y);
				case TrackDirection.W_to_NW: return (x - 1, y - 1);
				case TrackDirection.NW_to_N: return (x, y - 1);
				case TrackDirection.N_to_NE: return (x + 1, y - 1);
				default: return (x, y);
			}
		}
	}
}