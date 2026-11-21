using System.IO;
#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER)
using System.Runtime.InteropServices;
#endif

namespace BizHawk.Common
{
	/// <summary>
	/// TODO: Switch to dotnet core and remove this junkus
	/// </summary>
	public interface ISpanStream
	{
#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER)
		void Write(ReadOnlySpan<byte> buffer);
		int Read(Span<byte> buffer);
#endif
	}
	public static class SpanStream
	{
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
		public static Stream GetOrBuild(Stream s)
			=> s;
#else
		/// <returns>a stream in spanstream mode, or a newly-created wrapper which provides that functionality</returns>
		public static ISpanStream GetOrBuild(Stream s)
		{
			return s as ISpanStream
				?? new SpanStreamAdapter(s);
		}
		private class SpanStreamAdapter : ISpanStream
		{
			public SpanStreamAdapter(Stream stream)
			{
				_stream = stream;
			}
			private byte[] _buffer = Array.Empty<byte>();
			private readonly Stream _stream;
			public unsafe int Read(Span<byte> buffer)
			{
				if (buffer.IsEmpty) return 0;
				if (buffer.Length > _buffer.Length)
				{
					_buffer = new byte[buffer.Length];
				}
				var n = _stream.Read(_buffer, 0, buffer.Length);
				fixed(byte* p = buffer)
				{
					Marshal.Copy(_buffer, 0, (IntPtr)p, n);
				}
				return n;
			}

			public unsafe void Write(ReadOnlySpan<byte> buffer)
			{
				if (buffer.IsEmpty) return;
				if (buffer.Length > _buffer.Length)
				{
					_buffer = new byte[buffer.Length];
				}
				fixed(byte* p = buffer)
				{
					Marshal.Copy((IntPtr)p, _buffer, 0, buffer.Length);
				}
				_stream.Write(_buffer, 0, buffer.Length);
			}
		}
#endif
	}
}
