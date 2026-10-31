#if !NET8_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
	public static class MemoryMarshalExtensions
	{
		extension(MemoryMarshal)
		{
			public static void Write<T>(Span<byte> destination, in T value)
				where T : struct
				=> MemoryMarshal.Write(destination, ref Unsafe.AsRef(in value));
		}
	}
}
#endif
