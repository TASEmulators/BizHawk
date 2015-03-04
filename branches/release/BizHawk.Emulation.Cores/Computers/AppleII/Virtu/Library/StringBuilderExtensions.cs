using System;
using System.Globalization;
using System.Text;

namespace Jellyfish.Library
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder AppendHex(this StringBuilder builder, short value) // little endian
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            return builder.AppendFormat(CultureInfo.InvariantCulture, "{0:X2}{1:X2}", value & 0xFF, value >> 8);
        }

        public static StringBuilder AppendHex(this StringBuilder builder, int value) // little endian
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            return builder.AppendFormat(CultureInfo.InvariantCulture, "{0:X2}{1:X2}{2:X2}{3:X2}", value & 0xFF, (value >> 8) & 0xFF, (value >> 16) & 0xFF, value >> 24);
        }

        public static StringBuilder AppendWithoutGarbage(this StringBuilder builder, int value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            if (value < 0)
            {
                builder.Append('-');
            }

            int index = builder.Length;
            do
            {
                builder.Insert(index, Digits, (value % 10) + 9, 1);
                value /= 10;
            }
            while (value != 0);

            return builder;
        }

        private static readonly char[] Digits = new char[] { '9', '8', '7', '6', '5', '4', '3', '2', '1', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
    }
}
