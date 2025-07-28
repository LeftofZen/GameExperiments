/*
 * A custom implmentation of a Quadtree for use with collision detection.
 * All thanks goes to the author of this tutorial found here:
 * http://gamedevelopment.tutsplus.com/tutorials/quick-tip-use-quadtrees-to-detect-likely-collisions-in-2d-space--gamedev-374
 */

using Microsoft.Xna.Framework;

namespace Experiments.Entities
{
	public interface IGameEntity
	{
		Rectangle HitBox { get; }
		//Vector2 Position { get; set; }
	}
}