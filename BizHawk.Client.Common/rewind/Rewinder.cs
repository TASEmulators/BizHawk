using System;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	public class Rewinder
	{
		private StreamBlobDatabase _rewindBuffer;
		private RewindThreader _rewindThread;
		private byte[] _lastState;
		private bool _rewindImpossible;
		private int _rewindFrequency = 1;
		private bool _rewindDeltaEnable;
		private byte[] _rewindFellationBuf;
		private byte[] _tempBuf = new byte[0];

		public Rewinder()
		{
			RewindActive = true;
		}

		public Action<string> MessageCallback { get; set; }
		public bool RewindActive { get; set; }

		// TODO: make RewindBuf never be null
		public float FullnessRatio
		{
			get { return _rewindBuffer.FullnessRatio; }
		}
		
		public int Count
		{
			get { return _rewindBuffer != null ? _rewindBuffer.Count : 0; }
		}
		
		public long Size 
		{
			get { return _rewindBuffer != null ? _rewindBuffer.Size : 0; }
		}
		
		public int BufferCount
		{
			get { return _rewindBuffer != null ? _rewindBuffer.Count : 0; }
		}

		public bool HasBuffer
		{
			get { return _rewindBuffer != null; }
		}

		public int RewindFrequency
		{
			get { return _rewindFrequency; }
		}

		// TOOD: this should not be parameterless?! It is only possible due to passing a static context in
		public void CaptureRewindState()
		{
			if (Global.Emulator.HasSavestates())
			{
				if (_rewindImpossible)
				{
					return;
				}

				if (_lastState == null)
				{
					DoRewindSettings();
				}

				// log a frame
				if (_lastState != null && Global.Emulator.Frame % _rewindFrequency == 0)
				{
					_rewindThread.Capture(Global.Emulator.AsStatable().SaveStateBinary());
				}
			}
		}

		public void DoRewindSettings()
		{
			if (Global.Emulator.HasSavestates())
			{
				// This is the first frame. Capture the state, and put it in LastState for future deltas to be compared against.
				_lastState = (byte[])Global.Emulator.AsStatable().SaveStateBinary().Clone();

				int state_size;
				if (_lastState.Length >= Global.Config.Rewind_LargeStateSize)
				{
					SetRewindParams(Global.Config.RewindEnabledLarge, Global.Config.RewindFrequencyLarge);
					state_size = 3;
				}
				else if (_lastState.Length >= Global.Config.Rewind_MediumStateSize)
				{
					SetRewindParams(Global.Config.RewindEnabledMedium, Global.Config.RewindFrequencyMedium);
					state_size = 2;
				}
				else
				{
					SetRewindParams(Global.Config.RewindEnabledSmall, Global.Config.RewindFrequencySmall);
					state_size = 1;
				}

				var rewind_enabled = false;
				if (state_size == 1)
				{
					rewind_enabled = Global.Config.RewindEnabledSmall;
				}
				else if (state_size == 2)
				{
					rewind_enabled = Global.Config.RewindEnabledMedium;
				}
				else if (state_size == 3)
				{
					rewind_enabled = Global.Config.RewindEnabledLarge;
				}

				_rewindDeltaEnable = Global.Config.Rewind_UseDelta;

				if (rewind_enabled)
				{
					var cap = Global.Config.Rewind_BufferSize * (long)1024 * 1024;

					if (_rewindBuffer != null)
					{
						_rewindBuffer.Dispose();
					}

					_rewindBuffer = new StreamBlobDatabase(Global.Config.Rewind_OnDisk, cap, BufferManage);

					if (_rewindThread != null)
					{
						_rewindThread.Dispose();
					}

					_rewindThread = new RewindThreader(this, Global.Config.Rewind_IsThreaded);
				}
			}
		}

		public void Rewind(int frames)
		{
			if (Global.Emulator.HasSavestates())
			{
				_rewindThread.Rewind(frames);
			}
		}

		// TODO remove me
		public void _RunRewind(int frames)
		{
			for (int i = 0; i < frames; i++)
			{
				if (_rewindBuffer.Count == 0 || (Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie.InputLogLength == 0))
				{
					return;
				}

				if (_lastState.Length < 0x10000)
				{
					Rewind64K();
				}
				else
				{
					RewindLarge();
				}
			}
		}

		// TODO: only run by RewindThreader, refactor
		public void RunCapture(byte[] coreSavestate)
		{
			if (_rewindDeltaEnable)
			{
				CaptureRewindStateDelta(coreSavestate, _lastState.Length <= 0x10000);
			}
			else
			{
				CaptureRewindStateNonDelta(coreSavestate);
			}
		}

		public void ResetRewindBuffer()
		{
			if (_rewindBuffer != null)
			{
				_rewindBuffer.Clear();
			}

			_rewindImpossible = false;
			_lastState = null;
		}

		private void DoMessage(string message)
		{
			if (MessageCallback != null)
			{
				MessageCallback(message);
			}
		}

		private void SetRewindParams(bool enabled, int frequency)
		{
			if (RewindActive != enabled)
			{
				DoMessage("Rewind " + (enabled ? "Enabled" : "Disabled"));
			}

			if (_rewindFrequency != frequency && enabled)
			{
				DoMessage("Rewind frequency set to " + frequency);
			}

			RewindActive = enabled;
			_rewindFrequency = frequency;

			if (!RewindActive)
			{
				_lastState = null;
			}
		}

		private byte[] BufferManage(byte[] inbuf, long size, bool allocate)
		{
			if (allocate)
			{
				// if we have an appropriate buffer free, return it
				if (_rewindFellationBuf != null && _rewindFellationBuf.LongLength == size)
				{
					var ret = _rewindFellationBuf;
					_rewindFellationBuf = null;
					return ret;
				}

				// otherwise, allocate it
				return new byte[size];
			}
			
			_rewindFellationBuf = inbuf;
			return null;
		}

		private void CaptureRewindStateNonDelta(byte[] currentState)
		{
			long offset = _rewindBuffer.Enqueue(0, currentState.Length + 1);
			var stream = _rewindBuffer.Stream;
			stream.Position = offset;

			// write the header for a non-delta frame
			stream.WriteByte(1); // i.e. true
			stream.Write(currentState, 0, currentState.Length);
		}

		private unsafe void CaptureRewindStateDelta(byte[] currentState, bool isSmall)
		{
			// in case the state sizes mismatch, capture a full state rather than trying to do anything clever
			if (currentState.Length != _lastState.Length)
			{
				CaptureRewindStateNonDelta(currentState);
				return;
			}

			var beginChangeSequence = -1;
			var inChangeSequence = false;

			if (_tempBuf.Length < currentState.Length)
			{
				_tempBuf = new byte[currentState.Length];
			}

			int offset = 0;
			_tempBuf[offset++] = 0; // Full state (false = delta)
			int stateLength = Math.Min(currentState.Length, _lastState.Length); // Just to be safe :)
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
					beginChangeSequence = i;
				}

				if (thisByteMatches || i - beginChangeSequence == 254 || i == currentState.Length - 1)
				{
					const int maxHeaderSize = 5;
					int length = i - beginChangeSequence + (thisByteMatches ? 0 : 1);

					if (offset + length + maxHeaderSize >= stateLength)
					{
						// If the delta ends up being larger than the full state, capture the full state instead
						CaptureRewindStateNonDelta(currentState);
						return;
					}

					// Length
					_tempBuf[offset++] = (byte)length;

					// Offset
					if (isSmall)
					{
						BitConverterLE.WriteBytes((ushort)beginChangeSequence, _tempBuf, offset);
						offset += 2;
					}
					else
					{
						BitConverterLE.WriteBytes((uint)beginChangeSequence, _tempBuf, offset);
						offset += 4;
					}

					// Data
					Buffer.BlockCopy(_lastState, beginChangeSequence, _tempBuf, offset, length);
					offset += length;

					inChangeSequence = false;
				}
			}

			if (_lastState.Length == currentState.Length)
			{
				Buffer.BlockCopy(currentState, 0, _lastState, 0, _lastState.Length);
			}
			else
			{
				_lastState = (byte[])currentState.Clone();
			}

			_rewindBuffer.Push(new ArraySegment<byte>(_tempBuf, 0, offset));
		}

		private void RewindLarge() 
		{
			RewindDelta(false); 
		}
		
		private void Rewind64K() 
		{
			RewindDelta(true); 
		}

		private void RewindDelta(bool isSmall)
		{
			if (Global.Emulator.HasSavestates())
			{
				var ms = _rewindBuffer.PopMemoryStream();
				var reader = new BinaryReader(ms);
				var fullstate = reader.ReadBoolean();
				if (fullstate)
				{
					Global.Emulator.AsStatable().LoadStateBinary(reader);
				}
				else
				{
					var output = new MemoryStream(_lastState);
					while (ms.Position < ms.Length)
					{
						var len = reader.ReadByte();
						int offset = isSmall ? reader.ReadUInt16() : reader.ReadInt32();

						output.Position = offset;
						output.Write(ms.GetBuffer(), (int)ms.Position, len);
						ms.Position += len;
					}

					reader.Close();
					output.Position = 0;
					Global.Emulator.AsStatable().LoadStateBinary(new BinaryReader(output));
				}
			}
		}
	}
}
