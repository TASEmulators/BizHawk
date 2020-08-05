using System;
using System.IO;
using System.IO.Compression;
using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public class ZwinderBuffer
	{
		/*
		Main goals:
		1. No copies, ever.  States are deposited directly to, and read directly from, one giant ring buffer.
			As a consequence, there is no multi-threading because there is nothing to thread.
		2. Support for arbitrary and changeable state sizes.  Frequency is calculated dynamically.
		3. No delta compression.  Keep it simple.  If there are cores that benefit heavily from delta compression, we should
			maintain a separate rewinder alongside this one that is customized for those cores.
		*/
		public ZwinderBuffer(IRewindSettings settings)
		{
			long targetSize = settings.BufferSize * 1024 * 1024;
			if (settings.TargetFrameLength < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(settings.TargetFrameLength));
			}

			Size = 1L << (int)Math.Floor(Math.Log(targetSize, 2));
			_sizeMask = Size - 1;
			_buffer = new byte[Size];
			_targetFrameLength = settings.TargetFrameLength;
			_states = new StateInfo[StateMask + 1];
			_useCompression = settings.UseCompression;
		}

		/// <summary>
		/// Number of states that could be in the state ringbuffer, Mask for the state ringbuffer
		/// </summary>
		private const int StateMask = 16383;

		/// <summary>
		/// How many states are actually in the state ringbuffer
		/// </summary>
		public int Count => (_nextStateIndex - _firstStateIndex) & StateMask;

		public float FullnessRatio => Used / (float)Size;

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
		private readonly byte[] _buffer;

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
		private int HeadStateIndex => (_nextStateIndex - 1) & StateMask;

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
		/// calls to Write() inside `callback`, and any reuse of the old state will have to happen immediately
		/// </param>
		public void Capture(int frame, Action<Stream> callback, Action<int> indexInvalidated = null, bool force = false)
		{
			if (!force && !ShouldCapture(frame))
				return;

			var start = (_states[HeadStateIndex].Start + _states[HeadStateIndex].Size) & _sizeMask;
			var initialMaxSize = Count > 0
					? (_states[_firstStateIndex].Start - start) & _sizeMask
					: Size;
			Func<long> notifySizeReached = () =>
			{
				if (Count == 0)
					throw new IOException("A single state must not be larger than the buffer");
				indexInvalidated?.Invoke(0);
				_firstStateIndex = (_firstStateIndex + 1) & StateMask;
				return Count > 0
					? (_states[_firstStateIndex].Start - start) & _sizeMask
					: Size;
			};
			var stream = new SaveStateStream(_buffer, start, _sizeMask, initialMaxSize, notifySizeReached);

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
			_nextStateIndex = (_nextStateIndex + 1) & StateMask;

			Util.DebugWriteLine($"Size: {Size >> 20}MiB, Used: {Used >> 20}MiB, States: {Count}");
		}

		private Stream MakeLoadStream(int index)
		{
			Stream stream = new LoadStateStream(_buffer, _states[index].Start, _states[index].Size, _sizeMask);
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
			return new StateInformation(this, (index + _firstStateIndex) & StateMask);
		}

		/// <summary>
		/// Invalidate states from GetState(index) on to the end of the buffer, so that Count == index afterwards
		/// </summary>
		/// <param name="index"></param>
		public void InvalidateEnd(int index)
		{
			if ((uint)index > (uint)Count)
				throw new IndexOutOfRangeException();
			_nextStateIndex = (index + _firstStateIndex) & StateMask;
			Util.DebugWriteLine($"Size: {Size >> 20}MiB, Used: {Used >> 20}MiB, States: {Count}");
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			writer.Write(Size);
			writer.Write(_sizeMask);
			writer.Write(_targetFrameLength);
			writer.Write(_useCompression);

			writer.Write(_buffer);
			foreach (var s in _states)
			{
				writer.Write(s.Start);
				writer.Write(s.Size);
				writer.Write(s.Frame);
			}
			writer.Write(_firstStateIndex);
			writer.Write(_nextStateIndex);
		}

		// public void LoadStateBinary(BinaryReader reader)
		// {
		// 	if (reader.ReadInt64() != Size)
		// 		throw new InvalidOperationException("Bad format");
		// 	if (reader.ReadInt64() != _sizeMask)
		// 		throw new InvalidOperationException("Bad format");
		// 	if (reader.ReadInt32() != _targetFrameLength)
		// 		throw new InvalidOperationException("Bad format");
		// 	if (reader.ReadBoolean() != _useCompression)
		// 		throw new InvalidOperationException("Bad format");

		// 	LoadStateBodyBinary(reader);
		// }

		private void LoadStateBodyBinary(BinaryReader reader)
		{
			reader.Read(_buffer, 0, _buffer.Length);
			for (var i = 0; i < _states.Length; i++)
			{
				_states[i].Start = reader.ReadInt64();
				_states[i].Size = reader.ReadInt32();
				_states[i].Frame = reader.ReadInt32();
			}
			_firstStateIndex = reader.ReadInt32();
			_nextStateIndex = reader.ReadInt32();
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

		private class SaveStateStream : Stream, ISpanStream
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
			public SaveStateStream(byte[] buffer, long offset, long mask, long notifySize, Func<long> notifySizeReached)
			{
				_buffer = buffer;
				_offset = offset;
				_mask = mask;
				_notifySize = notifySize;
				_notifySizeReached = notifySizeReached;
			}
			
			private readonly byte[] _buffer;
			private readonly long _offset;
			private readonly long _mask;
			private long _position;
			private long _notifySize;
			private readonly Func<long> _notifySizeReached;

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
				long requestedSize = _position + count;
				while (requestedSize > _notifySize)
					_notifySize = _notifySizeReached();
				long n = count;
				if (n > 0)
				{
					var start = (_position + _offset) & _mask;
					var end = (start + n) & _mask;
					if (end < start)
					{
						long m = _buffer.LongLength - start;
						Array.Copy(buffer, offset, _buffer, start, m);
						offset += (int)m;
						n -= m;
						_position += m;
						start = 0;
					}
					if (n > 0)
					{
						Array.Copy(buffer, offset, _buffer, start, n);
						_position += n;
					}
				}
			}

			public void Write(ReadOnlySpan<byte> buffer)
			{
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
						long m = _buffer.LongLength - start;

						// Array.Copy(buffer, offset, _buffer, start, m);
						buffer.Slice(0, (int)m).CopyTo(new Span<byte>(_buffer, (int)start, (int)m));

						// offset += (int)m;
						buffer = buffer.Slice((int)m);

						n -= m;
						_position += m;
						start = 0;
					}
					if (n > 0)
					{
						// Array.Copy(buffer, offset, _buffer, start, n);
						buffer.CopyTo(new Span<byte>(_buffer, (int)start, (int)n));

						_position += n;
					}
				}
			}

			public override void WriteByte(byte value)
			{
				long requestedSize = _position + 1;
				while (requestedSize > _notifySize)
					_notifySize = _notifySizeReached();
				_buffer[(_position++ + _offset) & _mask] = value;
			}
		}

		private class LoadStateStream : Stream, ISpanStream
		{
			public LoadStateStream(byte[] buffer, long offset, long size, long mask)
			{
				_buffer = buffer;
				_offset = offset;
				_size = size;
				_mask = mask;
			}

			private readonly byte[] _buffer;
			private readonly long _offset;
			private readonly long _size;
			private long _position;
			private readonly long _mask;

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
				long n = Math.Min(_size - _position, count);
				int ret = (int)n;
				if (n > 0)
				{
					var start = (_position + _offset) & _mask;
					var end = (start + n) & _mask;
					if (end < start)
					{
						long m = _buffer.LongLength - start;
						Array.Copy(_buffer, start, buffer, offset, m);
						offset += (int)m;
						n -= m;
						_position += m;
						start = 0;
					}
					if (n > 0)
					{
						Array.Copy(_buffer, start, buffer, offset, n);
						_position += n;
					}
				}
				return ret;
			}

			public unsafe int Read(Span<byte> buffer)
			{
				long n = Math.Min(_size - _position, buffer.Length);
				int ret = (int)n;
				if (n > 0)
				{
					var start = (_position + _offset) & _mask;
					var end = (start + n) & _mask;
					if (end < start)
					{
						long m = _buffer.LongLength - start;

						// Array.Copy(_buffer, start, buffer, offset, m);
						new ReadOnlySpan<byte>(_buffer, (int)start, (int)m).CopyTo(buffer);

						// offset += (int)m;
						buffer = buffer.Slice((int)m);
	
						n -= m;
						_position += m;
						start = 0;
					}
					if (n > 0)
					{
						// Array.Copy(_buffer, start, buffer, offset, n);
						new ReadOnlySpan<byte>(_buffer, (int)start, (int)n).CopyTo(buffer);
						_position += n;
					}
				}
				return ret;
			}

			public override int ReadByte()
			{
				return _position < _size
					? _buffer[(_position++ + _offset) & _mask]
					: -1;
			}

			public override long Seek(long offset, SeekOrigin origin) => throw new IOException();
			public override void SetLength(long value) => throw new IOException();
			public override void Write(byte[] buffer, int offset, int count) => throw new IOException();

			public void Write(ReadOnlySpan<byte> buffer) => throw new IOException();
		}
	}
}
