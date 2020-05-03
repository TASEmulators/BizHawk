#nullable enable

namespace BizHawk.Client.Common
{
	public static class PolarRectConversion
	{
		/// <param name="r">radial displacement in range <c>0..181</c> (this is not asserted)</param>
		/// <param name="θ">angle (in degrees) in range <c>0..359</c> (this is not asserted)</param>
		/// <returns>rectangular (Cartesian) coordinates <c>(x, y)</c>. <c>x</c> and/or <c>y</c> may be outside the range <c>-128..127</c>.</returns>
		/// <seealso cref="RectToPolarLookup"/>
		public static (short X, short Y) PolarToRectLookup(ushort r, ushort θ) => (PolarRectConversionData._rθ2x[r, θ], PolarRectConversionData._rθ2y[r, θ]);

		/// <param name="x">horizontal component of rectangular (Cartesian) coordinates <c>(x, y)</c>, in range <c>-128..127</c> (this is not asserted)</param>
		/// <param name="y">vertical component, as <paramref name="x"/></param>
		/// <returns>polar coordinates <c>(r, θ)</c> where <c>r</c> is radial displacement in range <c>0..181</c> and <c>θ</c> is angle (in degrees) in range <c>0..359</c> (from <c>+x</c> towards <c>+y</c>)</returns>
		/// <seealso cref="PolarToRectLookup"/>
		public static (ushort R, ushort Θ) RectToPolarLookup(sbyte x, sbyte y) => unchecked((PolarRectConversionData._xy2r[(byte) x, (byte) y], PolarRectConversionData._xy2θ[(byte) x, (byte) y]));
	}
}
