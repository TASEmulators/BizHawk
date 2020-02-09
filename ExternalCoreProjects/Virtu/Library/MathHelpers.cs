namespace Jellyfish.Library
{
	internal static class MathHelpers
	{
		internal static int Clamp(this int value, int min, int max)
		{
			return (value < min) ? min : (value > max) ? max : value;
		}
	}
}
