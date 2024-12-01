#nullable disable

using System.Collections.Generic;

using BizHawk.Common;

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
		public static ControllerDefinition GetMerged(
			string mergedName,
			IEnumerable<ControllerDefinition> controllers,
			out List<ControlDefUnMerger> unmergers)
		{
			ControllerDefinition ret = new(mergedName);
			unmergers = new List<ControlDefUnMerger>();
			int plr = 1;
			int playerNext = 1;
			foreach (var def in controllers)
			{
				Dictionary<string, string> buttonAxisRemaps = new();

				foreach (string s in def.BoolButtons)
				{
					string r = Allocate(s, ref plr, ref playerNext);
					ret.BoolButtons.Add(r);
					buttonAxisRemaps[s] = r;
				}

				foreach (var (k, v) in def.Axes)
				{
					var r = Allocate(k, ref plr, ref playerNext);
					ret.Axes.Add(r, v);
					buttonAxisRemaps[k] = r;
				}

				plr = playerNext;
				unmergers.Add(new ControlDefUnMerger(buttonAxisRemaps));
			}

			return ret;
		}
	}

	public class ControlDefUnMerger
	{
		private class DummyController : IController
		{
			private readonly IReadOnlyDictionary<string, string> _buttonAxisRemaps;

			private readonly IController _src;

			public DummyController(
				IController src,
				IReadOnlyDictionary<string, string> buttonAxisRemaps)
			{
				_src = src;
				_buttonAxisRemaps = buttonAxisRemaps;
			}

			/// <exception cref="NotImplementedException">always</exception>
			public ControllerDefinition Definition => throw new NotImplementedException();

			public bool IsPressed(string button)
			{
				return _src.IsPressed(_buttonAxisRemaps[button]);
			}

			public int AxisValue(string name)
			{
				return _src.AxisValue(_buttonAxisRemaps[name]);
			}

			public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Array.Empty<(string, int)>();

			public void SetHapticChannelStrength(string name, int strength) {}
		}

		private readonly IReadOnlyDictionary<string, string> _buttonAxisRemaps;

		public ControlDefUnMerger(IReadOnlyDictionary<string, string> buttonAxisRemaps)
		{
			_buttonAxisRemaps = buttonAxisRemaps;
		}

		public IController UnMerge(IController c) => new DummyController(c, _buttonAxisRemaps);
	}
}
