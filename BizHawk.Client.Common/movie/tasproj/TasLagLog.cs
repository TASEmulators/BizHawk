using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	public class TasLagLog
	{
		// TODO: Change this into a regular list.
		private List<bool> LagLog = new List<bool>();

		private List<bool> WasLag = new List<bool>();

		public bool? this[int frame]
		{
			get
			{
				if (frame < LagLog.Count)
				{
					if (frame < 0)
						return null;
					else
						return LagLog[frame];
				}
				else if (frame == Global.Emulator.Frame && frame == LagLog.Count)
				{
					// LagLog[frame] = Global.Emulator.AsInputPollable().IsLagFrame; // Note: Side effects!
					return Global.Emulator.AsInputPollable().IsLagFrame;
				}

				return null;
			}

			set
			{
				if (!value.HasValue)
				{
					LagLog.RemoveAt(frame);
					return;
				}
				else if (frame < 0)
				{
					return; // Nothing to do
				}

				if (frame > LagLog.Count)
				{
					System.Diagnostics.Debug.Print("Lag Log error. f" + frame + ", log: " + LagLog.Count);
					return; // Can this break anything?
				}

				bool wasValue;
				if (frame < LagLog.Count)
					wasValue = LagLog[frame];
				else if (frame == WasLag.Count)
					wasValue = value.Value;
				else
					wasValue = WasLag[frame];

				if (frame == WasLag.Count)
					WasLag.Add(wasValue);
				else
					WasLag[frame] = wasValue;

				if (frame != 0)
					WasLag[frame - 1] = LagLog[frame - 1];
				if (frame >= LagLog.Count)
					LagLog.Add(value.Value);
				else
					LagLog[frame] = value.Value;
			}
		}

		public void Clear()
		{
			LagLog.Clear();
		}

		public bool RemoveFrom(int frame)
		{
			if (LagLog.Count > frame && frame >= 0)
			{
				LagLog.RemoveRange(frame + 1, LagLog.Count - frame - 1);
				return true;
			}
			return false;
		}

		public void RemoveHistoryAt(int frame)
		{
			WasLag.RemoveAt(frame);
		}
		public void InsertHistoryAt(int frame, bool isLag)
		{ // LagLog was invalidated when the frame was inserted
			if (frame <= LagLog.Count)
				LagLog.Insert(frame, isLag);
			WasLag.Insert(frame, isLag);
		}

		public void Save(BinaryWriter bw)
		{
			bw.Write((byte)1); // New saving format.
			bw.Write(LagLog.Count);
			bw.Write(WasLag.Count);
			for (int i = 0; i < LagLog.Count; i++)
			{
				bw.Write(LagLog[i]);
				bw.Write(WasLag[i]);
			}
			for (int i = LagLog.Count; i < WasLag.Count; i++)
				bw.Write(WasLag[i]);
		}

		public void Load(BinaryReader br)
		{
			LagLog.Clear();
			WasLag.Clear();
			//if (br.BaseStream.Length > 0)
			//{ BaseStream.Length does not return the expected value.
			int formatVersion = br.ReadByte();
			if (formatVersion == 0)
			{
				int length = (br.ReadByte() << 8) | formatVersion; // The first byte should be a part of length.
				length = (br.ReadInt16() << 16) | length;
				for (int i = 0; i < length; i++)
				{
					br.ReadInt32();
					LagLog.Add(br.ReadBoolean());
					WasLag.Add(LagLog.Last());
				}
			}
			else if (formatVersion == 1)
			{
				int length = br.ReadInt32();
				int lenWas = br.ReadInt32();
				for (int i = 0; i < length; i++)
				{
					LagLog.Add(br.ReadBoolean());
					WasLag.Add(br.ReadBoolean());
				}
				for (int i = length; i < lenWas; i++)
					WasLag.Add(br.ReadBoolean());
			}
			//}
		}

		public bool? History(int frame)
		{
			if (frame < WasLag.Count)
			{
				if (frame < 0)
					return null;

				return WasLag[frame];
			}

			return null;
		}

		public int LastValidFrame
		{
			get
			{
				if (LagLog.Count == 0)
					return 0;
				return LagLog.Count - 1;
			}
		}

		public TasLagLog Clone()
		{
			var log = new TasLagLog();
			log.LagLog = LagLog.ToList();
			log.WasLag = WasLag.ToList();

			return log;
		}

		public void FromLagLog(TasLagLog log)
		{
			LagLog = log.LagLog.ToList();
			WasLag = log.WasLag.ToList();
		}

		public void StartFromFrame(int index)
		{
			LagLog.RemoveRange(0, index);
			WasLag.RemoveRange(0, index);
		}
	}
}
