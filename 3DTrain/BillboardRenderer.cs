using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _3DTrain
{
	// Billboard renderer class
	public class BillboardRenderer
	{
		private GraphicsDevice _graphicsDevice;
		private VertexPositionColorTexture[] _vertices;
		private short[] _indices;
		private const int MaxBillboards = 10000;

		public BillboardRenderer(GraphicsDevice graphicsDevice)
		{
			_graphicsDevice = graphicsDevice;
			_vertices = new VertexPositionColorTexture[MaxBillboards * 4];
			_indices = new short[MaxBillboards * 6];

			// Setup indices (same for all billboards)
			for (var i = 0; i < MaxBillboards; i++)
			{
				var vertexOffset = i * 4;
				var indexOffset = i * 6;

				_indices[indexOffset + 0] = (short)(vertexOffset + 0);
				_indices[indexOffset + 1] = (short)(vertexOffset + 1);
				_indices[indexOffset + 2] = (short)(vertexOffset + 2);
				_indices[indexOffset + 3] = (short)(vertexOffset + 0);
				_indices[indexOffset + 4] = (short)(vertexOffset + 2);
				_indices[indexOffset + 5] = (short)(vertexOffset + 3);
			}
		}

		public void Draw(System.Collections.Generic.List<BillboardData> billboards, BasicEffect effect, Vector3 cameraPosition, Quaternion cameraRotation, bool isOrthographic)
		{
			if (billboards.Count == 0) return;

			// Sort billboards back-to-front for proper alpha blending
			billboards.Sort((a, b) =>
				Vector3.DistanceSquared(b.Position, cameraPosition).CompareTo(
					Vector3.DistanceSquared(a.Position, cameraPosition)));

			var billboardCount = System.Math.Min(billboards.Count, MaxBillboards);

			// Build vertices for all billboards
			for (var i = 0; i < billboardCount; i++)
			{
				var billboard = billboards[i];
				BuildBillboardVertices(i, billboard, cameraPosition, cameraRotation, isOrthographic);
			}

			// Draw all billboards
			foreach (var pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				_graphicsDevice.DrawUserIndexedPrimitives(
					PrimitiveType.TriangleList,
					_vertices,
					0,
					billboardCount * 4,
					_indices,
					0,
					billboardCount * 2
				);
			}
		}

		private void BuildBillboardVertices(int index, BillboardData billboard, Vector3 cameraPosition, Quaternion cameraRotation, bool isOrthographic)
		{
			Vector3 right, up;

			// All billboards face a static diagonal direction (45° in XZ plane)
			// This creates the isometric look where all tiles face the same direction
			// Rotated 90° from (1,0,1) to (-1,0,1) for northwest facing
			right = Vector3.Normalize(new Vector3(-1, 0, 1));
			up = Vector3.Up; // Keep vertical

			// Calculate half-size
			Vector2 halfSize = billboard.Size / 2.0f;

			// Calculate UV coordinates
			var u1 = billboard.SourceRectangle.X / billboard.TextureSize.X;
			var v1 = billboard.SourceRectangle.Y / billboard.TextureSize.Y;
			var u2 = (billboard.SourceRectangle.X + billboard.SourceRectangle.Width) / billboard.TextureSize.X;
			var v2 = (billboard.SourceRectangle.Y + billboard.SourceRectangle.Height) / billboard.TextureSize.Y;

			// Build quad vertices
			var vertexOffset = index * 4;

			// Bottom-left
			_vertices[vertexOffset + 0] = new VertexPositionColorTexture(
				billboard.Position + (-right * halfSize.X) + (-up * halfSize.Y),
				billboard.Color,
				new Vector2(u1, v2)
			);

			// Bottom-right
			_vertices[vertexOffset + 1] = new VertexPositionColorTexture(
				billboard.Position + (right * halfSize.X) + (-up * halfSize.Y),
				billboard.Color,
				new Vector2(u2, v2)
			);

			// Top-right
			_vertices[vertexOffset + 2] = new VertexPositionColorTexture(
				billboard.Position + (right * halfSize.X) + (up * halfSize.Y),
				billboard.Color,
				new Vector2(u2, v1)
			);

			// Top-left
			_vertices[vertexOffset + 3] = new VertexPositionColorTexture(
				billboard.Position + (-right * halfSize.X) + (up * halfSize.Y),
				billboard.Color,
				new Vector2(u1, v1)
			);
		}
	}
}
