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
		private readonly SortedList<int, bool> LagLog = new SortedList<int, bool>();

		private readonly SortedList<int, bool> RemovedFrames = new SortedList<int, bool>();

		public bool? this[int frame]
		{
			get
			{
				if (LagLog.ContainsKey(frame))
				{
					return LagLog[frame];
				}
				else if (frame == Global.Emulator.Frame)
				{
					if (frame == LagLog.Count)
					{
						LagLog[frame] = Global.Emulator.AsInputPollable().IsLagFrame; // Note: Side effects!
					}

					return Global.Emulator.AsInputPollable().IsLagFrame;
				}

				return null;
			}

			set
			{
				if (!value.HasValue)
				{
					LagLog.Remove(frame);
				}
				else if (frame < 0)
				{
					return; // Nothing to do
				}
				else if (LagLog.ContainsKey(frame))
				{
					LagLog[frame] = value.Value;
				}
				else
				{
					LagLog.Add(frame, value.Value);
				}
			}
		}

		public void Clear()
		{
			for (int i = 0; i < LagLog.Count; i++)
			{
				RemovedFrames.Remove(LagLog.Keys[i]);
				RemovedFrames.Add(i, LagLog[i]);
			}

			LagLog.Clear();

		}

		public void RemoveFrom(int frame)
		{
			for (int i = LagLog.Count - 1; i > frame; i--) // Reverse order because removing from a sorted list re-indexes the items after the removed item
			{
				RemovedFrames.Remove(LagLog.Keys[i]);
				RemovedFrames.Add(LagLog.Keys[i], LagLog.Values[i]); // use .Keys[i] instead of [i] here because indizes might not be consistent with keys
				LagLog.Remove(LagLog.Keys[i]);
			}

		}

		public void Save(BinaryWriter bw)
		{
			bw.Write(LagLog.Count);
			foreach (var kvp in LagLog)
			{
				bw.Write(kvp.Key);
				bw.Write(kvp.Value);
			}
		}

		public void Load(BinaryReader br)
		{
			LagLog.Clear();
			if (br.BaseStream.Length > 0)
			{
				int length = br.ReadInt32();
				for (int i = 0; i < length; i++)
				{
					LagLog.Add(br.ReadInt32(), br.ReadBoolean());
				}
			}
		}

		public bool? History(int frame)
		{
			if (RemovedFrames.ContainsKey(frame))
			{
				return RemovedFrames[frame];
			}

			return null;
		}
	}
}
