using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// A rewinder that uses Zelda compression, built on top of a ring buffer
	/// </summary>
	public class ZeldaWinder : IRewinder
	{
		private const int IS_GAP = unchecked((int)0x80000000);

		private readonly ZwinderBuffer _buffer;
		private readonly IStatable _stateSource;

		private byte[] _master = new byte[0];
		private int _masterFrame = -1;
		private int _masterLength = 0;
		private byte[] _scratch = new byte[0];

		// private Task _activeTask = null;

		// private void Sync()
		// {
		// 	_activeTask?.Wait();
		// 	_activeTask = null;
		// }

		public ZeldaWinder(IStatable stateSource, IRewindSettings settings)
		{
			_buffer = new ZwinderBuffer(settings);
			_stateSource = stateSource;
			Active = true;
		}

		/// <summary>
		/// How many states are actually in the state ringbuffer
		/// </summary>
		public int Count => _buffer.Count + (_masterFrame != -1 ? 1 : 0);

		public float FullnessRatio => Used / (float)Size;

		/// <summary>
		/// total number of bytes used
		/// </summary>
		/// <value></value>
		public long Used => _buffer.Used;

		/// <summary>
		/// Total size of the _buffer
		/// </summary>
		/// <value></value>
		public long Size => _buffer.Size;

		/// <summary>
		/// TODO: This is not a frequency, it's the reciprocal
		/// </summary>
		public int RewindFrequency => _buffer.RewindFrequency;

		public bool Active { get; private set; }

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
			_buffer.Dispose();
		}

		public void Clear()
		{
			_buffer.InvalidateEnd(0);
		}

		public unsafe void Capture(int frame)
		{
			if (!Active)
				return;
			if (_masterFrame == -1)
			{
				var sss = new SaveStateStream(this);
				_stateSource.SaveStateBinary(new BinaryWriter(sss));
				(_master, _scratch) = (_scratch, _master);
				_masterLength = (int)sss.Position;
				_masterFrame = frame;
				return;				
			}

			_buffer.Capture(_masterFrame, underlyingStream_ =>
			{
				var zeldas = SpanStream.GetOrBuild(underlyingStream_);
				var sss = new SaveStateStream(this);
				_stateSource.SaveStateBinary(new BinaryWriter(sss));
				if (_master.Length < _scratch.Length)
				{
					var replacement = new byte[_scratch.Length];
					Array.Copy(_master, replacement, _master.Length);
					_master = replacement;
				}

				var lengthHolder = _masterLength;
				var lengthHolderSpan = new ReadOnlySpan<byte>(&lengthHolder, 4);

				zeldas.Write(lengthHolderSpan);

				fixed (byte* older_ = _master)
				fixed (byte* newer_ = _scratch)
				{
					int* older = (int*)older_;
					int* newer = (int*)newer_;
					int lastIndex = (Math.Min(_masterLength, (int)sss.Position) + 3) / 4;
					int lastOldIndex = (_masterLength + 3) / 4;
					int* olderEnd = older + lastIndex;

					int* from = older;
					int* to = older;

					while (older < olderEnd)
					{
						if (*older++ == *newer++)
						{
							if (to < from)
							{
								// Save on [to, from]
								lengthHolder = (int)(from - to);
								zeldas.Write(lengthHolderSpan);
								zeldas.Write(new ReadOnlySpan<byte>(to, lengthHolder * 4));
							}
							to = older;
						}
						else
						{
							if (from < to)
							{
								// encode gap [from, to]
								lengthHolder = (int)(to - from) | IS_GAP;
								zeldas.Write(lengthHolderSpan);
							}
							from = older;
						}
					}
					if (from < to)
					{
						// encode gap [from, to]
						lengthHolder = (int)(to - from) | IS_GAP;
						zeldas.Write(lengthHolderSpan);
					}
					if (lastOldIndex > lastIndex)
					{
						from += lastOldIndex - lastIndex;
					}
					if (to < from)
					{
						// Save on [to, from]
						lengthHolder = (int)(from - to);
						zeldas.Write(lengthHolderSpan);
						zeldas.Write(new ReadOnlySpan<byte>(to, lengthHolder * 4));
					}
				}

				(_master, _scratch) = (_scratch, _master);
				_masterLength = (int)sss.Position;
				_masterFrame = frame;
			});
		}

		private unsafe void RefillMaster(ZwinderBuffer.StateInformation state)
		{
			var lengthHolder = 0;
			var lengthHolderSpan = new Span<byte>(&lengthHolder, 4);
			var zeldas = SpanStream.GetOrBuild(state.GetReadStream());
			zeldas.Read(lengthHolderSpan);
			_masterLength = lengthHolder;
			fixed (byte* buffer_ = _master)
			{
				int* buffer = (int*)buffer_;
				while (zeldas.Read(lengthHolderSpan) == 4)
				{
					if ((lengthHolder & IS_GAP) != 0)
					{
						buffer += lengthHolder & ~IS_GAP;
					}
					else
					{
						zeldas.Read(new Span<byte>(buffer, lengthHolder * 4));
						buffer += lengthHolder;
					}
				}
			}
			_masterFrame = state.Frame;
		}

		public bool Rewind(int frameToAvoid)
		{
			if (!Active || Count == 0)
				return false;

			if (_masterFrame == frameToAvoid && Count > 1)
			{
				var index = _buffer.Count - 1;
				RefillMaster(_buffer.GetState(index));
				_buffer.InvalidateEnd(index);	
				_stateSource.LoadStateBinary(new BinaryReader(new MemoryStream(_master, 0, _masterLength, false)));			
			}
			else
			{
				_stateSource.LoadStateBinary(new BinaryReader(new MemoryStream(_master, 0, _masterLength, false)));
				var index = _buffer.Count - 1;
				if (index >= 0)
				{
					RefillMaster(_buffer.GetState(index));
					_buffer.InvalidateEnd(index);
				}
				else
				{
					_masterFrame = -1;
				}
			}
			return true;
		}

		private class SaveStateStream : Stream, ISpanStream
		{
			public SaveStateStream(ZeldaWinder owner)
			{
				_owner = owner;
			}
			private ZeldaWinder _owner;
			private byte[] _dest
			{
				get => _owner._scratch;
				set => _owner._scratch = value;
			}
			private int _position;
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
				Write(new ReadOnlySpan<byte>(buffer, offset, count));
			}
			private void MaybeResize(int requestedSize)
			{
				if (requestedSize > _dest.Length)
				{
					var replacement = new byte[(Math.Max(_dest.Length * 2, requestedSize) + 3) & ~3];
					Array.Copy(_dest, replacement, _dest.Length);
					_dest = replacement;
				}				
			}
			public void Write(ReadOnlySpan<byte> buffer)
			{
				var requestedSize = _position + buffer.Length;
				MaybeResize(requestedSize);
				buffer.CopyTo(new Span<byte>(_dest, _position, buffer.Length));
				_position = requestedSize;
			}
			public override void WriteByte(byte value)
			{
				var requestedSize = _position + 1;
				MaybeResize(requestedSize);
				_dest[_position] = value;
				_position = requestedSize;
			}
			public int Read(Span<byte> buffer) => throw new IOException();
		}
	}
}
