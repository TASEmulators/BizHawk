using System;

using DoublePair = System.Tuple<double, double>;

namespace BizHawk.Client.Common
{
	public static class PolarRectConversion
	{
		/// <param name="θ">angle in degrees</param>
		/// <returns>rectangular (Cartesian) coordinates (x, y)</returns>
		public static DoublePair PolarDegToRect(double r, double θ) => PolarRadToRect(r, θ * Math.PI / 180);

		/// <param name="θ">angle in radians</param>
		/// <returns>rectangular (Cartesian) coordinates (x, y)</returns>
		public static DoublePair PolarRadToRect(double r, double θ) => new DoublePair(r * Math.Cos(θ), r * Math.Sin(θ));

		/// <returns>polar coordinates (r, θ) where θ is in degrees</returns>
		public static DoublePair RectToPolarDeg(double x, double y) => new DoublePair(Math.Sqrt(x * x + y * y), Math.Atan2(y, x) * 180 / Math.PI);
	}
}