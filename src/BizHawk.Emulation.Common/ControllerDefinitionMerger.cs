using System;
using System.Collections.Generic;
using System.Linq;

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
				Dictionary<string, string> buttonAxisRemaps = new();
				Dictionary<string, string> feedbackRemaps = new();

				foreach (string s in def.BoolButtons)
				{
					string r = Allocate(s, ref plr, ref playerNext);
					ret.BoolButtons.Add(r);
					buttonAxisRemaps[s] = r;
				}

				foreach (var kvp in def.Axes)
				{
					string r = Allocate(kvp.Key, ref plr, ref playerNext);
					ret.Axes.Add(r, kvp.Value);
					buttonAxisRemaps[kvp.Key] = r;
				}

				foreach (var s in def.HapticsChannels)
				{
					string r = Allocate(s, ref plr, ref playerNext);
					ret.HapticsChannels.Add(r);
					feedbackRemaps[s] = r;
				}

				plr = playerNext;
				unmergers.Add(new ControlDefUnMerger(buttonAxisRemaps, feedbackRemaps));
			}

			return ret;
		}
	}

	public class ControlDefUnMerger
	{
		private class DummyController : IController
		{
			/// <inheritdoc cref="ControlDefUnMerger._buttonAxisRemaps"/>
			private readonly IReadOnlyDictionary<string, string> _buttonAxisRemaps;

			/// <inheritdoc cref="ControlDefUnMerger._buttonAxisRemaps"/>
			private readonly IReadOnlyDictionary<string, string> _feedbackRemaps;

			private readonly IController _src;

			public DummyController(
				IController src,
				IReadOnlyDictionary<string, string> buttonAxisRemaps,
				IReadOnlyDictionary<string, string> feedbackRemaps)
			{
				_src = src;
				_buttonAxisRemaps = buttonAxisRemaps;
				_feedbackRemaps = feedbackRemaps;
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

			public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot()
				=> _src.GetHapticsSnapshot()
					.Select(hapticsEntry => (_feedbackRemaps.First(kvpRemap => kvpRemap.Value == hapticsEntry.Name).Value, hapticsEntry.Strength)) // reverse lookup
					.ToArray();

			public void SetHapticChannelStrength(string name, int strength) => _src.SetHapticChannelStrength(_feedbackRemaps[name], strength);
		}

		/// <remarks>these need to be separate because it's expected that <c>"P1 Left"</c> will appear in both</remarks>
		private readonly IReadOnlyDictionary<string, string> _buttonAxisRemaps;

		/// <inheritdoc cref="_buttonAxisRemaps"/>
		private readonly IReadOnlyDictionary<string, string> _feedbackRemaps;

		public ControlDefUnMerger(
			IReadOnlyDictionary<string, string> buttonAxisRemaps,
			IReadOnlyDictionary<string, string> feedbackRemaps)
		{
			_buttonAxisRemaps = buttonAxisRemaps;
			_feedbackRemaps = feedbackRemaps;
		}

		public IController UnMerge(IController c) => new DummyController(c, _buttonAxisRemaps, _feedbackRemaps);
	}
}
