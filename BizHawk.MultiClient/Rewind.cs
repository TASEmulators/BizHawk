using System.IO;

namespace BizHawk.MultiClient
{
	public partial class MainForm
	{
		MruStack<MemoryStream> RewindBuf = new MruStack<MemoryStream>(15000);
		byte[] LastState;
        bool RewindImpossible = false;

		void CaptureRewindState()
		{
            if (RewindImpossible)
                return;

			if (LastState == null)
			{
				// This is the first frame. Capture the state, and put it in LastState for future deltas to be compared against.
				LastState = Global.Emulator.SaveStateBinary();
                if (LastState.Length > 0x100000)
                {
                    RewindImpossible = true;
                    LastState = null;
                    Global.OSD.AddMessage("Rewind Disabled: State too large.");
										Global.OSD.AddMessage("See 'Arcade Card Rewind Hack' in Emulation->PC Engine options.");
                }

				return;
			}

			// Otherwise, it's not the first frame, so build a delta.
            if (LastState.Length <= 0x10000)
				CaptureRewindState64K();
			else
				CaptureRewindStateLarge();
		}

		// Builds a delta for states that are <= 64K in size.
		void CaptureRewindState64K()
		{
			byte[] CurrentState = Global.Emulator.SaveStateBinary();
			int beginChangeSequence = -1;
			bool inChangeSequence = false;
			var ms = new MemoryStream();
			var writer = new BinaryWriter(ms);
			if (CurrentState.Length != LastState.Length)
			{
				writer.Write(true); // full state
				writer.Write(CurrentState);
			}
			else
			{
				writer.Write(false); // delta state
				for (int i = 0; i < CurrentState.Length; i++)
				{
					if (inChangeSequence == false)
					{
						if (i >= LastState.Length)
							continue;
						if (CurrentState[i] == LastState[i])
							continue;

						inChangeSequence = true;
						beginChangeSequence = i;
						continue;
					}

					if (i - beginChangeSequence == 254 || i == CurrentState.Length - 1)
					{
						writer.Write((byte)(i - beginChangeSequence + 1));
						writer.Write((ushort)beginChangeSequence);
						writer.Write(LastState, beginChangeSequence, i - beginChangeSequence + 1);
						inChangeSequence = false;
						continue;
					}

					if (CurrentState[i] == LastState[i])
					{
						writer.Write((byte)(i - beginChangeSequence));
						writer.Write((ushort)beginChangeSequence);
						writer.Write(LastState, beginChangeSequence, i - beginChangeSequence);
						inChangeSequence = false;
					}
				}
			}
			LastState = CurrentState;
			ms.Position = 0;
			RewindBuf.Push(ms);
		}

		// Builds a delta for states that are > 64K in size.
		void CaptureRewindStateLarge()
		{
			byte[] CurrentState = Global.Emulator.SaveStateBinary();
			int beginChangeSequence = -1;
			bool inChangeSequence = false;
			var ms = new MemoryStream();
			var writer = new BinaryWriter(ms);
			if (CurrentState.Length != LastState.Length)
			{
				writer.Write(true); // full state
				writer.Write(CurrentState);
			}
			else
			{
				writer.Write(false); // delta state
				for (int i = 0; i < CurrentState.Length; i++)
				{
					if (inChangeSequence == false)
					{
						if (i >= LastState.Length)
							continue;
						if (CurrentState[i] == LastState[i])
							continue;

						inChangeSequence = true;
						beginChangeSequence = i;
						continue;
					}

					if (i - beginChangeSequence == 254 || i == CurrentState.Length - 1)
					{
						writer.Write((byte)(i - beginChangeSequence + 1));
						writer.Write(beginChangeSequence);
						writer.Write(LastState, beginChangeSequence, i - beginChangeSequence + 1);
						inChangeSequence = false;
						continue;
					}

					if (CurrentState[i] == LastState[i])
					{
						writer.Write((byte)(i - beginChangeSequence));
						writer.Write(beginChangeSequence);
						writer.Write(LastState, beginChangeSequence, i - beginChangeSequence);
						inChangeSequence = false;
					}
				}
			}
			LastState = CurrentState;
			ms.Position = 0;
			RewindBuf.Push(ms);
		}

		void Rewind64K()
		{
			var ms = RewindBuf.Pop();
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
					ushort offset = reader.ReadUInt16();
					output.Position = offset;
					output.Write(ms.GetBuffer(), (int)ms.Position, len);
					ms.Position += len;
				}
				reader.Close();
				output.Position = 0;
				Global.Emulator.LoadStateBinary(new BinaryReader(output));
			}
		}

		void RewindLarge()
		{
			var ms = RewindBuf.Pop();
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
					int offset = reader.ReadInt32();
					output.Position = offset;
					output.Write(ms.GetBuffer(), (int)ms.Position, len);
					ms.Position += len;
				}
				reader.Close();
				output.Position = 0;
				Global.Emulator.LoadStateBinary(new BinaryReader(output));
			}
		}

		public void Rewind(int frames)
		{
			for (int i = 0; i < frames; i++)
			{
				if (RewindBuf.Count == 0 || (true == Global.MovieSession.Movie.Loaded && 0 == Global.MovieSession.Movie.Frames))
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

		public void ResetRewindBuffer()
		{
			RewindBuf.Clear();
            RewindImpossible = false;
			LastState = null;
		}

        public int RewindBufferCount()
        {
            return RewindBuf.Count;
        }
	}
}
