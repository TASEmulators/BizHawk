namespace Jellyfish.Library
{
	internal static class MathHelpers
	{
		public static int Clamp(int value, int min, int max)
		{
			return (value < min) ? min : (value > max) ? max : value;
		}
	}
}
