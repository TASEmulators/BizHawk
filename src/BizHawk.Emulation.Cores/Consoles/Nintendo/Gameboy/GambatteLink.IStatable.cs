using System.IO;

using Newtonsoft.Json;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : ITextStatable
	{
		public bool AvoidRewind
		{
			get
			{
				var ret = false;
				for (var i = 0; i < _numCores; i++)
				{
					ret |= _linkedCores[i].AvoidRewind;
				}

				return ret;
			}
		}

		public void SaveStateText(TextWriter writer)
		{
			ser.Serialize(writer, new GBLSerialized(this));
		}

		public void LoadStateText(TextReader reader)
		{
			var s = (GBLSerialized)ser.Deserialize(reader, typeof(GBLSerialized));
			if (s.NumCores != _numCores)
			{
				throw new InvalidOperationException("Core number mismatch!");
			}
			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].LoadState(s.LinkedStates[i]);
				_linkedOverflow[i] = s.LinkedOverflow[i];
				_linkedLatches[i] = s.LinkedLatches[i];
			}
			IsLagFrame = s.IsLagFrame;
			LagCount = s.LagCount;
			Frame = s.Frame;
			_linkConnected = s.LinkConnected;
			_linkDiscoSignal = s.LinkDiscoSignal;
			_linkShifted = s.LinkShifted;
			_linkShiftSignal = s.LinkShiftSignal;
			_linkSpaced = s.LinkSpaced;
			_linkSpaceSignal = s.LinkSpaceSignal;
			reader.ReadToEnd();
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			writer.Write(_numCores);
			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].SaveStateBinary(writer);
				writer.Write(_linkedOverflow[i]);
				writer.Write(_linkedLatches[i]);
			}
			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
			writer.Write(_linkConnected);
			writer.Write(_linkDiscoSignal);
			writer.Write(_linkShifted);
			writer.Write(_linkShiftSignal);
			writer.Write(_linkSpaced);
			writer.Write(_linkSpaceSignal);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			if (_numCores != reader.ReadInt32())
			{
				throw new InvalidOperationException("Core number mismatch!");
			}
			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].LoadStateBinary(reader);
				_linkedOverflow[i] = reader.ReadInt32();
				_linkedLatches[i] = reader.ReadInt32();
			}
			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
			_linkConnected = reader.ReadBoolean();
			_linkDiscoSignal = reader.ReadBoolean();
			_linkShifted = reader.ReadBoolean();
			_linkShiftSignal = reader.ReadBoolean();
			_linkSpaced = reader.ReadBoolean();
			_linkSpaceSignal = reader.ReadBoolean();
		}

		private readonly JsonSerializer ser = new JsonSerializer { Formatting = Formatting.Indented };

		private class GBLSerialized
		{
			public int NumCores;
			public TextState<Gameboy.TextStateData>[] LinkedStates;
			// other data
			public bool IsLagFrame;
			public int LagCount;
			public int Frame;
			public int[] LinkedOverflow;
			public int[] LinkedLatches;
			public bool LinkConnected;
			public bool LinkDiscoSignal;
			public bool LinkShifted;
			public bool LinkShiftSignal;
			public bool LinkSpaced;
			public bool LinkSpaceSignal;

			public GBLSerialized(GambatteLink linkcore)
			{
				if (linkcore == null)
				{
					return;
				}

				NumCores = linkcore._numCores;
				LinkedStates = new TextState<Gameboy.TextStateData>[NumCores];
				LinkedOverflow = new int[NumCores];
				LinkedLatches = new int[NumCores];
				for (int i = 0; i < NumCores; i++)
				{
					LinkedStates[i] = linkcore._linkedCores[i].SaveState();
					LinkedOverflow[i] = linkcore._linkedOverflow[i];
					LinkedLatches[i] = linkcore._linkedLatches[i];
				}
				IsLagFrame = linkcore.IsLagFrame;
				LagCount = linkcore.LagCount;
				Frame = linkcore.Frame;
				LinkConnected = linkcore._linkConnected;
				LinkDiscoSignal = linkcore._linkDiscoSignal;
				LinkShifted = linkcore._linkShifted;
				LinkShiftSignal = linkcore._linkShiftSignal;
				LinkSpaced = linkcore._linkSpaced;
				LinkSpaceSignal = linkcore._linkSpaceSignal;
			}
		}
	}
}
