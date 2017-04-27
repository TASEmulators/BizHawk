using System;
using System.Collections.Generic;

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
			{
				plrnext = currplr + 1;
			}

			return $"P{currplr} {input.Substring(1)}";
		}

		/// <summary>
		/// merge some controller definitions for different ports, and such.  i promise to fully document this tomorrow
		/// </summary>
		public static ControllerDefinition GetMerged(IEnumerable<ControllerDefinition> controllers, out List<ControlDefUnMerger> unmergers)
		{
			ControllerDefinition ret = new ControllerDefinition();
			unmergers = new List<ControlDefUnMerger>();
			int plr = 1;
			int plrnext = 1;
			foreach (var def in controllers)
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
				unmergers.Add(new ControlDefUnMerger(remaps));
			}

			return ret;
		}
	}

	public class ControlDefUnMerger
	{
		private readonly Dictionary<string, string> _remaps;

		public ControlDefUnMerger(Dictionary<string, string> remaps)
		{
			_remaps = remaps;
		}

		private class DummyController : IController
		{
			private readonly IController _src;
			private readonly Dictionary<string, string> _remaps;

			public DummyController(IController src, Dictionary<string, string> remaps)
			{
				_src = src;
				_remaps = remaps;
			}

			public ControllerDefinition Definition
			{
				get { throw new NotImplementedException(); }
			}

			public bool IsPressed(string button)
			{
				return _src.IsPressed(_remaps[button]);
			}

			public float GetFloat(string name)
			{
				return _src.GetFloat(_remaps[name]);
			}
		}

		public IController UnMerge(IController c)
		{
			return new DummyController(c, _remaps);
		}
	}
}
