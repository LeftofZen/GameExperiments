using Experiments.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using SharedContent;
using System.Linq;

namespace Experiments.Components
{
	public class QuadTreeComponent : DrawableGameComponent
	{
		private readonly SpriteBatch _spriteBatch;
		private readonly QuadTree _quadtree;
		private ButtonState _previousLeftButtonState = ButtonState.Released;
		private ButtonState _previousRightButtonState = ButtonState.Released;

		public QuadTreeComponent(Game game, SpriteBatch spriteBatch, Rectangle bounds)
			: base(game)
		{
			_spriteBatch = spriteBatch;
			_quadtree = new QuadTree(0, bounds);
		}
		public override void Draw(GameTime gameTime)
		{
			_spriteBatch.Begin();

			//_quadtree.Draw(_spriteBatch);
			Draw(_spriteBatch, _quadtree);
			DrawDebug(_spriteBatch, _quadtree, Game.Content.Load<SpriteFont>($"Fonts\\{FontNames._Pixeltype}"), new Vector2(600, 10));

			_spriteBatch.End();
			base.Draw(gameTime);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			var mouseState = Mouse.GetState();

			// Only add entity on new left click (transition from Released to Pressed)
			if (_previousLeftButtonState == ButtonState.Released &&
				mouseState.LeftButton == ButtonState.Pressed)
			{
				var mousePosition = new Point(mouseState.X, mouseState.Y);

				if (_quadtree.Bounds.Contains(mousePosition))
				{
					IGameEntity newEntity = new GameEntity(mousePosition);
					_quadtree.Insert(newEntity);
				}
			}

			// Only remove entity on new right click (transition from Released to Pressed)
			if (_previousRightButtonState == ButtonState.Released &&
				mouseState.RightButton == ButtonState.Pressed)
			{
				var mousePosition = new Point(mouseState.X, mouseState.Y);
				var mouseRect = new Rectangle(mousePosition.X, mousePosition.Y, 1, 1);

				// Retrieve possible entities at this position
				var possibleEntities = _quadtree.Retrieve(mouseRect);

				// Find the first entity whose HitBox contains the mouse position
				var entityToRemove = possibleEntities.Find(e => e.HitBox.Contains(mousePosition));
				if (entityToRemove != null)
				{
					_ = _quadtree.Delete(entityToRemove);
				}
			}

			_previousLeftButtonState = mouseState.LeftButton;
			_previousRightButtonState = mouseState.RightButton;

			QuadTree.Update(_quadtree, gameTime);
		}

		private void DrawDebug(SpriteBatch spriteBatch, QuadTree tree, SpriteFont font, Vector2 position, float indent = 0)
		{
			// Print this node's info
			var nodeInfo = $"Level {tree.Level} | Bounds: {tree.Bounds} | Objects: {tree.Objects.Count} | Nodes: {tree.Nodes.Count(x => x != null)}";
			spriteBatch.DrawString(font, nodeInfo, position + new Vector2(indent, 0), tree.Level % 2 == 0 ? Color.White : Color.Black);

			float lineHeight = font.LineSpacing;
			var yOffset = lineHeight;

			// Print all objects in this node
			foreach (var obj in tree.Objects)
			{
				var objInfo = $"Obj: {obj.HitBox}";
				spriteBatch.DrawString(font, objInfo, position + new Vector2(indent + 20, yOffset), Color.LightGray);
				yOffset += lineHeight;
			}

			// Print subnodes recursively
			for (var i = 0; i < tree.Nodes.Length; i++)
			{
				if (tree.Nodes[i] != null)
				{
					DrawDebug(spriteBatch, tree.Nodes[i], font, position + new Vector2(0, yOffset), indent + 20);
					// Add the number of lines printed by the subnode to yOffset
					// To do this, you could return the total height used from DrawDebug, but for simplicity, 
					// you can estimate or adjust as needed for your use case.
					// Here, let's assume each subnode prints at least one line (node info).
					// For more accurate spacing, you could refactor to return the used height.
					yOffset += font.LineSpacing * (1 + tree.Nodes[i].Objects.Count);
				}
			}
		}

		private void Draw(SpriteBatch spriteBatch, QuadTree tree)
		{
			if (tree != null)
			{
				_spriteBatch.DrawRectangle(tree.Bounds, tree.Level % 2 == 0 ? Color.White : Color.Black, 2f);

				foreach (var node in tree.Nodes)
				{
					Draw(spriteBatch, node);
				}

				foreach (var obj in tree.Objects)
				{
					_ = new RectangleF(
						obj.HitBox.X + obj.HitBox.X - (obj.HitBox.Width / 2),
						obj.HitBox.Y + obj.HitBox.Y - (obj.HitBox.Height / 2),
						obj.HitBox.Width,
						obj.HitBox.Height);

					spriteBatch.DrawRectangle(obj.HitBox, Color.LightGreen, 2f);
					//spriteBatch.DrawCircle(new CircleF(obj.HitBox.Center.ToVector2(), 8), 8, Color.LightGreen, 1f);
				}
			}
		}
	}
}