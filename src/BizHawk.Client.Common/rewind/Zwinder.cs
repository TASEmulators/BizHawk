using System;
using System.IO;
using System.IO.Compression;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// A simple ring buffer rewinder
	/// </summary>
	public class Zwinder : IRewinder
	{
		/*
		Main goals:
		1. No copies, ever.  States are deposited directly to, and read directly from, one giant ring buffer.
			As a consequence, there is no multi-threading because there is nothing to thread.
		2. Support for arbitrary and changeable state sizes.  Frequency is calculated dynamically.
		3. No delta compression.  Keep it simple.  If there are cores that benefit heavily from delta compression, we should
			maintain a separate rewinder alongside this one that is customized for those cores.
		*/
		public Zwinder(IBinaryStateable stateSource, IRewindSettings settings)
		{
			long targetSize = settings.BufferSize * 1024 * 1024;
			if (settings.TargetFrameLength < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(settings.TargetFrameLength));
			}

			Size = 1L << (int)Math.Floor(Math.Log(targetSize, 2));
			_sizeMask = Size - 1;
			_buffer = new byte[Size];
			Active = true;
			_stateSource = stateSource;
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
		private byte[] _buffer;

		private readonly int _targetFrameLength;

		private struct StateInfo
		{
			public long Start;
			public int Size;
			public int Frame;
		}

		private StateInfo[] _states;
		private int _firstStateIndex;
		private int _nextStateIndex;
		private int HeadStateIndex => (_nextStateIndex - 1) & StateMask;

		private readonly bool _useCompression;

		private IBinaryStateable _stateSource;

		/// <summary>
		/// TODO: This is not a frequency, it's the reciprocal
		/// </summary>
		public int RewindFrequency => ComputeIdealRewindInterval();

		public bool Active { get; private set; }

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

		public void Capture(int frame)
		{
			if (!Active || !ShouldCapture(frame))
				return;

			var start = (_states[HeadStateIndex].Start + _states[HeadStateIndex].Size) & _sizeMask;

			var stream = new SaveStateStream(
				_buffer,
				start,
				Size, _sizeMask);

			if (_useCompression)
			{
				using var compressor = new DeflateStream(stream, CompressionLevel.Fastest, leaveOpen: true);
				_stateSource.SaveStateBinary(new BinaryWriter(compressor));
			}
			else
			{
				_stateSource.SaveStateBinary(new BinaryWriter(stream));
			}

			// invalidate states if we're at the state ringbuffer size limit, or if they were overridden in the byte buffer
			var length = stream.Length;
			while (Count == StateMask || Count > 0 && ((_states[_firstStateIndex].Start - start) & _sizeMask) < length)
				_firstStateIndex = (_firstStateIndex + 1) & StateMask;
			
			_states[_nextStateIndex].Frame = frame;
			_states[_nextStateIndex].Start = start;
			_states[_nextStateIndex].Size = (int)length;
			_nextStateIndex = (_nextStateIndex + 1) & StateMask;

			Console.WriteLine($"Size: {Size >> 20}MiB, Used: {Used >> 20}MiB, States: {Count}");
		}

		public bool Rewind(int frames)
		{
			if (!Active)
				return false;
			// this is supposed to rewind to the previous saved frame
			// It's only ever called with a value of 1 from the frontend?

			frames = Math.Min(frames, Count);
			if (frames == 0)
				return false; // no states saved
			int loadIndex = (_nextStateIndex - frames) & StateMask;

			var stream = new LoadStateStream(_buffer, _states[loadIndex].Start, _states[loadIndex].Size, _sizeMask);
			_stateSource.LoadStateBinary(_useCompression
				? new BinaryReader(new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: true))
				: new BinaryReader(stream));

			_nextStateIndex = loadIndex;
			Console.WriteLine($"Size: {Size >> 20}MiB, Used: {Used >> 20}MiB, States: {Count}");
			return true;
		}

		public void Suspend()
		{
			Active = false;
		}

		public void Resume()
		{
			Active = true;
		}


		public void Dispose()
		{
			_buffer = null;
			_states = null;
			_stateSource = null;
		}

		private class SaveStateStream : Stream
		{
			public SaveStateStream(byte[] buffer, long offset, long maxSize, long mask)
			{
				_buffer = buffer;
				_offset = offset;
				_maxSize = maxSize;
				_mask = mask;
			}
			
			private readonly byte[] _buffer;
			private readonly long _offset;
			private readonly long _maxSize;
			private readonly long _mask;
			private long _position;

			public override bool CanRead => false;
			public override bool CanSeek => false;
			public override bool CanWrite => true;
			public override long Length => _position;

			public override long Position { get => _position; set => throw new IOException(); }

			public override void Flush() {}

			public override int Read(byte[] buffer, int offset, int count) => throw new IOException();
			public override long Seek(long offset, SeekOrigin origin) => throw new IOException();
			public override void SetLength(long value) => throw new IOException();

			public override void Write(byte[] buffer, int offset, int count)
			{
				long n = Math.Min(_maxSize - _position, count);
				if (n != count)
					throw new IOException("A single state cannot be bigger than the buffer!");
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

			public override void WriteByte(byte value)
			{
				if (_position < _maxSize)
				{
					_buffer[(_position++ + _offset) & _mask] = value;
				}
				else
				{
					throw new IOException("A single state cannot be bigger than the buffer!");
				}
			}
		}

		private class LoadStateStream : Stream
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

			public override int ReadByte()
			{
				return _position < _size
					? _buffer[(_position++ + _offset) & _mask]
					: -1;
			}

			public override long Seek(long offset, SeekOrigin origin) => throw new IOException();
			public override void SetLength(long value) => throw new IOException();
			public override void Write(byte[] buffer, int offset, int count) => throw new IOException();
		}
	}
}
