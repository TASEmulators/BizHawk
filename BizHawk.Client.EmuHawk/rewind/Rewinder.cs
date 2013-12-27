using System;
using System.IO;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class Rewinder
	{
		public bool RewindActive = true;

		private  StreamBlobDatabase RewindBuffer;
		private RewindThreader RewindThread;
		private byte[] LastState;
		private bool RewindImpossible;
		private int RewindFrequency = 1;
		private bool RewindDeltaEnable = false;
		private byte[] RewindFellationBuf;
		private byte[] TempBuf = new byte[0];

		// TODO: make RewindBuf never be null
		public float FullnessRatio
		{
			get { return RewindBuffer.FullnessRatio; }
		}
		
		public int Count
		{
			get { return RewindBuffer != null ? RewindBuffer.Count : 0; }
		}
		
		public long Size 
		{
			get { return RewindBuffer != null ? RewindBuffer.Size : 0; }
		}
		
		public int BufferCount
		{
			get { return RewindBuffer != null ? RewindBuffer.Count : 0; }
		}

		public bool HasBuffer
		{
			get { return RewindBuffer != null; }
		}

		// TOOD: this should not be parameterless?! It is only possible due to passing a static context in
		public void CaptureRewindState()
		{
			if (RewindImpossible)
			{
				return;
			}

			if (LastState == null)
			{
				DoRewindSettings();
			}

		
			//log a frame
			if (LastState != null && Global.Emulator.Frame % RewindFrequency == 0)
			{
				byte[] CurrentState = Global.Emulator.SaveStateBinary();
				RewindThread.Capture(CurrentState);
			}
		}

		public void DoRewindSettings()
		{
			// This is the first frame. Capture the state, and put it in LastState for future deltas to be compared against.
			LastState = (byte[])Global.Emulator.SaveStateBinary().Clone();

			int state_size = 0;
			if (LastState.Length >= Global.Config.Rewind_LargeStateSize)
			{
				SetRewindParams(Global.Config.RewindEnabledLarge, Global.Config.RewindFrequencyLarge);
				state_size = 3;
			}
			else if (LastState.Length >= Global.Config.Rewind_MediumStateSize)
			{
				SetRewindParams(Global.Config.RewindEnabledMedium, Global.Config.RewindFrequencyMedium);
				state_size = 2;
			}
			else
			{
				SetRewindParams(Global.Config.RewindEnabledSmall, Global.Config.RewindFrequencySmall);
				state_size = 1;
			}

			bool rewind_enabled = false;
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

			RewindDeltaEnable = Global.Config.Rewind_UseDelta;

			if (rewind_enabled)
			{
				long cap = Global.Config.Rewind_BufferSize * (long)1024 * (long)1024;

				if (RewindBuffer != null)
					RewindBuffer.Dispose();
				RewindBuffer = new StreamBlobDatabase(Global.Config.Rewind_OnDisk, cap, BufferManage);

				if (RewindThread != null)
					RewindThread.Dispose();
				RewindThread = new RewindThreader(this, Global.Config.Rewind_IsThreaded);
			}
		}

		public void Rewind(int frames)
		{
			RewindThread.Rewind(frames);
		}

		// TODO remove me
		public void _RunRewind(int frames)
		{
			for (int i = 0; i < frames; i++)
			{
				if (RewindBuffer.Count == 0 || (Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie.InputLogLength == 0))
				{
					return;
				}

				if (LastState.Length < 0x10000)
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
			if (RewindDeltaEnable)
			{
				if (LastState.Length <= 0x10000)
				{
					CaptureRewindStateDelta(coreSavestate, true);
				}
				else
				{
					CaptureRewindStateDelta(coreSavestate, false);
				}
			}
			else
			{
				CaptureRewindStateNonDelta(coreSavestate);
			}
		}

		public void ResetRewindBuffer()
		{
			if (RewindBuffer != null)
			{
				RewindBuffer.Clear();
			}

			RewindImpossible = false;
			LastState = null;
		}

		private void SetRewindParams(bool enabled, int frequency)
		{
			if (RewindActive != enabled)
			{
				GlobalWin.OSD.AddMessage("Rewind " + (enabled ? "Enabled" : "Disabled"));
			}

			if (RewindFrequency != frequency && enabled)
			{
				GlobalWin.OSD.AddMessage("Rewind frequency set to " + frequency);
			}

			RewindActive = enabled;
			RewindFrequency = frequency;

			if (!RewindActive)
			{
				LastState = null;
			}
		}

		private byte[] BufferManage(byte[] inbuf, long size, bool allocate)
		{
			if (allocate)
			{
				//if we have an appropriate buffer free, return it
				if (RewindFellationBuf != null && RewindFellationBuf.LongLength == size)
				{
					byte[] ret = RewindFellationBuf;
					RewindFellationBuf = null;
					return ret;
				}
				//otherwise, allocate it
				return new byte[size];
			}
			else
			{
				RewindFellationBuf = inbuf;
				return null;
			}
		}

		private void CaptureRewindStateNonDelta(byte[] CurrentState)
		{
			long offset = RewindBuffer.Enqueue(0, CurrentState.Length + 1);
			Stream stream = RewindBuffer.Stream;
			stream.Position = offset;

			//write the header for a non-delta frame
			stream.WriteByte(1); //i.e. true
			stream.Write(CurrentState, 0, CurrentState.Length);
		}

		private void CaptureRewindStateDelta(byte[] CurrentState, bool isSmall)
		{
			//in case the state sizes mismatch, capture a full state rather than trying to do anything clever
			if (CurrentState.Length != LastState.Length)
			{
				CaptureRewindStateNonDelta(CurrentState);
				return;
			}

			int beginChangeSequence = -1;
			bool inChangeSequence = false;
			MemoryStream ms;

			// try to set up the buffer in advance so we dont ever have exceptions in here
			if (TempBuf.Length < CurrentState.Length)
			{
				TempBuf = new byte[CurrentState.Length * 2];
			}

			ms = new MemoryStream(TempBuf, 0, TempBuf.Length, true, true); 
		RETRY:
			try
			{
				var writer = new BinaryWriter(ms);
				writer.Write(false); // delta state
				for (int i = 0; i < CurrentState.Length; i++)
				{
					if (inChangeSequence == false)
					{
						if (i >= LastState.Length)
						{
							continue;
						}

						if (CurrentState[i] == LastState[i])
						{
							continue;
						}

						inChangeSequence = true;
						beginChangeSequence = i;
						continue;
					}

					if (i - beginChangeSequence == 254 || i == CurrentState.Length - 1)
					{
						writer.Write((byte)(i - beginChangeSequence + 1));
						if (isSmall)
						{
							writer.Write((ushort)beginChangeSequence);
						}
						else
						{
							writer.Write(beginChangeSequence);
						}

						writer.Write(LastState, beginChangeSequence, i - beginChangeSequence + 1);
						inChangeSequence = false;
						continue;
					}

					if (CurrentState[i] == LastState[i])
					{
						writer.Write((byte)(i - beginChangeSequence));
						if (isSmall)
						{
							writer.Write((ushort)beginChangeSequence);
						}
						else
						{
							writer.Write(beginChangeSequence);
						}

						writer.Write(LastState, beginChangeSequence, i - beginChangeSequence);
						inChangeSequence = false;
					}
				}
			}
			catch (NotSupportedException)
			{
				//ok... we had an exception after all
				//if we did actually run out of room in the memorystream, then try it again with a bigger buffer
				TempBuf = new byte[TempBuf.Length * 2];
				goto RETRY;
			}

			if (LastState != null && LastState.Length == CurrentState.Length)
			{
				Buffer.BlockCopy(CurrentState, 0, LastState, 0, LastState.Length);
			}
			else
			{
				LastState = (byte[])CurrentState.Clone();
			}
		
			var seg = new ArraySegment<byte>(TempBuf, 0, (int)ms.Position);
			RewindBuffer.Push(seg);
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
			var ms = RewindBuffer.PopMemoryStream();
			var reader = new BinaryReader(ms);
			bool fullstate = reader.ReadBoolean();
			if (fullstate)
			{
				Global.Emulator.LoadStateBinary(reader);
			}
			else
			{
				var output = new MemoryStream(LastState);
				while (ms.Position < ms.Length - 1)
				{
					byte len = reader.ReadByte();
					int offset;
					if(isSmall)
						offset = reader.ReadUInt16();
					else offset = reader.ReadInt32();
					output.Position = offset;
					output.Write(ms.GetBuffer(), (int)ms.Position, len);
					ms.Position += len;
				}

				reader.Close();
				output.Position = 0;
				Global.Emulator.LoadStateBinary(new BinaryReader(output));
			}
		}
	}
}
