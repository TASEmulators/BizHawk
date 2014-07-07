using System;
using System.IO;
using System.Linq;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public partial class TasMovie
	{
		// TODO: all these
		public override void RecordFrame(int frame, Emulation.Common.IController source)
		{
			base.RecordFrame(frame, source);

			LagLog.RemoveRange(frame, LagLog.Count - frame);
			LagLog.Add(Global.Emulator.IsLagFrame);

			StateManager.Invalidate(frame);
			StateManager.Capture();
		}

		public override void Truncate(int frame)
		{
			base.Truncate(frame);

			LagLog.RemoveRange(frame + 2, LagLog.Count - frame - 1);
			StateManager.Invalidate(frame + 1);
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
	}
}
