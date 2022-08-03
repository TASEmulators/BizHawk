using System;
using System.IO;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Common;

namespace BizHawk.Client.Common.Zstd
{
	public class Zstd : IDisposable
	{
		private class ZstdCompressionStream : Stream
		{
			private readonly Stream _baseStream;
			private readonly IntPtr _zcs;

			private readonly byte[] _inputBuffer;
			private readonly GCHandle _inputHandle;
			private LibZstd.Buffer _input;

			private readonly byte[] _outputBuffer;
			private readonly GCHandle _outputHandle;
			private LibZstd.Buffer _output;

			// TODO: tweak these sizes

			// 4 MB input buffer
			private const int INPUT_BUFFER_SIZE = 1024 * 1024 * 4;
			// 1 MB output buffer
			private const int OUTPUT_BUFFER_SIZE = 1024 * 1024 * 1;

			public ZstdCompressionStream(Stream baseStream, int compressionLevel)
			{
				_baseStream = baseStream;

				_zcs = _lib.ZSTD_createCStream();
				_lib.ZSTD_initCStream(_zcs, compressionLevel);

				_inputBuffer = new byte[INPUT_BUFFER_SIZE];
				_inputHandle = GCHandle.Alloc(_inputBuffer, GCHandleType.Pinned);

				_input = new()
				{
					Ptr = _inputHandle.AddrOfPinnedObject(),
					Size = 0,
					Pos = 0,
				};

				_outputBuffer = new byte[OUTPUT_BUFFER_SIZE];
				_outputHandle = GCHandle.Alloc(_outputBuffer, GCHandleType.Pinned);

				_output = new()
				{
					Ptr = _outputHandle.AddrOfPinnedObject(),
					Size = OUTPUT_BUFFER_SIZE,
					Pos = 0,
				};
			}

			~ZstdCompressionStream()
			{
				Dispose();
			}

			private bool _disposed = false;

			protected override void Dispose(bool disposing)
			{
				if (!_disposed)
				{
					Flush();
					while (true)
					{
						var n = _lib.ZSTD_endStream(_zcs, ref _output);
						CheckError(n);
						InternalFlush();
						if (n == 0)
						{
							break;
						}
					}
					_lib.ZSTD_freeCStream(_zcs);
					_inputHandle.Free();
					_outputHandle.Free();
					GC.SuppressFinalize(this);
					_disposed = true;
				}
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
				_baseStream.Write(_outputBuffer, 0, (int)_output.Pos);
				_output.Pos = 0;
			}

			public override void Flush()
			{
				while (_input.Pos < _input.Size)
				{
					CheckError(_lib.ZSTD_compressStream(_zcs, ref _output, ref _input));
					while (true)
					{
						if (_output.Pos == OUTPUT_BUFFER_SIZE)
						{
							InternalFlush();
						}

						var n = _lib.ZSTD_flushStream(_zcs, ref _output);
						CheckError(n);
						if (n == 0)
						{
							InternalFlush();
							break;
						}
					}
				}

				_input.Pos = _input.Size = 0;
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
					if (_input.Size == INPUT_BUFFER_SIZE)
					{
						Flush();
					}

					var n = count > (int)(INPUT_BUFFER_SIZE - _input.Size) ? (int)(INPUT_BUFFER_SIZE - _input.Size) : count;
					Marshal.Copy(buffer, offset, _input.Ptr + (int)_input.Size, n);
					offset += n;
					_input.Size += (ulong)n;
					count -= n;
				}
			}
		}

		private class ZstdDecompressionStream : Stream
		{
			private readonly Stream _baseStream;
			private readonly IntPtr _zds;

			private readonly byte[] _inputBuffer;
			private readonly GCHandle _inputHandle;
			private LibZstd.Buffer _input;

			private readonly byte[] _outputBuffer;
			private readonly GCHandle _outputHandle;
			private LibZstd.Buffer _output;

			// TODO: tweak these sizes

			// 1 MB input buffer
			private const int INPUT_BUFFER_SIZE = 1024 * 1024 * 1;
			// 4 MB output buffer
			private const int OUTPUT_BUFFER_SIZE = 1024 * 1024 * 4;

			public ZstdDecompressionStream(Stream baseStream)
			{
				_baseStream = baseStream;

				_zds = _lib.ZSTD_createDStream();
				_lib.ZSTD_initDStream(_zds);

				_inputBuffer = new byte[INPUT_BUFFER_SIZE];
				_inputHandle = GCHandle.Alloc(_inputBuffer, GCHandleType.Pinned);

				_input = new()
				{
					Ptr = _inputHandle.AddrOfPinnedObject(),
					Size = 0,
					Pos = 0,
				};

				_outputBuffer = new byte[OUTPUT_BUFFER_SIZE];
				_outputHandle = GCHandle.Alloc(_outputBuffer, GCHandleType.Pinned);

				_output = new()
				{
					Ptr = _outputHandle.AddrOfPinnedObject(),
					Size = OUTPUT_BUFFER_SIZE,
					Pos = 0,
				};
			}

			~ZstdDecompressionStream()
			{
				Dispose();
			}

			private bool _disposed = false;

			protected override void Dispose(bool disposing)
			{
				if (!_disposed)
				{
					_lib.ZSTD_freeDStream(_zds);
					_inputHandle.Free();
					_outputHandle.Free();
					GC.SuppressFinalize(this);
					_disposed = true;
				}
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

			private ulong _outputConsumed = 0;

			public override int Read(byte[] buffer, int offset, int count)
			{
				var n = count;
				while (n > 0)
				{
					var inputConsumed = _baseStream.Read(_inputBuffer, (int)_input.Size, (int)(INPUT_BUFFER_SIZE - _input.Size));
					_input.Size += (ulong)inputConsumed;
					CheckError(_lib.ZSTD_decompressStream(_zds, ref _output, ref _input));
					var outputToConsume = n > (int)(_output.Pos - _outputConsumed) ? (int)(_output.Pos - _outputConsumed) : n;
					Marshal.Copy(_output.Ptr + (int)_outputConsumed, buffer, offset, outputToConsume);
					_outputConsumed += (ulong)outputToConsume;
					offset += outputToConsume;
					if (_outputConsumed == OUTPUT_BUFFER_SIZE)
					{
						// all the buffer is consumed, kick these back to the beginning
						_output.Pos = _outputConsumed = 0;
					}
					n -= outputToConsume;

					if (_input.Pos == INPUT_BUFFER_SIZE)
					{
						// ditto here
						_input.Pos = _input.Size = 0;
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

		private static readonly LibZstd _lib;

		public static int MinCompressionLevel { get; }

		public static int MaxCompressionLevel { get; }

		static Zstd()
		{
			var resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "libzstd.so" : "libzstd.dll", hasLimitedLifetime: false);
			_lib = BizInvoker.GetInvoker<LibZstd>(resolver, CallingConventionAdapters.Native);

			MinCompressionLevel = _lib.ZSTD_minCLevel();
			MaxCompressionLevel = _lib.ZSTD_maxCLevel();
		}

		private bool _disposed = false;
		private readonly IntPtr _CCtx;
		private readonly IntPtr _DCtx;
		// these functions are not thread safe anyways, so...
		private byte[] _compressionBuffer = Array.Empty<byte>();

		public Zstd()
		{
			_CCtx = _lib.ZSTD_createCCtx();
			_DCtx = _lib.ZSTD_createDCtx();
		}

		~Zstd()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_lib.ZSTD_freeCCtx(_CCtx);
				_lib.ZSTD_freeDCtx(_DCtx);
				GC.SuppressFinalize(this);
				_disposed = true;
			}
		}

		private static void CheckError(ulong code)
		{
			if (_lib.ZSTD_isError(code) != 0)
			{
				throw new Exception($"ZSTD ERROR: {Marshal.PtrToStringAnsi(_lib.ZSTD_getErrorName(code))}");
			}
		}

		public byte[] CompressArray(byte[] src, int compressionLevel)
		{
			var compressBound = (int)_lib.ZSTD_compressBound((ulong)src.Length);
			if (_compressionBuffer.Length < compressBound)
			{
				_compressionBuffer = new byte[compressBound];
			}

			var sz = _lib.ZSTD_compressCCtx(_CCtx, _compressionBuffer, (ulong)_compressionBuffer.Length, src, (ulong)src.Length, compressionLevel);

			CheckError(sz);

			var ret = new byte[sz];
			Buffer.BlockCopy(_compressionBuffer, 0, ret, 0, (int)sz);
			return ret;
		}

		public byte[] DecompressArray(byte[] src)
		{
			var sz = _lib.ZSTD_getFrameContentSize(src, (ulong)src.Length);
			if (sz == unchecked((ulong)-1))
			{
				throw new Exception($"ZSTD ERROR: ZSTD_CONTENTSIZE_UNKNOWN");
			}
			else if (sz == unchecked((ulong)-2))
			{
				throw new Exception($"ZSTD ERROR: ZSTD_CONTENTSIZE_ERROR");
			}

			var ret = new byte[sz];
			var code = _lib.ZSTD_decompressDCtx(_DCtx, ret, sz, src, (ulong)src.Length);

			CheckError(code);

			return ret;
		}

		public static Stream CreateZstdCompressionStream(Stream stream, int compressionLevel)
			=> new ZstdCompressionStream(stream, compressionLevel);

		public static Stream CreateZstdDecompressionStream(Stream stream)
			=> new ZstdDecompressionStream(stream);
	}
}
