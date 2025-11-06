using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Shared
{
	public class Camera3D
	{
		public Vector3 Position { get; set; }
		public Quaternion Rotation { get; set; }
		public Matrix ViewMatrix { get; private set; }
		public Matrix ProjectionMatrix { get; private set; }

		public float FieldOfView
		{
			get => _fieldOfView;
			set
			{
				_fieldOfView = value;
				UpdateProjectionMatrix();
			}
		}
		private float _fieldOfView;
		private float _aspectRatio;
		private float _nearPlaneDistance = 0.1f;
		private float _farPlaneDistance = float.MaxValue;

		const float DefaultMovementSpeed = 50f;
		const float BoostMultiplier = 5f;
		public float MovementSpeed { get; set; } = DefaultMovementSpeed;
		public float RotationSpeed { get; set; } = 0.003f;

		GraphicsDevice GraphicsDevice { get; init; }

		private MouseState previousMouseState;

		public enum ProjectionMode
		{
			Perspective,
			Isometric
		}

		private float _isometricSize = 200f; // Default size for isometric projection
		public ProjectionMode CurrentProjectionMode
		{
			get => projectionMode;
			set
			{
				projectionMode = value;
				UpdateProjectionMatrix();
			}

		}
		private ProjectionMode projectionMode = ProjectionMode.Perspective;


		/// <summary>
		/// Initializes a new instance of the Camera class.
		/// The camera is initially positioned and rotated to look at the origin (0,0,0).
		/// </summary>
		/// <param name="graphicsDevice">The GraphicsDevice used for projection setup.</param>
		/// <param name="initialPosition">The initial position of the camera.</param>
		/// <param name="fieldOfView">The camera's field of view in radians.</param>
		/// <param name="nearPlaneDistance">The distance to the near clipping plane.</param>
		/// <param name="farPlaneDistance">The distance to the far clipping plane.</param>
		public Camera3D(GraphicsDevice graphicsDevice, Vector3 initialPosition, float fieldOfView = MathHelper.PiOver4, float nearPlaneDistance = 0.1f, float farPlaneDistance = float.MaxValue)
		{
			GraphicsDevice = graphicsDevice;
			Position = initialPosition;
			FieldOfView = fieldOfView;
			_aspectRatio = (float)graphicsDevice.Viewport.Width / graphicsDevice.Viewport.Height;
			_nearPlaneDistance = nearPlaneDistance;
			_farPlaneDistance = farPlaneDistance;

			// Initialize rotation to look at the origin
			// Calculate the direction vector from camera position to origin
			var directionToOrigin = Vector3.Normalize(Vector3.Zero - Position);

			// Calculate the rotation needed to point the camera's forward vector (typically -Z in view space)
			// towards the origin.
			// We can use Matrix.CreateLookAt and then convert to Quaternion, or directly calculate.
			// A common way is to use Matrix.CreateWorld and then extract the rotation.
			var lookAtMatrix = Matrix.CreateLookAt(Position, Vector3.Zero, Vector3.Up);
			Rotation = Quaternion.CreateFromRotationMatrix(lookAtMatrix);
			// Invert the rotation because CreateLookAt creates a view matrix, and we want the world rotation
			// of the camera. The inverse of the view matrix is the camera's world matrix.
			Rotation = Quaternion.Inverse(Rotation);

			// Ensure the rotation is normalized
			Rotation.Normalize();

			// Update initial matrices
			UpdateViewMatrix();
			UpdateProjectionMatrix();
		}

		/// <summary>
		/// Updates the camera's view matrix based on its current position and rotation.
		/// </summary>
		public void UpdateViewMatrix()
		{
			// Create the camera's world matrix from its position and rotation.
			// The rotation quaternion defines the camera's orientation.
			// The camera's forward vector is usually the negative Z-axis in its local space.
			// The camera's up vector is usually the Y-axis in its local space.
			// The camera's right vector is usually the X-axis in its local space.
			var cameraWorld = Matrix.CreateFromQuaternion(Rotation);
			cameraWorld.Translation = Position;

			// The view matrix is the inverse of the camera's world matrix.
			ViewMatrix = Matrix.Invert(cameraWorld);
		}

		/// <summary>
		/// Updates the camera's projection matrix. Call this if the viewport size changes or the projection mode changes.
		/// </summary>
		public void UpdateProjectionMatrix()
		{
			if (CurrentProjectionMode == ProjectionMode.Perspective)
			{
				ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
					FieldOfView,
					_aspectRatio,
					_nearPlaneDistance,
					_farPlaneDistance
				);
			}
			else if (CurrentProjectionMode == ProjectionMode.Isometric)
			{
				float halfSize = _isometricSize / 2;

				ProjectionMatrix = Matrix.CreateOrthographicOffCenter(
					-halfSize * _aspectRatio,
					halfSize * _aspectRatio,
					-halfSize,
					halfSize,
					_nearPlaneDistance,
					_farPlaneDistance
				);
			}
		}

		/// <summary>
		/// Moves the camera relative to its current orientation.
		/// </summary>
		/// <param name="amount">The vector representing the movement in local camera space (e.g., (0,0,-1) for forward).</param>
		public void Move(Vector3 amount)
		{
			// Rotate the movement vector by the camera's current rotation
			var rotatedAmount = Vector3.Transform(amount, Rotation);
			Position += rotatedAmount * MovementSpeed;
			UpdateViewMatrix();
		}

		/// <summary>
		/// Rotates the camera around its local axes.
		/// </summary>
		/// <param name="yaw">Rotation around the Y-axis (up/down look).</param>
		/// <param name="pitch">Rotation around the X-axis (left/right turn).</param>
		/// <param name="roll">Rotation around the Z-axis (tilt).</param>
		public void Rotate(float yaw, float pitch, float roll)
		{
			// Create rotation quaternions for each axis
			var yawRotation = Quaternion.CreateFromAxisAngle(Vector3.Up, yaw * RotationSpeed);
			var pitchRotation = Quaternion.CreateFromAxisAngle(Vector3.Right, pitch * RotationSpeed);
			var rollRotation = Quaternion.CreateFromAxisAngle(Vector3.Forward, roll * RotationSpeed);

			// Apply rotations in a specific order (e.g., yaw -> pitch -> roll)
			// Note: Order matters for quaternion concatenation.
			// Multiplying a quaternion by another applies the second rotation relative to the first.
			// Rotation = Rotation * yawRotation * pitchRotation * rollRotation; // Global rotation
			// For local rotation, apply the new rotation before the existing one:
			Rotation = yawRotation * Rotation * pitchRotation * rollRotation;

			Rotation.Normalize(); // Keep the quaternion normalized to prevent drift
			UpdateViewMatrix();
		}

		/// <summary>
		/// Gets the camera's forward direction vector.
		/// </summary>
		public Vector3 Forward => Vector3.Transform(Vector3.Forward, Rotation); // Or Vector3.Transform(-Vector3.UnitZ, Rotation) if camera's forward is -Z

		/// <summary>
		/// Gets the camera's right direction vector.
		/// </summary>
		public Vector3 Right => Vector3.Transform(Vector3.Right, Rotation);

		/// <summary>
		/// Gets the camera's up direction vector.
		/// </summary>
		public Vector3 Up => Vector3.Transform(Vector3.Up, Rotation);

		/// <summary>
		/// Example of how to integrate camera input in your Game's Update method.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		public void HandleInput(GameTime gameTime)
		{
			var currentKeyboardState = Keyboard.GetState();
			var currentMouseState = Mouse.GetState();

			// --- Movement ---
			var movement = Vector3.Zero;
			if (currentKeyboardState.IsKeyDown(Keys.I)) // Forward
				movement += Vector3.Forward; // Local Z-axis
			if (currentKeyboardState.IsKeyDown(Keys.K)) // Backward
				movement += Vector3.Backward;
			if (currentKeyboardState.IsKeyDown(Keys.J)) // Strafe Left
				movement += Vector3.Left; // Local X-axis
			if (currentKeyboardState.IsKeyDown(Keys.L)) // Strafe Right
				movement += Vector3.Right;
			if (currentKeyboardState.IsKeyDown(Keys.Space)) // Up
				movement += Vector3.Up; // Local Y-axis
			if (currentKeyboardState.IsKeyDown(Keys.OemQuestion)) // Down
				movement += Vector3.Down;

			MovementSpeed = DefaultMovementSpeed;
			if (currentKeyboardState.IsKeyDown(Keys.LeftShift) || currentKeyboardState.IsKeyDown(Keys.RightShift))
			{
				MovementSpeed = DefaultMovementSpeed * BoostMultiplier;
			}

			if (movement != Vector3.Zero)
			{
				movement.Normalize(); // Normalize to ensure consistent speed in all directions
				Move(movement * (float)gameTime.ElapsedGameTime.TotalSeconds);
			}

			// --- Rotation (Keyboard) ---
			float yaw = 0f, pitch = 0f, roll = 0f;
			if (currentKeyboardState.IsKeyDown(Keys.A)) // Yaw left
				yaw = 1f;
			if (currentKeyboardState.IsKeyDown(Keys.D)) // Yaw right
				yaw -= 1f;
			if (currentKeyboardState.IsKeyDown(Keys.S)) // Pitch up
				pitch += 1f;
			if (currentKeyboardState.IsKeyDown(Keys.W)) // Pitch down
				pitch -= 1f;
			if (currentKeyboardState.IsKeyDown(Keys.Q)) // Roll left
				roll -= 1f;
			if (currentKeyboardState.IsKeyDown(Keys.E)) // Roll right
				roll += 1f;

			if (yaw != 0f || pitch != 0f || roll != 0f)
			{
				Rotate(yaw, pitch, roll);
			}

			// --- Rotation (Mouse Look) ---
			if (currentMouseState.RightButton == ButtonState.Pressed)
			{
				// Get the center of the screen
				var centerX = GraphicsDevice.Viewport.Width / 2;
				var centerY = GraphicsDevice.Viewport.Height / 2;

				// Calculate mouse delta from center
				float deltaX = currentMouseState.X - centerX;
				float deltaY = currentMouseState.Y - centerY;

				if (previousMouseState != default && previousMouseState.RightButton != ButtonState.Pressed)
				{
					deltaX = 0.0000000001f;
					deltaY = 0.0000000001f;
				}

				const float sensitivity = 0.2f;

				if (deltaX != 0 || deltaY != 0)
				{
					// Apply rotation based on mouse delta
					// deltaX affects yaw (Y-axis rotation), deltaY affects pitch (X-axis rotation)
					Rotate(-deltaX * sensitivity, -deltaY * sensitivity, 0); // Negative deltaX for typical mouse look (right mouse moves right)

					// Reset mouse to center of the screen to prevent it from going out of bounds
					Mouse.SetPosition(centerX, centerY);
				}
			}
			previousMouseState = currentMouseState;
		}
	}
}
