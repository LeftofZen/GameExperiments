namespace _3D
{
	public class TerrainGenParameters
	{
		public int Width { get; set; } = 256;
		public int Height { get; set; } = 256;
		public float Scale { get; set; } = 0.001f;
		public float HeightScale { get; set; } = 128f;
		public int Octaves { get; set; } = 8;
		public float Frequency { get; set; } = 2f;
		public float Amplitude { get; set; } = 1f;
		public float Persistence { get; set; } = 0.35f;
		public float Lacunarity { get; set; } = 2.5f;
	}
}
