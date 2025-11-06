// 3D/TerrainMeshGenerator.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _3D
{
	public static class TerrainMeshGenerator
	{
		public static void GenerateTerrainMesh(
			float[,] heightMap,
			TerrainMeshParameters meshParams,
			out VertexPositionColorNormal[] terrainVertices,
			out int[] terrainIndices,
			out Vector3[,] terrainNormals)
		{
			var width = heightMap.GetLength(0);
			var height = heightMap.GetLength(1);

			var minHeight = float.MaxValue;
			var maxHeight = float.MinValue;

			for (var z = 0; z < height; z++)
			{
				for (var x = 0; x < width; x++)
				{
					var h = heightMap[x, z];
					if (h < minHeight)
						minHeight = h;
					if (h > maxHeight)
						maxHeight = h;
				}
			}

			// Generate indices
			terrainIndices = new int[(width - 1) * (height - 1) * 6];
			var i = 0;
			for (var z = 0; z < height - 1; z++)
			{
				for (var x = 0; x < width - 1; x++)
				{
					var topLeft = x + (z * width);
					var topRight = x + 1 + (z * width);
					var bottomLeft = x + ((z + 1) * width);
					var bottomRight = x + 1 + ((z + 1) * width);

					// Counter-clockwise winding:
					terrainIndices[i++] = topLeft;
					terrainIndices[i++] = topRight;
					terrainIndices[i++] = bottomLeft;

					terrainIndices[i++] = topRight;
					terrainIndices[i++] = bottomRight;
					terrainIndices[i++] = bottomLeft;
				}
			}

			terrainVertices = new VertexPositionColorNormal[width * height];

			var xOffset = width / 2f;
			var zOffset = height / 2f;

			for (var z = 0; z < height; z++)
			{
				for (var x = 0; x < width; x++)
				{
					terrainVertices[x + (z * width)] = new VertexPositionColorNormal(
						new Vector3(x - xOffset, heightMap[x, z], z - zOffset),
						Color.Magenta, // Temporary, will be recalculated below
						Vector3.Up // Temporary, will be recalculated below
					);
				}
			}

			// Calculate normals
			for (var v = 0; v < terrainVertices.Length; v++)
				terrainVertices[v].Normal = Vector3.Zero;

			for (var n = 0; n < terrainIndices.Length / 3; n++)
			{
				var i1 = terrainIndices[n * 3];
				var i2 = terrainIndices[(n * 3) + 1];
				var i3 = terrainIndices[(n * 3) + 2];

				var side1 = terrainVertices[i2].Position - terrainVertices[i1].Position;
				var side2 = terrainVertices[i3].Position - terrainVertices[i1].Position;
				var normal = Vector3.Cross(side2, side1);

				terrainVertices[i1].Normal += normal;
				terrainVertices[i2].Normal += normal;
				terrainVertices[i3].Normal += normal;
			}
			for (var v = 0; v < terrainVertices.Length; v++)
				terrainVertices[v].Normal.Normalize();

			// Now, color if normal is steeper than cliff angle
			var cliffAngle = MathHelper.ToRadians(90 - meshParams.CliffAngle);

			for (var v = 0; v < terrainVertices.Length; v++)
			{
				ref var currentVert = ref terrainVertices[v];
				var t = (currentVert.Position.Y - minHeight) / (maxHeight - minHeight);
				Color heightColour;
				if (t < 0.2f)
					heightColour = Color.Blue;
				else if (t < 0.7f)
					heightColour = Color.Green;
				else if (t < 0.9f)
					heightColour = Color.Gray;
				else
					heightColour = Color.White;

				currentVert.Color = heightColour;

				var dot = Vector3.Dot(currentVert.Normal, Vector3.Up);
				if (dot < cliffAngle)
				{
					currentVert.Color = Color.Lerp(heightColour, Color.DarkGray, meshParams.CliffAngleLerp);
				}
			}

			terrainNormals = new Vector3[width, height];
			for (var z = 0; z < height; z++)
				for (var x = 0; x < width; x++)
					terrainNormals[x, z] = terrainVertices[x + (z * width)].Normal;
		}
	}
}