using System;
using System.IO;
using System.IO.Compression;
using BizHawk.BizInvoke;
using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public class ZwinderBuffer : IDisposable
	{
		/*
		Main goals:
		1. No copies, ever.  States are deposited directly to, and read directly from, one giant ring buffer.
			As a consequence, there is no multi-threading because there is nothing to thread.
		2. Support for arbitrary and changeable state sizes.  Frequency is calculated dynamically.
		3. No delta compression.  Keep it simple.  If there are cores that benefit heavily from delta compression, we should
			maintain a separate rewinder alongside this one that is customized for those cores.
		*/

		const string baseStatePath = "tastudiostates";
		bool disposed = false;
		static int count = 0;
		int _id;

		public ZwinderBuffer(IRewindSettings settings)
		{
			_id = count;
			count++;

			// delete any old files that may have not been properly deleted
			if (_id == 0 && Directory.Exists(baseStatePath))
				Directory.Delete(baseStatePath, true);
			Directory.CreateDirectory(baseStatePath);

			long targetSize = settings.BufferSize * 1024 * 1024;
			if (settings.TargetFrameLength < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(settings.TargetFrameLength));
			}

			Size = 1L << (int)Math.Floor(Math.Log(targetSize, 2));
			_sizeMask = Size - 1;
			if (settings.UseDrive)
			{
				_fStream = new FileStream(baseStatePath + "/" + _id, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
				_fStream.SetLength(Size);
			}
			else
			{
				_buffer = new MemoryBlock((ulong)Size);
				_buffer.Protect(_buffer.Start, _buffer.Size, MemoryBlock.Protection.RW);
			}
			_targetFrameLength = settings.TargetFrameLength;
			_states = new StateInfo[STATEMASK + 1];
			_useCompression = settings.UseCompression;
		}

		~ZwinderBuffer()
		{
			Dispose();
		}
		public void Dispose()
		{
			if (!disposed)
			{
				_fStream?.Dispose();
				_buffer?.Dispose();

				if (File.Exists(baseStatePath + "/" + _id))
					File.Delete(baseStatePath + "/" + _id);

				disposed = true;
			}
		}


		/// <summary>
		/// Number of states that could be in the state ringbuffer, Mask for the state ringbuffer
		/// </summary>
		private const int STATEMASK = 16383;

		/// <summary>
		/// How many states are actually in the state ringbuffer
		/// </summary>
		public int Count => (_nextStateIndex - _firstStateIndex) & STATEMASK;

		/// <summary>
		/// total number of bytes used
		/// </summary>
		/// <value></value>
		public long Used => Count == 0
			? 0
			: (_states[HeadStateIndex].Start
				+ _states[HeadStateIndex].Size
				- _states[_firstStateIndex].Start
			) & _sizeMask;

		/// <summary>
		/// Total size of the _buffer
		/// </summary>
		/// <value></value>
		public long Size { get; }

		private readonly long _sizeMask;
		private readonly MemoryBlock _buffer;
		private readonly FileStream _fStream;

		private readonly int _targetFrameLength;

		private struct StateInfo
		{
			public long Start;
			public int Size;
			public int Frame;
		}

		private readonly StateInfo[] _states;
		private int _firstStateIndex;
		private int _nextStateIndex;
		private int HeadStateIndex => (_nextStateIndex - 1) & STATEMASK;

		private readonly bool _useCompression;

		/// <summary>
		/// TODO: This is not a frequency, it's the reciprocal
		/// </summary>
		public int RewindFrequency => ComputeIdealRewindInterval();

		private int ComputeIdealRewindInterval()
		{
			if (Count == 0)
			{
				return 1; // shrug
			}

			// assume that the most recent state size is representative of stuff
			var sizeRatio = Size / (float)_states[HeadStateIndex].Size;
			var frameRatio = _targetFrameLength / sizeRatio;

			var idealInterval = (int)Math.Round(frameRatio);
			return Math.Max(idealInterval, 1);
		}

		public bool MatchesSettings(RewindConfig settings)
		{
			long targetSize = settings.BufferSize * 1024 * 1024;
			long size = 1L << (int)Math.Floor(Math.Log(targetSize, 2));
			return Size == size &&
				_useCompression == settings.UseCompression &&
				_targetFrameLength == settings.TargetFrameLength &&
				(_fStream != null) == settings.UseDrive;
		}

		private bool ShouldCapture(int frame)
		{
			if (Count == 0)
			{
				return true;
			}

			var frameDiff = frame - _states[HeadStateIndex].Frame;
			if (frameDiff < 1)
				// non-linear time is from a combination of other state changing mechanisms and the rewinder
				// not much we can say here, so just take a state
				return true;

			return frameDiff >= ComputeIdealRewindInterval();
		}

		/// <summary>
		/// Maybe captures a state, if the conditions are favorable
		/// </summary>
		/// <param name="frame">frame number to capture</param>
		/// <param name="callback">will be called with the stream if capture is to be performed</param>
		/// <param name="indexInvalidated">
		/// If provided, will be called with the index of states that are about to be removed.  This will happen during
		/// calls to Write() inside `callback`, and any reading of the old state must be finished before this returns.
		/// </param>
		public void Capture(int frame, Action<Stream> callback, Action<int> indexInvalidated = null, bool force = false)
		{
			if (!force && !ShouldCapture(frame))
				return;

			if (Count == STATEMASK)
			{
				indexInvalidated?.Invoke(0);
				_firstStateIndex = (_firstStateIndex + 1) & STATEMASK;
			}

			var start = (_states[HeadStateIndex].Start + _states[HeadStateIndex].Size) & _sizeMask;
			var initialMaxSize = Count > 0
					? (_states[_firstStateIndex].Start - start) & _sizeMask
					: Size;
			Func<long> notifySizeReached = () =>
			{
				if (Count == 0)
					throw new IOException("A single state must not be larger than the buffer");
				indexInvalidated?.Invoke(0);
				_firstStateIndex = (_firstStateIndex + 1) & STATEMASK;
				return Count > 0
					? (_states[_firstStateIndex].Start - start) & _sizeMask
					: Size;
			};
			SaveStateStream stream;
			if (_fStream == null)
				stream = new SaveStateStream(_buffer, start, _sizeMask, initialMaxSize, notifySizeReached);
			else
				stream = new SaveStateStream(_fStream, start, _sizeMask, initialMaxSize, notifySizeReached);

			if (_useCompression)
			{
				using var compressor = new DeflateStream(stream, CompressionLevel.Fastest, leaveOpen: true);
				callback(compressor);
			}
			else
			{
				callback(stream);
			}

			_states[_nextStateIndex].Frame = frame;
			_states[_nextStateIndex].Start = start;
			_states[_nextStateIndex].Size = (int)stream.Length;
			_nextStateIndex = (_nextStateIndex + 1) & STATEMASK;

			//Util.DebugWriteLine($"Size: {Size >> 20}MiB, Used: {Used >> 20}MiB, States: {Count}");
		}

		private Stream MakeLoadStream(int index)
		{
			Stream stream;
			if (_fStream == null)
				stream = new LoadStateStream(_buffer, _states[index].Start, _states[index].Size, _sizeMask);
			else
				stream = new LoadStateStream(_fStream, _states[index].Start, _states[index].Size, _sizeMask);

			if (_useCompression)
				stream = new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: true);
			return stream;
		}

		public class StateInformation
		{
			private readonly int _index;
			public int Frame => _parent._states[_index].Frame;
			public int Size => _parent._states[_index].Size;
			private readonly ZwinderBuffer _parent;
			public Stream GetReadStream()
			{
				return _parent.MakeLoadStream(_index);
			}
			internal StateInformation(ZwinderBuffer parent, int index)
			{
				_index = index;
				_parent = parent;
			}
		}

		/// <summary>
		/// Retrieve information about a state from 0..Count - 1.
		/// The information contained within is valid only until the collection is modified.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public StateInformation GetState(int index)
		{
			if ((uint)index >= (uint)Count)
				throw new IndexOutOfRangeException();
			return new StateInformation(this, (index + _firstStateIndex) & STATEMASK);
		}

		/// <summary>
		/// Invalidate states from GetState(index) on to the end of the buffer, so that Count == index afterwards
		/// </summary>
		/// <param name="index"></param>
		public void InvalidateEnd(int index)
		{
			if ((uint)index > (uint)Count)
				throw new IndexOutOfRangeException();
			_nextStateIndex = (index + _firstStateIndex) & STATEMASK;
			//Util.DebugWriteLine($"Size: {Size >> 20}MiB, Used: {Used >> 20}MiB, States: {Count}");
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			writer.Write(Size);
			writer.Write(_sizeMask);
			writer.Write(_targetFrameLength);
			writer.Write(_useCompression);

			SaveStateBodyBinary(writer);
		}

		private void SaveStateBodyBinary(BinaryWriter writer)
		{
			writer.Write(Count);
			for (var i = _firstStateIndex; i != _nextStateIndex; i = (i + 1) & STATEMASK)
			{
				writer.Write(_states[i].Frame);
				writer.Write(_states[i].Size);
			}
			if (Count != 0)
			{
				var startByte = _states[_firstStateIndex].Start;
				var endByte = (_states[HeadStateIndex].Start + _states[HeadStateIndex].Size) & _sizeMask;
				// TODO: Use spans to avoid these extra copies in .net core
				if (startByte > endByte)
				{
					{
						var stream = _buffer.GetStream(_buffer.Start + (ulong)startByte, (ulong)(Size - startByte), false);
						stream.CopyTo(writer.BaseStream);
					}
					startByte = 0;
				}
				{
					var stream = _buffer.GetStream(_buffer.Start + (ulong)startByte, (ulong)(endByte - startByte), false);
					stream.CopyTo(writer.BaseStream);
				}
			}
		}

		private void LoadStateBodyBinary(BinaryReader reader)
		{
			_firstStateIndex = 0;
			_nextStateIndex = reader.ReadInt32();
			long nextByte = 0;
			for (var i = 0; i < _nextStateIndex; i++)
			{
				_states[i].Frame = reader.ReadInt32();
				_states[i].Size = reader.ReadInt32();
				_states[i].Start = nextByte;
				nextByte += _states[i].Size;
			}
			// TODO: Use spans to avoid this extra copy in .net core
			var dest = _buffer.GetStream(_buffer.Start, (ulong)nextByte, true);
			WaterboxUtils.CopySome(reader.BaseStream, dest, nextByte);
		}

		public static ZwinderBuffer Create(BinaryReader reader)
		{
			var size = reader.ReadInt64();
			var sizeMask = reader.ReadInt64();
			var targetFrameLength = reader.ReadInt32();
			var useCompression = reader.ReadBoolean();
			var ret = new ZwinderBuffer(new RewindConfig
			{
				BufferSize = (int)(size >> 20),
				TargetFrameLength = targetFrameLength,
				UseCompression = useCompression
			});
			if (ret.Size != size || ret._sizeMask != sizeMask)
			{
				throw new InvalidOperationException("Bad format");
			}
			ret.LoadStateBodyBinary(reader);
			return ret;
		}

		private unsafe class SaveStateStream : Stream, ISpanStream
		{
			/// <summary>
			/// 
			/// </summary>
			/// <param name="buffer">The ringbuffer to write into</param>
			/// <param name="offset">Offset into the buffer to start writing (and treat as position 0 in the stream)</param>
			/// <param name="mask">Buffer size mask, used to wrap values in the ringbuffer correctly</param>
			/// <param name="notifySize">
			/// If the stream will exceed this size, notifySizeReached must be called before clobbering any data
			/// </param>
			/// <param name="notifySizeReached">
			/// The callback that will be called when notifySize is about to be exceeded.  Can either return a new larger notifySize,
			/// or abort processing with an IOException.  This must fail if size is going to exceed buffer.Length, as nothing else
			/// is preventing that case.
			/// </param>
			public SaveStateStream(MemoryBlock buffer, long offset, long mask, long notifySize, Func<long> notifySizeReached)
			{
				_ptr = (byte*)Z.US(buffer.Start);
				_offset = offset;
				_mask = mask;
				_notifySize = notifySize;
				_notifySizeReached = notifySizeReached;
			}
			public SaveStateStream(FileStream fileStream, long offset, long mask, long notifySize, Func<long> notifySizeReached)
			{
				_fStream = fileStream;
				_offset = offset;
				_mask = mask;
				_notifySize = notifySize;
				_notifySizeReached = notifySizeReached;
			}

			private readonly byte* _ptr;
			private readonly FileStream _fStream;
			private readonly long _offset;
			private readonly long _mask;
			private long _position;
			private long _notifySize;
			private readonly Func<long> _notifySizeReached;
			private long BufferLength => _mask + 1;

			public override bool CanRead => false;
			public override bool CanSeek => false;
			public override bool CanWrite => true;
			public override long Length => _position;

			public override long Position { get => _position; set => throw new IOException(); }

			public override void Flush() {}

			public override int Read(byte[] buffer, int offset, int count) => throw new IOException();
			public override long Seek(long offset, SeekOrigin origin) => throw new IOException();
			public override void SetLength(long value) => throw new IOException();
			public int Read(Span<byte> buffer) => throw new IOException();

			public override void Write(byte[] buffer, int offset, int count)
			{
				if (_fStream == null)
				{
					Write(new ReadOnlySpan<byte>(buffer, offset, count));
					return;
				}

				long requestedSize = _position + buffer.Length;
				while (requestedSize > _notifySize)
					_notifySize = _notifySizeReached();
				int n = Math.Min(buffer.Length - offset, count);
				if (n > 0)
				{
					var start = (_position + _offset) & _mask;
					_fStream.Seek(start, SeekOrigin.Begin);
					var end = (start + n) & _mask;
					if (end < start)
					{
						int m = (int)(BufferLength - start);

						_fStream.Write(buffer, offset, m);
						_fStream.Seek(0, SeekOrigin.Begin);
						_fStream.Write(buffer, offset + m, n - m);
					}
					else
						_fStream.Write(buffer, 0, n);

					_position += n;
				}
			}

			public void Write(ReadOnlySpan<byte> buffer)
			{
				if (_fStream != null)
				{
					Write(buffer.ToArray(), 0, buffer.Length);
					return;
				}

				long requestedSize = _position + buffer.Length;
				while (requestedSize > _notifySize)
					_notifySize = _notifySizeReached();
				long n = buffer.Length;
				if (n > 0)
				{
					var start = (_position + _offset) & _mask;
					var end = (start + n) & _mask;
					if (end < start)
					{
						long m = BufferLength - start;

						// Array.Copy(buffer, offset, _buffer, start, m);
						buffer.Slice(0, (int)m).CopyTo(new Span<byte>(_ptr + start, (int)m));
						start = 0;

						buffer = buffer.Slice((int)m);

						n -= m;
						_position += m;
					}
					if (n > 0)
					{
						buffer.CopyTo(new Span<byte>(_ptr + start, (int)n));

						_position += n;
					}
				}
			}

			public override void WriteByte(byte value)
			{
				long requestedSize = _position + 1;
				while (requestedSize > _notifySize)
					_notifySize = _notifySizeReached();
				long index = (_position++ + _offset) & _mask;
				if (_fStream == null)
					_ptr[index] = value;
				else
				{
					_fStream.Seek(index, SeekOrigin.Begin);
					_fStream.WriteByte(value);
					_position++;
				}
			}
		}

		private unsafe class LoadStateStream : Stream, ISpanStream
		{
			public LoadStateStream(MemoryBlock buffer, long offset, long size, long mask)
			{
				_ptr = (byte*)Z.US(buffer.Start);
				_offset = offset;
				_size = size;
				_mask = mask;
			}
			public LoadStateStream(FileStream fileStream, long offset, long size, long mask)
			{
				_fStream = fileStream;
				_offset = offset;
				_size = size;
				_mask = mask;
			}

			private readonly byte* _ptr;
			private readonly FileStream _fStream;
			private readonly long _offset;
			private readonly long _size;
			private long _position;
			private readonly long _mask;
			private long BufferLength => _mask + 1;

			public override bool CanRead => true;
			public override bool CanSeek => false;
			public override bool CanWrite => false;
			public override long Length => _size;
			public override long Position
			{
				get => _position;
				set => throw new IOException();
			}
			public override void Flush()
			{}

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (_fStream == null)
					return Read(new Span<byte>(buffer, offset, count));

				long n = Math.Min(_size - _position, buffer.Length);
				int ret = (int)n;
				if (n > 0)
				{
					var start = (_position + _offset) & _mask;
					_fStream.Seek(start, SeekOrigin.Begin);
					var end = (start + n) & _mask;
					if (end < start)
					{
						int m = (int)(BufferLength - start);

						_fStream.Read(buffer, offset, m);
						_fStream.Seek(0, SeekOrigin.Begin);

						n -= m;
						_position += m;
						offset += m;
					}
					if (n > 0)
					{
						_fStream.Read(buffer, offset, (int)n);

						_position += n;
					}
				}
				return ret;
			}

			public unsafe int Read(Span<byte> buffer)
			{
				int n = (int)Math.Min(_size - _position, buffer.Length);

				if (_fStream != null)
				{
					byte[] bytes = buffer.ToArray(); // Is this byte array the actual buffer?
					byte original = bytes[0];
					bytes[0] = (byte)(255 - bytes[0]); // guarantees a change
					if (buffer[0] == bytes[0])
					{ // yes
						bytes[0] = original;
						return Read(bytes, 0, n);
					}
					else
					{ // no
						bytes = new byte[n];
						int bytesRead = Read(bytes, 0, n);
						new Span<byte>(bytes).CopyTo(buffer);
						return bytesRead;
					}
				}

				int ret = n;
				if (n > 0)
				{
					var start = (_position + _offset) & _mask;
					var end = (start + n) & _mask;
					if (end < start)
					{
						int m = (int)(BufferLength - start);

						// Array.Copy(_buffer, start, buffer, offset, m);
						new ReadOnlySpan<byte>(_ptr + start, m).CopyTo(buffer);
						buffer = buffer.Slice(m);
	
						n -= m;
						_position += m;
						start = 0;
					}
					if (n > 0)
					{
						new ReadOnlySpan<byte>(_ptr + start, n).CopyTo(buffer);
						_position += n;
					}
				}
				return ret;
			}

			public override int ReadByte()
			{
				if (_position < _size)
				{
					long index = (_position++ + _offset) & _mask;
					if (_fStream == null)
						return _ptr[index];
					else
					{
						_fStream.Seek(index, SeekOrigin.Begin);
						_position++;
						return _fStream.ReadByte();
					}
				}
				else
					return -1;
			}

			public override long Seek(long offset, SeekOrigin origin) => throw new IOException();
			public override void SetLength(long value) => throw new IOException();
			public override void Write(byte[] buffer, int offset, int count) => throw new IOException();

			public void Write(ReadOnlySpan<byte> buffer) => throw new IOException();
		}
	}
}
