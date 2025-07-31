using System;

namespace _3D
{
	public static class TerrainHeightmapGenerator
	{
		public static float[,] GenerateSimplexHeightMap(TerrainGenParameters tgParams)
		{
			var noise = new SimplexNoise();
			var map = new float[tgParams.Width, tgParams.Height];

			// For normalization
			float min = float.MaxValue, max = float.MinValue;

			for (var z = 0; z < tgParams.Height; z++)
			{
				for (var x = 0; x < tgParams.Width; x++)
				{
					var frequency = tgParams.Frequency;
					var amplitude = tgParams.Amplitude;
					var noiseHeight = 0f;

					for (var o = 0; o < tgParams.Octaves; o++)
					{
						var nx = x * tgParams.Scale * frequency;
						var nz = z * tgParams.Scale * frequency;
						var value = (float)noise.Evaluate(nx, nz);

						noiseHeight += value * amplitude;

						amplitude *= tgParams.Persistence;
						frequency *= tgParams.Lacunarity;
					}

					// Track min/max for normalization
					if (noiseHeight < min)
						min = noiseHeight;
					if (noiseHeight > max)
						max = noiseHeight;

					map[x, z] = noiseHeight;
				}
			}

			// Normalize to [0,1] and apply heightScale
			for (var z = 0; z < tgParams.Height; z++)
			{
				for (var x = 0; x < tgParams.Width; x++)
				{
					var norm = (map[x, z] - min) / (max - min);
					map[x, z] = norm * tgParams.HeightScale;
				}
			}

			return map;
		}
	}
}