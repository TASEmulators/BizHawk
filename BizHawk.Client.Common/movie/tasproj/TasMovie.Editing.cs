using System;
using System.IO;
using System.Linq;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public partial class TasMovie
	{
		public override void RecordFrame(int frame, Emulation.Common.IController source)
		{
			base.RecordFrame(frame, source);

			if (frame < LagLog.Count)
			{
				LagLog.RemoveRange(frame, LagLog.Count - frame);
			}

			LagLog.Add(Global.Emulator.IsLagFrame);

			StateManager.Invalidate(frame);
			StateManager.Capture();
		}

		public override void Truncate(int frame)
		{
			base.Truncate(frame);

			LagLog.RemoveRange(frame + 2, LagLog.Count - frame - 1);
			StateManager.Invalidate(frame + 1);
			// TODO: Markers? What does taseditor do?
		}

		public override void PokeFrame(int frame, Emulation.Common.IController source)
		{
			base.PokeFrame(frame, source);

			LagLog.RemoveRange(frame, LagLog.Count - frame);
			StateManager.Invalidate(frame);
		}

		public override void ClearFrame(int frame)
		{
			base.ClearFrame(frame);

			LagLog.RemoveRange(frame + 1, LagLog.Count - frame - 1);
			StateManager.Invalidate(frame + 1);
		}

		public void RemoveFrames(int[] frames)
		{
			if (frames.Any())
			{
				var truncateStatesTo = frames.Min(x => x);
				foreach (var frame in frames.OrderByDescending(x => x)) // Removin them in reverse order allows us to remove by index;
				{
					_log.RemoveAt(frame);
				}

				StateManager.Invalidate(truncateStatesTo);
				Changes = true;
			}
		}
	}
}
