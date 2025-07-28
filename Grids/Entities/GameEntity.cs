using Microsoft.Xna.Framework;
using System;

namespace Experiments.Entities
{
	public class GameEntity : IGameEntity
	{
		public Rectangle HitBox { get; private set; }

		public GameEntity(Vector2 position) : this(position.ToPoint())
		{ }

		private const int DefaultSize = 16;
		private Vector2 _velocity = new Vector2(1, 0);
		private float _changeDirectionTimer = 0f;
		//private static readonly Random _random = new();
		private const float Speed = 60f; // pixels per second
		public GameEntity(Point position)
		{
			HitBox = new Rectangle(position, new Point(DefaultSize, DefaultSize));
		}

		public void Update(GameTime gameTime)
		{
			var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_changeDirectionTimer -= elapsed;
			if (_changeDirectionTimer <= 0f)
			{
				// Change direction smoothly
				var angle = ((float)Random.Shared.NextDouble()) * MathF.PI * 2f;
				var currentAngle = MathF.Atan2(_velocity.Y, _velocity.X);
				var newAngle = currentAngle + angle;
				_velocity = new Vector2(MathF.Cos(newAngle), MathF.Sin(newAngle)) * Speed;
				_changeDirectionTimer = 1f + (float)Random.Shared.NextDouble(); // Change every 1-2 seconds
			}

			// Move entity
			var position = HitBox.Location.ToVector2();
			position += _velocity * elapsed;
			HitBox = new Rectangle(position.ToPoint(), HitBox.Size);
		}
	}
}