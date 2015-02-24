using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	public class TasLagLog
	{
		private readonly SortedList<int, bool> LagLog = new SortedList<int, bool>();

		// TODO: eventually we want multiple levels of history
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
			if (LagLog.Any())
			{
				RemovedFrames.Clear();
				foreach (var lag in LagLog)
				{
					RemovedFrames.Add(lag.Key, lag.Value);
				}
			}

			LagLog.Clear();
			
		}

		public void RemoveFrom(int frame)
		{
			if (frame > 0 && frame <= LagLog.Count)
			{
				RemovedFrames.Clear();

				for (int i = LagLog.Count - 1; i > frame; i--) // Reverse order because removing from a sorted list re-indexes the items after the removed item
				{
					RemovedFrames.Add(i, LagLog[i]);
					LagLog.RemoveAt(i);
				}
			}
			else if (frame == 0)
			{
				RemovedFrames.Clear();
				foreach (var lag in LagLog)
				{
					RemovedFrames.Add(lag.Key, lag.Value);
				}
				this.Clear();
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
