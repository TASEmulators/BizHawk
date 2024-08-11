using System.IO;
using System.Runtime.InteropServices;

// ReSharper disable MemberCanBePrivate.Local

namespace BizHawk.Common
{
	public sealed class Zstd : IDisposable
	{
		private sealed class ZstdCompressionStreamContext : IDisposable
		{
			public readonly IntPtr Zcs;

			public readonly byte[] InputBuffer;
			public GCHandle InputHandle;
			public LibZstd.StreamBuffer Input;

			public readonly byte[] OutputBuffer;
			public GCHandle OutputHandle;
			public LibZstd.StreamBuffer Output;

			// TODO: tweak these sizes

			// 4 MB input buffer
			public const int INPUT_BUFFER_SIZE = 1024 * 1024 * 4;
			// 1 MB output buffer
			public const int OUTPUT_BUFFER_SIZE = 1024 * 1024 * 1;

			public bool InUse;

			public ZstdCompressionStreamContext()
			{
				Zcs = LibZstd.CreateCStream();

				InputBuffer = new byte[INPUT_BUFFER_SIZE];
				InputHandle = GCHandle.Alloc(InputBuffer, GCHandleType.Pinned);

				Input = new()
				{
					Ptr = InputHandle.AddrOfPinnedObject(),
					Size = 0,
					Pos = 0,
				};

				OutputBuffer = new byte[OUTPUT_BUFFER_SIZE];
				OutputHandle = GCHandle.Alloc(OutputBuffer, GCHandleType.Pinned);

				Output = new()
				{
					Ptr = OutputHandle.AddrOfPinnedObject(),
					Size = OUTPUT_BUFFER_SIZE,
					Pos = 0,
				};

				InUse = false;
			}

			private bool _disposed;

			public void Dispose()
			{
				if (!_disposed)
				{
					LibZstd.FreeCStream(Zcs);
					InputHandle.Free();
					OutputHandle.Free();
					_disposed = true;
				}
			}

			public void InitContext(int compressionLevel)
			{
				if (InUse)
				{
					throw new InvalidOperationException("Cannot init context still in use!");
				}

				LibZstd.InitCStream(Zcs, compressionLevel);
				Input.Size = Input.Pos = Output.Pos = 0;
				InUse = true;
			}
		}

		private sealed class ZstdCompressionStream : Stream
		{
			private readonly Stream _baseStream;
			private readonly ZstdCompressionStreamContext _ctx;

			public ZstdCompressionStream(Stream baseStream, ZstdCompressionStreamContext ctx)
			{
				_baseStream = baseStream;
				_ctx = ctx;
			}

			private bool _disposed;

			protected override void Dispose(bool disposing)
			{
				if (disposing && !_disposed)
				{
					Flush();
					while (true)
					{
						var n = LibZstd.EndStream(_ctx.Zcs, ref _ctx.Output);
						CheckError(n);
						InternalFlush();
						if (n == 0)
						{
							break;
						}
					}
					_ctx.InUse = false;
					_disposed = true;
				}

				base.Dispose(disposing);
			}

			public override bool CanRead
				=> false;

			public override bool CanSeek
				=> false;

			public override bool CanWrite
				=> true;

			public override long Length
				=> throw new NotImplementedException();

			public override long Position 
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			private void InternalFlush()
			{
				_baseStream.Write(_ctx.OutputBuffer, 0, (int)_ctx.Output.Pos);
				_ctx.Output.Pos = 0;
			}

			public override void Flush()
			{
				while (_ctx.Input.Pos < _ctx.Input.Size)
				{
					CheckError(LibZstd.CompressStream(_ctx.Zcs, ref _ctx.Output, ref _ctx.Input));
					while (true)
					{
						if (_ctx.Output.Pos == ZstdCompressionStreamContext.OUTPUT_BUFFER_SIZE)
						{
							InternalFlush();
						}

						var n = LibZstd.FlushStream(_ctx.Zcs, ref _ctx.Output);
						CheckError(n);
						if (n == 0)
						{
							InternalFlush();
							break;
						}
					}
				}

				_ctx.Input.Pos = _ctx.Input.Size = 0;
			}

			public override int Read(byte[] buffer, int offset, int count)
				=> throw new NotImplementedException();

			public override long Seek(long offset, SeekOrigin origin)
				=> throw new NotImplementedException();

			public override void SetLength(long value)
				=> throw new NotImplementedException();

			public override void Write(byte[] buffer, int offset, int count)
			{
				while (count > 0)
				{
					if (_ctx.Input.Size == ZstdCompressionStreamContext.INPUT_BUFFER_SIZE)
					{
						Flush();
					}

					var n = Math.Min(count, (int)(ZstdCompressionStreamContext.INPUT_BUFFER_SIZE - _ctx.Input.Size));
					Marshal.Copy(buffer, offset, _ctx.Input.Ptr + (int)_ctx.Input.Size, n);
					offset += n;
					_ctx.Input.Size += (uint)n;
					count -= n;
				}
			}
		}

		private sealed class ZstdDecompressionStreamContext : IDisposable
		{
			public readonly IntPtr Zds;

			public readonly byte[] InputBuffer;
			public GCHandle InputHandle;
			public LibZstd.StreamBuffer Input;

			public readonly byte[] OutputBuffer;
			public GCHandle OutputHandle;
			public LibZstd.StreamBuffer Output;

			// TODO: tweak these sizes

			// 1 MB input buffer
			public const int INPUT_BUFFER_SIZE = 1024 * 1024 * 1;
			// 4 MB output buffer
			public const int OUTPUT_BUFFER_SIZE = 1024 * 1024 * 4;

			public bool InUse;

			public ZstdDecompressionStreamContext()
			{
				Zds = LibZstd.CreateDStream();

				InputBuffer = new byte[INPUT_BUFFER_SIZE];
				InputHandle = GCHandle.Alloc(InputBuffer, GCHandleType.Pinned);

				Input = new()
				{
					Ptr = InputHandle.AddrOfPinnedObject(),
					Size = 0,
					Pos = 0,
				};

				OutputBuffer = new byte[OUTPUT_BUFFER_SIZE];
				OutputHandle = GCHandle.Alloc(OutputBuffer, GCHandleType.Pinned);

				Output = new()
				{
					Ptr = OutputHandle.AddrOfPinnedObject(),
					Size = OUTPUT_BUFFER_SIZE,
					Pos = 0,
				};

				InUse = false;
			}

			private bool _disposed;

			public void Dispose()
			{
				if (!_disposed)
				{
					LibZstd.FreeDStream(Zds);
					InputHandle.Free();
					OutputHandle.Free();
					_disposed = true;
				}
			}

			public void InitContext()
			{
				if (InUse)
				{
					throw new InvalidOperationException("Cannot init context still in use!");
				}

				LibZstd.InitDStream(Zds);
				Input.Size = Input.Pos = Output.Pos = 0;
				InUse = true;
			}
		}

		private sealed class ZstdDecompressionStream : Stream
		{
			private readonly Stream _baseStream;
			private readonly ZstdDecompressionStreamContext _ctx;

			public ZstdDecompressionStream(Stream baseStream, ZstdDecompressionStreamContext ctx)
			{
				_baseStream = baseStream;
				_ctx = ctx;
			}

			private bool _disposed;

			protected override void Dispose(bool disposing)
			{
				if (disposing && !_disposed)
				{
					_ctx.InUse = false;
					_disposed = true;
				}

				base.Dispose(disposing);
			}

			public override bool CanRead
				=> true;

			public override bool CanSeek
				=> false;

			public override bool CanWrite
				=> false;

			public override long Length
				=> _baseStream.Length; // FIXME: this wrong but this is only used in a > 0 check so I guess it works?

			public override long Position
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			public override void Flush()
				=> throw new NotImplementedException();

			private ulong _outputConsumed;

			public override int Read(byte[] buffer, int offset, int count)
			{
				var n = count;
				while (n > 0)
				{
					var inputConsumed = _baseStream.Read(_ctx.InputBuffer,
						(int)_ctx.Input.Size, (int)(ZstdDecompressionStreamContext.INPUT_BUFFER_SIZE - _ctx.Input.Size));
					_ctx.Input.Size += (uint)inputConsumed;
					// avoid interop in case compression cannot be done
					if (_ctx.Output.Pos < ZstdDecompressionStreamContext.OUTPUT_BUFFER_SIZE
						&& _ctx.Input.Pos < _ctx.Input.Size)
					{
						CheckError(LibZstd.DecompressStream(_ctx.Zds, ref _ctx.Output, ref _ctx.Input));
					}
					var outputToConsume = Math.Min(n, (int)(_ctx.Output.Pos - _outputConsumed));
					Marshal.Copy(_ctx.Output.Ptr + (int)_outputConsumed, buffer, offset, outputToConsume);
					_outputConsumed += (ulong)outputToConsume;
					offset += outputToConsume;
					n -= outputToConsume;

					if (_outputConsumed == ZstdDecompressionStreamContext.OUTPUT_BUFFER_SIZE)
					{
						// all the buffer is consumed, kick these back to the beginning
						_outputConsumed = 0;
						_ctx.Output.Pos = 0;
					}

					if (_ctx.Input.Pos == ZstdDecompressionStreamContext.INPUT_BUFFER_SIZE)
					{
						// ditto here
						_ctx.Input.Pos = _ctx.Input.Size = 0;
					}

					// couldn't consume anything, get out
					// (decompression must be complete at this point)
					if (inputConsumed == 0 && outputToConsume == 0)
					{
						break;
					}
				}

				return count - n;
			}

			public override long Seek(long offset, SeekOrigin origin)
				=> throw new NotImplementedException();

			public override void SetLength(long value)
				=> throw new NotImplementedException();

			public override void Write(byte[] buffer, int offset, int count)
				=> throw new NotImplementedException();
		}

		public static int MinCompressionLevel { get; }
		public static int MaxCompressionLevel { get; }

		static Zstd()
		{
			MinCompressionLevel = LibZstd.MinCLevel();
			MaxCompressionLevel = LibZstd.MaxCLevel();
		}

		private ZstdCompressionStreamContext? _compressionStreamContext;
		private ZstdDecompressionStreamContext? _decompressionStreamContext;

		private bool _disposed;

		public void Dispose()
		{
			if (!_disposed)
			{
				_compressionStreamContext?.Dispose();
				_decompressionStreamContext?.Dispose();
				_disposed = true;
			}
		}

		private static void CheckError(nuint code)
		{
			if (LibZstd.IsError(code) != 0)
			{
				throw new Exception($"ZSTD ERROR: {Marshal.PtrToStringAnsi(LibZstd.GetErrorName(code))}");
			}
		}

		/// <summary>
		/// Creates a zstd compression stream.
		/// This stream uses a shared context as to avoid buffer allocation spam.
		/// It is absolutely important to call Dispose() / use using on returned stream.
		/// If this is not done, the shared context will remain in use,
		/// and the proceeding attempt to initialize it will throw.
		/// Also, of course, do not attempt to create multiple streams at once.
		/// Only 1 stream at a time is allowed per Zstd instance.
		/// </summary>
		/// <param name="stream">the stream to write compressed data</param>
		/// <param name="compressionLevel">compression level, bounded by MinCompressionLevel and MaxCompressionLevel</param>
		/// <returns>zstd compression stream</returns>
		/// <exception cref="ArgumentOutOfRangeException">compressionLevel is too small or too big</exception>
		public Stream CreateZstdCompressionStream(Stream stream, int compressionLevel)
		{
			if (compressionLevel < MinCompressionLevel || compressionLevel > MaxCompressionLevel)
			{
				throw new ArgumentOutOfRangeException(nameof(compressionLevel));
			}

			_compressionStreamContext ??= new();
			_compressionStreamContext.InitContext(compressionLevel);
			return new ZstdCompressionStream(stream, _compressionStreamContext);
		}

		/// <summary>
		/// Creates a zstd decompression stream.
		/// This stream uses a shared context as to avoid buffer allocation spam.
		/// It is absolutely important to call Dispose() / use using on returned stream.
		/// If this is not done, the shared context will remain in use,
		/// and the proceeding attempt to initialize it will throw.
		/// Also, of course, do not attempt to create multiple streams at once.
		/// Only 1 stream at a time is allowed per Zstd instance.
		/// </summary>
		/// <param name="stream">a stream with zstd compressed data to decompress</param>
		/// <returns>zstd decompression stream</returns>
		public Stream CreateZstdDecompressionStream(Stream stream)
		{
			_decompressionStreamContext ??= new();
			_decompressionStreamContext.InitContext();
			return new ZstdDecompressionStream(stream, _decompressionStreamContext);
		}

		/// <summary>
		/// Decompresses src stream and returns a memory stream with the decompressed contents.
		/// Context creation and disposing is handled internally in this function, unlike the non-static ones.
		/// This is useful in cases where you are not doing repeated decompressions,
		/// so keeping a Zstd instance around is not as useful.
		/// </summary>
		/// <param name="src">stream with zstd compressed data to decompress</param>
		/// <returns>MemoryStream with the decompressed contents of src</returns>
		/// <exception cref="InvalidOperationException">src does not have a ZSTD header</exception>
		public static MemoryStream DecompressZstdStream(Stream src)
		{
			// check for ZSTD header
			var tmp = new byte[4];
			if (src.Read(tmp, 0, 4) != 4)
			{
				throw new InvalidOperationException("Unexpected end of stream");
			}
			if (tmp[0] != 0x28 || tmp[1] != 0xB5 || tmp[2] != 0x2F || tmp[3] != 0xFD)
			{
				throw new InvalidOperationException("ZSTD header not present");
			}
			src.Seek(0, SeekOrigin.Begin);

			using var dctx = new ZstdDecompressionStreamContext();
			dctx.InitContext();
			using var dstream = new ZstdDecompressionStream(src, dctx);
			var ret = new MemoryStream();
			dstream.CopyTo(ret);
			return ret;
		}
	}
}
