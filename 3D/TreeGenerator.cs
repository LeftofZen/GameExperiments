using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace _3D
{
	public class TreeGeneratorParameters
	{
		public int TreeCount { get; set; } = 2000;
		public float TreeHeightLimitMin { get; set; } = 20f;
		public float TreeHeightLimitMax { get; set; } = 80f;
		public float TrunkBaseRadiusMin { get; set; } = 0.25f;
		public float TrunkBaseRadiusMax { get; set; } = 0.75f;
		public int TrunkSegmentsMin { get; set; } = 8;
		public int TrunkSegmentsMax { get; set; } = 8;
		public int TrunkRingsMin { get; set; } = 4;
		public int TrunkRingsMax { get; set; } = 8;
		public int BranchCountMin { get; set; } = 12;
		public int BranchCountMax { get; set; } = 24;
		public int LeafCountMin { get; set; } = 12;
		public int LeafCountMax { get; set; } = 24;
	}

	public static class TreeGenerator
	{
		public static IEnumerable<TreeInstance> GenerateTrees(
			float[,] terrainHeightMap,
			Vector3[,] terrainNormals,
			TreeGeneratorParameters parameters)
		{
			var width = terrainHeightMap.GetLength(0);
			var height = terrainHeightMap.GetLength(1);
			var rand = Random.Shared;

			for (var i = 0; i < parameters.TreeCount; i++)
			{
				var x = rand.Next(0, width);
				var z = rand.Next(0, height);

				if (Vector3.Dot(terrainNormals[x, z], Vector3.Up) < 0.9f)
				{
					i--;
					continue;
				}

				var y = terrainHeightMap[x, z];
				if (y > parameters.TreeHeightLimitMax || y < parameters.TreeHeightLimitMin)
				{
					i--;
					continue;
				}

				var pos = new Vector3(x - (width / 2f), y, z - (height / 2f));
				var scale = (float)(1.0 + (rand.NextDouble() * 0.5));

				// --- Trunk ---
				GenerateTrunk(pos, scale, rand, parameters, out var verts, out var indices, out var ringTransforms, out var trunkSegments, out var trunkRings, out var baseRadius);

				// --- Branches ---
				var branchTips = GenerateBranches(verts, indices, ringTransforms, trunkSegments, trunkRings, baseRadius, scale, rand, parameters);

				// --- Leaves ---
				GenerateLeaves(verts, indices, branchTips, scale, rand, parameters);

				// --- Calculate normals for all triangles ---
				var vertexNormals = new Vector3[verts.Count];
				for (var n = 0; n < indices.Count; n += 3)
				{
					var i1 = indices[n];
					var i2 = indices[n + 1];
					var i3 = indices[n + 2];
					var v1 = verts[i1].Position;
					var v2 = verts[i2].Position;
					var v3 = verts[i3].Position;
					var side1 = v2 - v1;
					var side2 = v3 - v1;
					var normal = Vector3.Cross(side2, side1);
					vertexNormals[i1] += normal;
					vertexNormals[i2] += normal;
					vertexNormals[i3] += normal;
				}
				for (var v = 0; v < verts.Count; v++)
				{
					var nrm = vertexNormals[v];
					if (nrm != Vector3.Zero)
						nrm.Normalize();
					verts[v] = new VertexPositionColorNormal(verts[v].Position, verts[v].Color, nrm);
				}

				yield return new TreeInstance
				{
					Position = pos,
					Scale = scale,
					Mesh = new TreeMesh
					{
						Vertices = [.. verts],
						Indices = [.. indices]
					}
				};
			}
		}

		private static void GenerateTrunk(
			Vector3 pos,
			float scale,
			Random rand,
			TreeGeneratorParameters parameters,
			out List<VertexPositionColorNormal> verts,
			out List<int> indices,
			out Matrix[] ringTransforms,
			out int trunkSegments,
			out int trunkRings,
			out float baseRadius)
		{
			verts = [];
			indices = [];

			trunkSegments = rand.Next(parameters.TrunkSegmentsMin, parameters.TrunkSegmentsMax + 1);
			trunkRings = rand.Next(parameters.TrunkRingsMin, parameters.TrunkRingsMax + 1);
			var trunkHeight = rand.Next(6, 12) * scale;
			baseRadius = ((float)rand.NextDouble() * (parameters.TrunkBaseRadiusMax - parameters.TrunkBaseRadiusMin) + parameters.TrunkBaseRadiusMin) * scale;
			var topRadius = baseRadius * (0.3f + 0.2f * (float)rand.NextDouble()); // 30-50% of base
			var segmentHeight = trunkHeight / trunkRings;

			ringTransforms = new Matrix[trunkRings + 1];
			ringTransforms[0] = Matrix.CreateTranslation(pos);

			var trunkRadii = new float[trunkRings + 1];
			for (var r = 0; r <= trunkRings; r++)
			{
				var t = (float)r / trunkRings;
				trunkRadii[r] = MathHelper.Lerp(baseRadius, topRadius, t);
			}

			var dir = Vector3.Up;

			for (var r = 1; r <= trunkRings; r++)
			{
				var yaw = (float)(rand.NextDouble() - 0.5) * 0.2f;
				var pitch = (float)(rand.NextDouble() - 0.5) * 0.2f;
				var rot = Matrix.CreateFromYawPitchRoll(yaw, pitch, 0);
				dir = Vector3.TransformNormal(dir, rot);
				dir.Normalize();

				var prevPos = ringTransforms[r - 1].Translation;
				var nextPos = prevPos + dir * segmentHeight;
				var up = dir;
				var right = Vector3.Normalize(Vector3.Cross(Vector3.Forward, up));
				if (right.LengthSquared() < 0.01f) right = Vector3.Right;
				var forward = Vector3.Normalize(Vector3.Cross(up, right));
				ringTransforms[r] = new Matrix(
					right.X, right.Y, right.Z, 0,
					up.X, up.Y, up.Z, 0,
					forward.X, forward.Y, forward.Z, 0,
					nextPos.X, nextPos.Y, nextPos.Z, 1
				);
			}

			var vertIndices = new int[trunkRings + 1, trunkSegments];
			for (var r = 0; r <= trunkRings; r++)
			{
				var ringRadius = trunkRadii[r];
				for (var s = 0; s < trunkSegments; s++)
				{
					var angle = MathF.PI * 2 * s / trunkSegments;
					var dx = MathF.Cos(angle) * ringRadius;
					var dz = MathF.Sin(angle) * ringRadius;
					var local = new Vector3(dx, 0, dz);
					var world = Vector3.Transform(local, ringTransforms[r]);
					vertIndices[r, s] = verts.Count;
					verts.Add(new VertexPositionColorNormal(world, Color.SaddleBrown, Vector3.Zero));
				}
			}

			for (var r = 0; r < trunkRings; r++)
			{
				for (var s = 0; s < trunkSegments; s++)
				{
					var sNext = (s + 1) % trunkSegments;
					var i00 = vertIndices[r, s];
					var i01 = vertIndices[r, sNext];
					var i10 = vertIndices[r + 1, s];
					var i11 = vertIndices[r + 1, sNext];
					indices.Add(i00); indices.Add(i01); indices.Add(i10);
					indices.Add(i01); indices.Add(i11); indices.Add(i10);
				}
			}
		}

		private static List<Vector3> GenerateBranches(
			List<VertexPositionColorNormal> verts,
			List<int> indices,
			Matrix[] ringTransforms,
			int trunkSegments,
			int trunkRings,
			float baseRadius,
			float scale,
			Random rand,
			TreeGeneratorParameters parameters)
		{
			var branchCount = rand.Next(parameters.BranchCountMin, parameters.BranchCountMax);
			var branchTips = new List<Vector3>();
			var topRingCenter = ringTransforms[trunkRings].Translation;
			var topRingDir = ringTransforms[trunkRings].Up;
			Vector3 dir;

			for (var b = 0; b < branchCount; b++)
			{
				var branchAngle = (MathF.PI * 2 * b / branchCount) + (float)rand.NextDouble();
				var branchTilt = (MathF.PI / 4) + ((float)rand.NextDouble() * MathF.PI / 8);
				var branchLen = 4f * scale * (0.8f + ((float)rand.NextDouble() * 0.4f));
				var branchBase = topRingCenter;
				dir = new Vector3(MathF.Cos(branchAngle) * MathF.Sin(branchTilt), MathF.Cos(branchTilt), MathF.Sin(branchAngle) * MathF.Sin(branchTilt));
				var branchTip = branchBase + (dir * branchLen);
				branchTips.Add(branchTip);
				var branchRadius = baseRadius * 0.4f;

				var branchStart = verts.Count;
				for (var s = 0; s < trunkSegments; s++)
				{
					var angle = MathF.PI * 2 * s / trunkSegments;
					var offset = Vector3.Transform(new Vector3(MathF.Cos(angle) * branchRadius, 0, MathF.Sin(angle) * branchRadius),
						Matrix.CreateFromAxisAngle(Vector3.Cross(Vector3.Up, dir), branchTilt));
					verts.Add(new VertexPositionColorNormal(branchBase + offset, Color.SaddleBrown, Vector3.Zero));
					verts.Add(new VertexPositionColorNormal(branchTip + (offset * 0.5f), Color.SaddleBrown, Vector3.Zero));
				}

				for (var s = 0; s < trunkSegments; s++)
				{
					var b0 = branchStart + (s * 2);
					var t0 = b0 + 1;
					var b1 = branchStart + ((s + 1) % trunkSegments * 2);
					var t1 = b1 + 1;
					indices.Add(b0); indices.Add(b1); indices.Add(t0);
					indices.Add(b1); indices.Add(t1); indices.Add(t0);
				}
			}

			return branchTips;
		}

		private static void GenerateLeaves(
			List<VertexPositionColorNormal> verts,
			List<int> indices,
			List<Vector3> branchTips,
			float scale,
			Random rand,
			TreeGeneratorParameters parameters)
		{
			foreach (var tip in branchTips)
			{
				var leafCount = rand.Next(parameters.LeafCountMin, parameters.LeafCountMax);
				for (var l = 0; l < leafCount; l++)
				{
					var angle = (MathF.PI * 2 * l / leafCount) + (float)rand.NextDouble();
					var radius = 1.2f * scale * (0.7f + ((float)rand.NextDouble() * 0.6f));
					var p0 = tip + new Vector3(MathF.Cos(angle) * radius, 0, MathF.Sin(angle) * radius);
					var p1 = tip + new Vector3(MathF.Cos(angle + 0.2f) * (radius * 0.7f), 0.7f * scale, MathF.Sin(angle + 0.2f) * (radius * 0.7f));
					var p2 = tip + new Vector3(MathF.Cos(angle - 0.2f) * (radius * 0.7f), 0.7f * scale, MathF.Sin(angle - 0.2f) * (radius * 0.7f));
					var leafStart = verts.Count;

					var edge1 = p1 - p0;
					var edge2 = p2 - p0;
					var normal = Vector3.Cross(edge1, edge2);
					if (normal != Vector3.Zero)
						normal.Normalize();

					verts.Add(new VertexPositionColorNormal(p0, Color.ForestGreen, normal));
					verts.Add(new VertexPositionColorNormal(p1, Color.ForestGreen, normal));
					verts.Add(new VertexPositionColorNormal(p2, Color.ForestGreen, normal));
					indices.Add(leafStart); indices.Add(leafStart + 1); indices.Add(leafStart + 2);

					verts.Add(new VertexPositionColorNormal(p0, Color.ForestGreen, -normal));
					verts.Add(new VertexPositionColorNormal(p2, Color.ForestGreen, -normal));
					verts.Add(new VertexPositionColorNormal(p1, Color.ForestGreen, -normal));
					indices.Add(leafStart + 3); indices.Add(leafStart + 4); indices.Add(leafStart + 5);
				}
			}
		}
	}

	public class TreeInstance
	{
		public Vector3 Position { get; set; }
		public float Scale { get; set; }
		public TreeMesh Mesh { get; set; }
	}

	public struct TreeMesh
	{
		public VertexPositionColorNormal[] Vertices;
		public int[] Indices;
	}
}