using System;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Rewinder : IDisposable
	{
		private readonly IStatable _statableCore;

		private const int MaxByteArraySize = 0x7FFFFFC7; // .NET won't let us allocate more than this in one array

		private readonly StreamBlobDatabase _rewindBuffer;
		private byte[] _rewindBufferBacking;
		private long _memoryLimit = MaxByteArraySize;
		private readonly RewindThreader _rewindThread;
		private byte[] _lastState;
		private readonly bool _rewindDeltaEnable;
		private bool _lastRewindLoadedState;
		private byte[] _deltaBuffer = new byte[0];

		public bool RewindActive => RewindEnabled && !SuspendRewind;

		private bool RewindEnabled { get; }

		public bool SuspendRewind { get; set; }

		public float FullnessRatio => _rewindBuffer?.FullnessRatio ?? 0;

		public int Count => _rewindBuffer?.Count ?? 0;

		public long Size => _rewindBuffer?.Size ?? 0;

		public bool HasBuffer => _rewindBuffer != null;

		public int RewindFrequency { get; }

		public Rewinder(IStatable statableCore, IRewindSettings settings)
		{
			_statableCore = statableCore;

			int stateSize = _statableCore.CloneSavestate().Length;

			if (stateSize >= settings.LargeStateSize)
			{
				RewindEnabled = settings.EnabledLarge;
				RewindFrequency = settings.FrequencyLarge;
			}
			else if (stateSize >= settings.MediumStateSize)
			{
				RewindEnabled = settings.EnabledMedium;
				RewindFrequency = settings.FrequencyMedium;
			}
			else
			{
				RewindEnabled = settings.EnabledSmall;
				RewindFrequency = settings.FrequencySmall;
			}

			_rewindDeltaEnable = settings.UseDelta;

			if (RewindActive)
			{
				var capacity = settings.BufferSize * 1024L * 1024L;
				_rewindBuffer = new StreamBlobDatabase(settings.OnDisk, capacity, BufferManage);
				_rewindThread = new RewindThreader(CaptureInternal, RewindInternal, settings.IsThreaded);
			}
		}

		private void Clear()
		{
			_rewindBuffer?.Clear();
			_lastState = new byte[0];
		}

		private byte[] BufferManage(byte[] inbuf, ref long size, bool allocate)
		{
			if (!allocate)
			{
				_rewindBufferBacking = inbuf;
				return null;
			}

			size = Math.Min(size, _memoryLimit);

			// if we have an appropriate buffer free, return it
			var buf = _rewindBufferBacking;
			_rewindBufferBacking = null;
			if (buf != null && buf.LongLength == size)
			{
				return buf;
			}

			// otherwise, allocate it
			do
			{
				try
				{
					return new byte[size];
				}
				catch (OutOfMemoryException)
				{
					size /= 2;
					_memoryLimit = size;
				}
			}
			while (size > 1);
			throw new OutOfMemoryException();
		}

		public void Capture(int frame)
		{
			if (!RewindActive)
			{
				return;
			}

			if (_rewindThread == null || frame % RewindFrequency != 0)
			{
				return;
			}

			_rewindThread.Capture(_statableCore.SaveStateBinary());
		}

		private void CaptureInternal(byte[] coreSavestate)
		{
			if (_rewindDeltaEnable)
			{
				CaptureStateDelta(coreSavestate);
			}
			else
			{
				CaptureStateNonDelta(coreSavestate);
			}
		}

		private void CaptureStateNonDelta(byte[] state)
		{
			long offset = _rewindBuffer.Enqueue(0, state.Length + 1);
			var stream = _rewindBuffer.Stream;
			stream.Position = offset;

			// write the header for a non-delta frame
			stream.WriteByte(1); // Full state = true
			stream.Write(state, 0, state.Length);
		}

		private void UpdateLastState(byte[] state, int index, int length)
		{
			if (_lastState.Length != length)
			{
				_lastState = new byte[length];
			}

			Buffer.BlockCopy(state, index, _lastState, 0, length);
		}

		private void UpdateLastState(byte[] state)
		{
			UpdateLastState(state, 0, state.Length);
		}

		private unsafe void CaptureStateDelta(byte[] currentState)
		{
			// Keep in mind that everything captured here is intended to be played back in
			// reverse. The goal is, given the current state, how to get back to the previous
			// state. That's why the data portion of the delta comes from the previous state,
			// and also why the previous state is used if we have to bail out and capture the
			// full state instead.
			if (currentState.Length != _lastState.Length)
			{
				// If the state sizes mismatch, capture a full state rather than trying to do anything clever
				goto CaptureFullState;
			}

			if (currentState.Length == 0)
			{
				// handle empty states as a "full" (empty) state
				goto CaptureFullState;
			}

			int index = 0;
			int stateLength = Math.Min(currentState.Length, _lastState.Length);
			bool inChangeSequence = false;
			int changeSequenceStartOffset = 0;
			int lastChangeSequenceStartOffset = 0;

			if (_deltaBuffer.Length < stateLength + 1)
			{
				_deltaBuffer = new byte[stateLength + 1];
			}

			_deltaBuffer[index++] = 0; // Full state = false (i.e. delta)

			fixed (byte* pCurrentState = &currentState[0])
			fixed (byte* pLastState = &_lastState[0])
			for (int i = 0; i < stateLength; i++)
			{
				bool thisByteMatches = *(pCurrentState + i) == *(pLastState + i);

				if (inChangeSequence == false)
				{
					if (thisByteMatches)
					{
						continue;
					}

					inChangeSequence = true;
					changeSequenceStartOffset = i;
				}

				if (thisByteMatches || i == stateLength - 1)
				{
					const int MaxHeaderSize = 10;
					int length = i - changeSequenceStartOffset + (thisByteMatches ? 0 : 1);

					if (index + length + MaxHeaderSize >= stateLength)
					{
						// If the delta ends up being larger than the full state, capture the full state instead
						goto CaptureFullState;
					}

					// Offset Delta
					VLInteger.WriteUnsigned((uint)(changeSequenceStartOffset - lastChangeSequenceStartOffset), _deltaBuffer, ref index);

					// Length
					VLInteger.WriteUnsigned((uint)length, _deltaBuffer, ref index);

					// Data
					Buffer.BlockCopy(_lastState, changeSequenceStartOffset, _deltaBuffer, index, length);
					index += length;

					inChangeSequence = false;
					lastChangeSequenceStartOffset = changeSequenceStartOffset;
				}
			}

			_rewindBuffer.Push(new ArraySegment<byte>(_deltaBuffer, 0, index));

			UpdateLastState(currentState);
			return;

		CaptureFullState:
			CaptureStateNonDelta(_lastState);
			UpdateLastState(currentState);
		}

		public bool Rewind(int frames)
		{
			if (!RewindActive || _rewindThread == null)
			{
				return false;
			}

			_rewindThread.Rewind(frames);

			return _lastRewindLoadedState;
		}

		private void RewindInternal(int frames)
		{
			_lastRewindLoadedState = false;

			for (int i = 0; i < frames; i++)
			{
				// Always leave the first item in the rewind buffer. For full states, once there's
				// one item remaining, we've already gone back as far as possible because the code
				// to load the previous state has already peeked at the first item after removing
				// the second item. We want to hold on to the first item anyway since it's a copy
				// of the current state (see comment in the following method). For deltas, since
				// each one records how to get back to the previous state, once we've gone back to
				// the second item, it's already resulted in the first state being loaded. The
				// first item is just a junk entry with the initial value of _lastState (0 bytes).
				if (_rewindBuffer.Count <= 1)
				{
					break;
				}

				LoadPreviousState();
				_lastRewindLoadedState = true;
			}
		}

		private MemoryStream GetPreviousStateMemoryStream()
		{
			if (_rewindDeltaEnable)
			{
				// When capturing deltas, the most recent state is stored in _lastState, and the
				// last item in the rewind buffer gets us back to the previous state.
				return _rewindBuffer.PopMemoryStream();
			}
			else
			{
				// When capturing full states, the last item in the rewind buffer is the most
				// recent state, so we need to get the item before it.
				_rewindBuffer.Pop();
				return _rewindBuffer.PeekMemoryStream();
			}

			// Note that in both cases, after loading the state, we still have a copy of it
			// either in _lastState or as the last item in the rewind buffer. This is good
			// because once we resume capturing, the first capture doesn't happen until
			// stepping forward to the following frame, which would result in a gap if we
			// didn't still have a copy of the current state here.
		}

		private void LoadPreviousState()
		{
			using var reader = new BinaryReader(GetPreviousStateMemoryStream());
			byte[] buf = ((MemoryStream)reader.BaseStream).GetBuffer();
			bool fullState = reader.ReadByte() == 1;
			if (_rewindDeltaEnable)
			{
				if (fullState)
				{
					UpdateLastState(buf, 1, buf.Length - 1);
				}
				else
				{
					int index = 1;
					int offset = 0;

					while (index < buf.Length)
					{
						int offsetDelta = (int)VLInteger.ReadUnsigned(buf, ref index);
						int length = (int)VLInteger.ReadUnsigned(buf, ref index);

						offset += offsetDelta;

						Buffer.BlockCopy(buf, index, _lastState, offset, length);
						index += length;
					}
				}

				_statableCore.LoadStateBinary(_lastState);
			}
			else
			{
				if (!fullState)
				{
					throw new InvalidOperationException();
				}

				_statableCore.LoadStateBinary(reader);
			}
		}

		public void Dispose()
		{
			Clear();
			_rewindBuffer?.Dispose();
			_rewindThread?.Dispose();
		}
	}

	public static class VLInteger
	{
		public static void WriteUnsigned(uint value, byte[] data, ref int index)
		{
			// This is optimized for good performance on both the x86 and x64 JITs. Don't change anything without benchmarking.
			do
			{
				var x = value & 0x7FU;
				value >>= 7;
				data[index++] = (byte)((value != 0U ? 0x80U : 0U) | x);
			}
			while (value != 0U);
		}

		public static uint ReadUnsigned(byte[] data, ref int index)
		{
			// This is optimized for good performance on both the x86 and x64 JITs. Don't change anything without benchmarking.
			var value = 0U;
			var shiftCount = 0;
			bool isLastByte; // Negating the comparison and moving it earlier in the loop helps a lot on x86 for some reason
			do
			{
				var x = (uint)data[index++];
				isLastByte = (x & 0x80U) == 0U;
				value |= (x & 0x7FU) << shiftCount;
				shiftCount += 7;
			}
			while (!isLastByte);
			return value;
		}
	}
}
