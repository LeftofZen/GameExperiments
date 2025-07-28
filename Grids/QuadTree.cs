/*
 * A custom implmentation of a Quadtree for use with collision detection.
 * All thanks goes to the author of this tutorial found here:
 * http://gamedevelopment.tutsplus.com/tutorials/quick-tip-use-quadtrees-to-detect-likely-collisions-in-2d-space--gamedev-374
 */

using Experiments.Entities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Experiments
{
	public class QuadTree
	{
		/// <summary>
		/// How many objects a node can hold before it splits
		/// </summary>
		private const int MAX_OBJECTS = 3;

		/// <summary>
		/// The deepest level subnode.
		/// </summary>
		private const int MAX_LEVELS = 5;

		/// <summary>
		/// The current node level (0 being the topmost)
		/// </summary>
		public int Level { get; init; }

		/// <summary>
		/// The list of objects in our current node
		/// </summary>
		public List<IGameEntity> Objects { get; private set; }

		/// <summary>
		/// The 2D space that the node occupies
		/// </summary>
		public Rectangle Bounds { get; init; }

		/// <summary>
		/// The four subnodes. Nodes fill out in a counter clockwise manner
		/// </summary>
		public QuadTree[] Nodes { get; init; }

		/// <summary>
		/// Gets the count of how many objects are in this current node.
		/// </summary>
		public int Count => Objects.Count;

		/// <summary>
		/// Constructor
		/// </summary>
		public QuadTree(int pLevel, Rectangle pBounds)
		{
			Level = pLevel;
			Objects = [];
			Bounds = pBounds;
			Nodes = new QuadTree[4];
		}

		/// <summary>
		/// Clears the quadtree recursively
		/// </summary>
		public void clear()
		{
			Objects.Clear();

			for (var i = 0; i < Nodes.Length; i++)
			{
				if (Nodes[i] != null)
				{
					Nodes[i].clear();
					Nodes[i] = null;
				}
			}
		}

		/// <summary>
		/// Splits the node into 4 subnodes, dividing the node into four equal parts and initializing
		/// the four subnodes with the new bounds.
		/// </summary>
		private void Split()
		{
			var subWidth = Bounds.Width / 2;
			var subHeight = Bounds.Height / 2;
			var x = Bounds.X;
			var y = Bounds.Y;

			Nodes[0] = new QuadTree(Level + 1, new Rectangle(x + subWidth, y, subWidth, subHeight));
			Nodes[1] = new QuadTree(Level + 1, new Rectangle(x, y, subWidth, subHeight));
			Nodes[2] = new QuadTree(Level + 1, new Rectangle(x, y + subHeight, subWidth, subHeight));
			Nodes[3] = new QuadTree(Level + 1, new Rectangle(x + subWidth, y + subHeight, subWidth, subHeight));
		}


		/// <summary>
		/// A helper function of the quadtree. It determines where an object belongs in the quadtree
		/// by determining which node the object can fit into.
		/// </summary>
		/// <param name="pRect">The rectangle being checked</param>
		/// <returns>The node that the object fits into, -1 means it fits in the parent node</returns>
		private int GetIndex(Rectangle pRect)
		{
			var index = -1;
			double verticalMidpoint = Bounds.X + (Bounds.Width / 2);
			double horizontalMidpoint = Bounds.Y + (Bounds.Height / 2);

			// Object can completely fit within the top quadrants
			var topQuadrant = pRect.Y < horizontalMidpoint && pRect.Y + pRect.Height < horizontalMidpoint;

			// Object can completely fit within the bottom quadrants
			var bottomQuadrant = pRect.Y > horizontalMidpoint;

			// Object can completely fit within the left quadrants
			if (pRect.X < verticalMidpoint && pRect.X + pRect.Width < verticalMidpoint)
			{
				if (topQuadrant)
				{
					index = 1;
				}
				else if (bottomQuadrant)
				{
					index = 2;
				}
			}
			// Object can completely fit within the right quadrants
			else if (pRect.X > verticalMidpoint)
			{
				if (topQuadrant)
				{
					index = 0;
				}
				else if (bottomQuadrant)
				{
					index = 3;
				}
			}
			return index;
		}

		/// <summary>
		/// Insert the object into the quad tree. If the node exceeds the capacity, it will split
		/// and add all objects to their corresponding nodes.
		/// </summary>
		/// <param name="pRect"></param>
		public void Insert(IGameEntity pBody)
		{
			if (!Bounds.Contains(pBody.HitBox))
			{
				return;
			}

			if (Nodes[0] != null)
			{
				var index = GetIndex(pBody.HitBox);
				if (index != -1)
				{
					Nodes[index].Insert(pBody);
					return;
				}
			}

			Objects.Add(pBody);

			if (Objects.Count > MAX_OBJECTS && Level < MAX_LEVELS)
			{
				if (Nodes[0] == null)
				{
					Split();
				}
				List<IGameEntity> save = [];
				foreach (var e in Objects.ToList())
				{
					var index = GetIndex(e.HitBox);
					if (index != -1)
					{
						Nodes[index].Insert(e);
					}
					else
					{
						save.Add(e);
					}
				}
				Objects = save;
			}
		}

		/// <summary>
		/// Removes the passed object from this node
		/// </summary>
		/// <param name="pBody"></param>
		private void Remove(IGameEntity pBody)
		{
			if (Objects != null && Objects.Contains(pBody))
			{
				_ = Objects.Remove(pBody);
			}
		}

		/// <summary>
		/// Deletes the item from this QuadTree. If the object is removed causes the Quadtree to have
		/// no objects in its children, they will also be removed.
		/// </summary>
		/// <param name="pBody"></param>
		public bool Delete(IGameEntity pBody)
		{
			// If the object is not within this node's bounds, do nothing
			if (!Bounds.Contains(pBody.HitBox))
			{
				return false;
			}

			// Try to remove from this node's objects
			if (Objects.Remove(pBody))
			{
				return true;
			}

			// If there are subnodes, try to delete recursively
			var deleted = false;
			for (var i = 0; i < Nodes.Length; i++)
			{
				if (Nodes[i] != null)
				{
					deleted = Nodes[i].Delete(pBody) || deleted;
				}
			}

			// After deletion, check if all subnodes are empty and collapse if so
			if (Nodes[0] != null && Nodes.All(n => n == null || (n.Objects.Count == 0 && n.Nodes.All(sub => sub == null))))
			{
				for (var i = 0; i < Nodes.Length; i++)
				{
					Nodes[i] = null;
				}
			}

			return deleted;
		}

		/// <summary>
		/// Returns all objects that could collide with the given object
		/// </summary>
		/// <param name="returnObjects">The list of potentially colliding objects</param>
		/// <param name="pRect">The boundries being compared against</param>
		/// <param name="exclude">An optional body to exclude from the list</param>
		/// <returns></returns>
		public List<IGameEntity> Retrieve(Rectangle pRect)
		{
			var index = GetIndex(pRect);
			var returnObjects = new List<IGameEntity>(Objects);

			// if we have Subnodes
			if (Nodes[0] != null)
			{
				// If the pRect fits into a sub node
				if (index != -1)
				{
					returnObjects.AddRange(Nodes[index].Retrieve(pRect));
				}
				// If the pRect does not fit into a sub node, check it against all subnodes
				else
				{
					for (var i = 0; i < Nodes.Length; i++)
					{
						returnObjects.AddRange(Nodes[i].Retrieve(pRect));
					}
				}
			}
			return returnObjects;
		}

		public static void Update(QuadTree tree, GameTime gameTime)
		{
			if (tree == null)
			{
				return;
			}

			foreach (var entity in tree.Objects)
			{
				_ = tree.UpdateEntity(entity, gameTime);

				Update(tree.Nodes[0], gameTime);
				Update(tree.Nodes[1], gameTime);
				Update(tree.Nodes[2], gameTime);
				Update(tree.Nodes[3], gameTime);
			}
		}

		/// <summary>
		/// Updates the position of an entity in the quadtree if it has moved outside its current node's bounds.
		/// Removes the entity from its current node and reinserts it into the correct node.
		/// </summary>
		/// <param name="pBody">The entity to update</param>
		/// <returns>True if the entity was moved to a different node, false otherwise</returns>
		private bool UpdateEntity(IGameEntity pBody, GameTime gameTime)
		{
			((GameEntity)pBody).Update(gameTime);

			// If the entity is still within this node's bounds, no update needed
			if (Bounds.Contains(pBody.HitBox))
			{
				// If the entity is not in this node's Objects, it may be in a child node
				if (Objects.Contains(pBody))
				{
					return false;
				}

				// Try to update in subnodes
				for (var i = 0; i < Nodes.Length; i++)
				{
					if (Nodes[i] != null && Nodes[i].Bounds.Intersects(pBody.HitBox))
					{
						if (Nodes[i].UpdateEntity(pBody, gameTime))
						{
							return true;
						}
					}
				}
				return false;
			}
			else
			{
				// Remove from current node (or subnode)
				if (Delete(pBody))
				{
					// Reinsert from the root node (assumes this is called from the root)
					Insert(pBody);
					return true;
				}
				// If not found in this node, try subnodes
				for (var i = 0; i < Nodes.Length; i++)
				{
					if (Nodes[i] != null && Nodes[i].Bounds.Intersects(pBody.HitBox))
					{
						if (Nodes[i].UpdateEntity(pBody, gameTime))
						{
							return true;
						}
					}
				}
				return false;
			}
		}
	}
}