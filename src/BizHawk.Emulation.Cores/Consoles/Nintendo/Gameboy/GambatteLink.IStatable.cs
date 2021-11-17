using System;
using System.IO;

using Newtonsoft.Json;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : ITextStatable
	{
		public void SaveStateText(TextWriter writer)
		{
			ser.Serialize(writer, new DGBSerialized(this));
		}

		public void LoadStateText(TextReader reader)
		{
			var s = (DGBSerialized)ser.Deserialize(reader, typeof(DGBSerialized));
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
			_cableconnected = s.cableconnected;
			_cablediscosignal = s.cablediscosignal;
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
			writer.Write(_cableconnected);
			writer.Write(_cablediscosignal);
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
			_cableconnected = reader.ReadBoolean();
			_cablediscosignal = reader.ReadBoolean();
		}

		private readonly JsonSerializer ser = new JsonSerializer { Formatting = Formatting.Indented };

		private class DGBSerialized
		{
			public int NumCores;
			public TextState<Gameboy.TextStateData>[] LinkedStates;
			// other data
			public bool IsLagFrame;
			public int LagCount;
			public int Frame;
			public int[] LinkedOverflow;
			public int[] LinkedLatches;
			public bool cableconnected;
			public bool cablediscosignal;

			public DGBSerialized(GambatteLink linkcore)
			{
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
				cableconnected = linkcore._cableconnected;
				cablediscosignal = linkcore._cablediscosignal;
			}
		}
	}
}
