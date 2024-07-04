using System.Numerics;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.N3DS
{
	public class _3DSMotionEmu
	{
		// update per frame
		private const float UPDATE_PERIOD = (float)(268111856.0 / 4481136.0);

		// todo: make these adjustable
		private const float SENSITIVITY = 0.01f;
		private const float TILT_CLAMP = 90.0f;

		public void Update(bool tilting, int x, int y)
		{
			if (!IsTilting && tilting)
			{
				TiltOrigin = new(x, y);
			}

			IsTilting = tilting;

			if (tilting)
			{
				var tiltMove = new Vector2(x, y) - TiltOrigin;
				if (tiltMove == default)
				{
					TiltAngle = 0;
				}
				else
				{
					TiltDirection = tiltMove;
					var tiltMoveNormalized = tiltMove.Length();
					TiltDirection /= tiltMoveNormalized;
					TiltAngle = tiltMoveNormalized * SENSITIVITY;
					TiltAngle = Math.Max(TiltAngle, 0.0f);
					TiltAngle = Math.Min(TiltAngle, (float)Math.PI * TILT_CLAMP / 180.0f);
				}
			}
			else
			{
				TiltAngle = 0;
			}

			var tiltQ = Quaternion.CreateFromAxisAngle(new(-TiltDirection.Y, 0.0f, TiltDirection.X), TiltAngle);
			var conTiltQ = Quaternion.Conjugate(tiltQ);

			var angularRateQ = (tiltQ - PrevTiltQuaternion) * conTiltQ;
			var angularRate = new Vector3(angularRateQ.X, angularRateQ.Y, angularRateQ.Z) * 2;
			angularRate *= UPDATE_PERIOD / (float)Math.PI * 180;

			Gravity = Vector3.Transform(new(0, -1, 0), conTiltQ);
			AngularRate = Vector3.Transform(angularRate, conTiltQ);

			PrevTiltQuaternion = tiltQ;
		}

		public bool IsTilting;
		public Vector2 TiltOrigin;
		public Vector2 TiltDirection;
		public float TiltAngle;
		public Quaternion PrevTiltQuaternion;
		public Vector3 Gravity;
		public Vector3 AngularRate;
	}
}
