namespace TrainGame.Track
{
	public enum TrackDirection
	{
		None,
		NorthSouth,
		EastWest,
		NorthEast,
		NorthWest,
		SouthEast,
		SouthWest,
		// For curves, etc.
		NE_to_E,
		E_to_SE,
		SE_to_S,
		S_to_SW,
		SW_to_W,
		W_to_NW,
		NW_to_N,
		N_to_NE
	}
}