using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	public partial class TasMovie
	{
		public override void RecordFrame(int frame, IController source)
		{
			base.RecordFrame(frame, source);

			LagLog.RemoveFrom(frame);
			LagLog[frame] = Global.Emulator.AsInputPollable().IsLagFrame;

			StateManager.Capture();
		}

		public override void Truncate(int frame)
		{
			if (frame < _log.Count - 1)
			{
				Changes = true;
			}

			base.Truncate(frame);

			LagLog.RemoveFrom(frame);
			StateManager.Invalidate(frame);
			Markers.TruncateAt(frame);
		}

		public override void PokeFrame(int frame, IController source)
		{
			base.PokeFrame(frame, source);
			InvalidateAfter(frame);
		}

		public override void ClearFrame(int frame)
		{
			base.ClearFrame(frame);
			InvalidateAfter(frame);
		}

		public void RemoveFrames(int[] frames)
		{
			if (frames.Any())
			{
				var invalidateAfter = frames.Min(x => x);
				foreach (var frame in frames.OrderByDescending(x => x)) // Removin them in reverse order allows us to remove by index;
				{
					_log.RemoveAt(frame);
				}

				Changes = true;
				InvalidateAfter(invalidateAfter);
			}
		}

		public void InsertInput(int frame, IEnumerable<string> inputLog)
		{
			_log.InsertRange(frame, inputLog);
			Changes = true;
			InvalidateAfter(frame);
		}

		public void InsertInput(int frame, IEnumerable<IController> inputStates)
		{
			var lg = LogGeneratorInstance();
			
			var inputLog = new List<string>();

			foreach (var input in inputStates)
			{
				lg.SetSource(input);
				inputLog.Add(lg.GenerateLogEntry());
			}
			
			InsertInput(frame, inputLog);
		}

		public void CopyOverInput(int frame, IEnumerable<IController> inputStates)
		{
			var lg = LogGeneratorInstance();
			var states = inputStates.ToList();
			for (int i = 0; i < states.Count; i++)
			{
				lg.SetSource(states[i]);
				_log[frame + i] = lg.GenerateLogEntry();
			}

			Changes = true;
			InvalidateAfter(frame);
		}

		public void InsertEmptyFrame(int frame, int count = 1)
		{
			var lg = LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.MovieControllerInstance());

			for (int i = 0; i < count; i++)
			{
				_log.Insert(frame, lg.EmptyEntry);
			}

			Changes = true;
			InvalidateAfter(frame - 1);
		}
	}
}
