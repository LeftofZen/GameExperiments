using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PDS
{
	public static class PoissonDiscSampler
	{
		public static List<Vector2> GeneratePoints(float minDist, int width, int height, int k = 30)
		{
			var rng = new Random();
			float cellSize = minDist / (float)Math.Sqrt(2);
			int gridWidth = (int)Math.Ceiling(width / cellSize);
			int gridHeight = (int)Math.Ceiling(height / cellSize);
			var grid = new Vector2?[gridWidth, gridHeight];
			var points = new List<Vector2>();
			var processList = new List<Vector2>();

			Vector2 firstPoint = new Vector2(rng.Next(width), rng.Next(height));
			points.Add(firstPoint);
			processList.Add(firstPoint);
			grid[(int)(firstPoint.X / cellSize), (int)(firstPoint.Y / cellSize)] = firstPoint;

			while (processList.Count > 0)
			{
				int idx = rng.Next(processList.Count);
				Vector2 point = processList[idx];
				bool found = false;

				for (int i = 0; i < k; i++)
				{
					float angle = (float)(rng.NextDouble() * Math.PI * 2);
					float radius = minDist + (float)rng.NextDouble() * minDist;
					Vector2 newPoint = point + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;

					if (newPoint.X >= 0 && newPoint.X < width && newPoint.Y >= 0 && newPoint.Y < height)
					{
						int gx = (int)(newPoint.X / cellSize);
						int gy = (int)(newPoint.Y / cellSize);
						bool ok = true;

						for (int x = Math.Max(0, gx - 2); x <= Math.Min(gridWidth - 1, gx + 2); x++)
						{
							for (int y = Math.Max(0, gy - 2); y <= Math.Min(gridHeight - 1, gy + 2); y++)
							{
								if (grid[x, y].HasValue && Vector2.Distance(grid[x, y].Value, newPoint) < minDist)
								{
									ok = false;
									break;
								}
							}
							if (!ok) break;
						}

						if (ok)
						{
							points.Add(newPoint);
							processList.Add(newPoint);
							grid[gx, gy] = newPoint;
							found = true;
						}
					}
				}

				if (!found)
				{
					processList.RemoveAt(idx);
				}
			}

			return points;
		}
	}
}
