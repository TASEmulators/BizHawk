using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Common
{
	// the idea here is that various connected peripherals have their controls all merged
	// into one definition, including logic to unmerge the data back so each one can work
	// with it without knowing what else is connected

	public static class ControllerDefinitionMerger
	{
		private static string Allocate(string input, ref int plr, ref int plrnext)
		{
			int offset = int.Parse(input.Substring(0, 1));
			int currplr = plr + offset;
			if (currplr >= plrnext)
				plrnext = currplr + 1;
			return string.Format("P{0} {1}", currplr, input.Substring(1));
		}

		/// <summary>
		/// merge some controller definitions for different ports, and such.  i promise to fully document this tomorrow
		/// </summary>
		/// <param name="Controllers"></param>
		/// <returns></returns>
		public static ControllerDefinition GetMerged(IEnumerable<ControllerDefinition> Controllers, out List<ControlDefUnMerger> Unmergers)
		{
			ControllerDefinition ret = new ControllerDefinition();
			Unmergers = new List<ControlDefUnMerger>();
			int plr = 1;
			int plrnext = 1;
			foreach (var def in Controllers)
			{
				Dictionary<string, string> remaps = new Dictionary<string, string>();

				foreach (string s in def.BoolButtons)
				{
					string r = Allocate(s, ref plr, ref plrnext);
					ret.BoolButtons.Add(r);
					remaps[s] = r;
				}
				foreach (string s in def.FloatControls)
				{
					string r = Allocate(s, ref plr, ref plrnext);
					ret.FloatControls.Add(r);
					remaps[s] = r;
				}
				ret.FloatRanges.AddRange(def.FloatRanges);
				plr = plrnext;
				Unmergers.Add(new ControlDefUnMerger(remaps));
			}
			return ret;
		}
	}

	public class ControlDefUnMerger
	{
		Dictionary<string, string> Remaps;

		public ControlDefUnMerger(Dictionary<string, string> Remaps)
		{
			this.Remaps = Remaps;
		}

		private class DummyController : IController
		{
			IController src;
			Dictionary<string, string> remaps;
			public DummyController(IController src, Dictionary<string, string> remaps)
			{
				this.src = src;
				this.remaps = remaps;
			}

			public ControllerDefinition Definition { get { throw new NotImplementedException(); } }

			public bool this[string button] { get { return IsPressed(button); } }

			public bool IsPressed(string button)
			{
				return src.IsPressed(remaps[button]);
			}

			public float GetFloat(string name)
			{
				return src.GetFloat(remaps[name]);
			}
		}

		public IController UnMerge(IController c)
		{
			return new DummyController(c, Remaps);
		}

	}
}
