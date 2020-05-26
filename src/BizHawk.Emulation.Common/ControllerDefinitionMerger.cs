using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	// the idea here is that various connected peripherals have their controls all merged
	// into one definition, including logic to unmerge the data back so each one can work
	// with it without knowing what else is connected
	public static class ControllerDefinitionMerger
	{
		private static string Allocate(string input, ref int plr, ref int playerNext)
		{
			int offset = int.Parse(input.Substring(0, 1));
			int currentPlayer = plr + offset;
			if (currentPlayer >= playerNext)
			{
				playerNext = currentPlayer + 1;
			}

			return $"P{currentPlayer} {input.Substring(1)}";
		}

		/// <summary>
		/// merge some controller definitions for different ports, and such.  i promise to fully document this tomorrow
		/// </summary>
		public static ControllerDefinition GetMerged(IEnumerable<ControllerDefinition> controllers, out List<ControlDefUnMerger> unmergers)
		{
			ControllerDefinition ret = new ControllerDefinition();
			unmergers = new List<ControlDefUnMerger>();
			int plr = 1;
			int playerNext = 1;
			foreach (var def in controllers)
			{
				var remaps = new Dictionary<string, string>();

				foreach (string s in def.BoolButtons)
				{
					string r = Allocate(s, ref plr, ref playerNext);
					ret.BoolButtons.Add(r);
					remaps[s] = r;
				}

				foreach (string s in def.AxisControls)
				{
					string r = Allocate(s, ref plr, ref playerNext);
					ret.AxisControls.Add(r);
					remaps[s] = r;
				}

				ret.AxisRanges.AddRange(def.AxisRanges);
				plr = playerNext;
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

			/// <exception cref="NotImplementedException">always</exception>
			public ControllerDefinition Definition => throw new NotImplementedException();

			public bool IsPressed(string button)
			{
				return _src.IsPressed(_remaps[button]);
			}

			public int AxisValue(string name)
			{
				return _src.AxisValue(_remaps[name]);
			}
		}

		public IController UnMerge(IController c)
		{
			return new DummyController(c, _remaps);
		}
	}
}
