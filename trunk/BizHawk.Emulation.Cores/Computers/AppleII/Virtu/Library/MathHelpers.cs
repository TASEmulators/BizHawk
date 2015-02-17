namespace Jellyfish.Library
{
    public static class MathHelpers
    {
        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static int ClampByte(int value)
        {
            return Clamp(value, byte.MinValue, byte.MaxValue);
        }
    }
}
