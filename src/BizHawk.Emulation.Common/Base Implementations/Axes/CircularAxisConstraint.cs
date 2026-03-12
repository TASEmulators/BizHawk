namespace BizHawk.Emulation.Common
{
	public sealed class CircularAxisConstraint : AxisConstraint
	{
		public string? Class { get; }

		private readonly float Magnitude;

		public string PairedAxis { get; }

		public CircularAxisConstraint(string @class, string pairedAxis, float magnitude)
		{
			Class = @class;
			Magnitude = magnitude;
			PairedAxis = pairedAxis;
		}

		public (int X, int Y) ApplyTo(int rawX, int rawY)
		{
			var xVal = (double) rawX;
			var yVal = (double) rawY;
			var length = Math.Sqrt(xVal * xVal + yVal * yVal);
			var ratio = Magnitude / length;
			return ratio < 1.0
				? ((int) Math.Round(xVal * ratio), (int) Math.Round(yVal * ratio))
				: ((int) xVal, (int) yVal);
		}
	}
}
