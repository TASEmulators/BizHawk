using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal class Bk2Controller : IMovieController
	{
		private readonly WorkingDictionary<string, bool> _myBoolButtons = new();
		private readonly WorkingDictionary<string, int> _myAxisControls = new();

		public Bk2Controller(ControllerDefinition definition, string logKey) : this(definition)
		{
			if (!string.IsNullOrEmpty(logKey))
				Definition = new Bk2ControllerDefinition(definition, logKey);
		}

		public Bk2Controller(ControllerDefinition definition)
		{
			Definition = definition;
			foreach ((string axisName, AxisSpec range) in definition.Axes)
			{
				_myAxisControls[axisName] = range.Neutral;
			}
		}

		public ControllerDefinition Definition { get; }

		public bool IsPressed(string button) => _myBoolButtons[button];
		public int AxisValue(string name) => _myAxisControls[name];

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() => Array.Empty<(string, int)>();

		public void SetHapticChannelStrength(string name, int strength) {}

		public void SetFrom(IController source)
		{
			for (int index = 0; index < Definition.BoolButtons.Count; index++)
			{
				string button = Definition.BoolButtons[index];
				_myBoolButtons[button] = source.IsPressed(button);
			}

			foreach (var name in Definition.Axes.Keys)
			{
				_myAxisControls[name] = source.AxisValue(name);
			}
		}

		public void SetFromSticky(IStickyAdapter controller)
		{
			foreach (var button in Definition.BoolButtons)
			{
				_myBoolButtons[button] = controller.IsSticky(button);
			}

			// axes don't have sticky logic, so latch default value
			foreach (var (k, v) in Definition.Axes) _myAxisControls[k] = v.Neutral;
		}

		public void SetFromMnemonic(string mnemonic)
		{
			if (!string.IsNullOrWhiteSpace(mnemonic))
			{
				var iterator = 0;

				foreach (var playerControls in Definition.ControlsOrdered)
				foreach ((string buttonName, AxisSpec? axisSpec) in playerControls)
				{
					while (mnemonic[iterator] == '|') iterator++;

					if (axisSpec.HasValue)
					{
						var commaIndex = mnemonic.IndexOf(',', iterator);
#if NET6_0_OR_GREATER
						var val = int.Parse(mnemonic.AsSpan(iterator, commaIndex));
#else
						var axisValueString = mnemonic.Substring(iterator, commaIndex);
						var val = int.Parse(axisValueString);
#endif
						_myAxisControls[buttonName] = val;

						iterator += commaIndex + 1;
					}
					else
					{
						_myBoolButtons[buttonName] = mnemonic[iterator] != '.';
						iterator++;
					}
				}
			}
		}

		public void SetBool(string buttonName, bool value)
		{
			_myBoolButtons[buttonName] = value;
		}

		public void SetAxis(string buttonName, int value)
		{
			_myAxisControls[buttonName] = value;
		}

		private class Bk2ControllerDefinition : ControllerDefinition
		{
			private readonly IReadOnlyList<IReadOnlyList<(string, AxisSpec?)>> _controlsFromLogKey;

			public Bk2ControllerDefinition(ControllerDefinition sourceDefinition, string logKey)
				: base(sourceDefinition)
			{
				var groups = logKey.Split(new[] { "#" }, StringSplitOptions.RemoveEmptyEntries);

				_controlsFromLogKey = groups
					.Select(group => group.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries)
						.Select(buttonname => (buttonname, sourceDefinition.Axes.TryGetValue(buttonname, out var axisSpec) ? axisSpec : (AxisSpec?)null))
						.ToArray())
					.ToArray();
			}

			protected override IReadOnlyList<IReadOnlyList<(string Name, AxisSpec? AxisSpec)>> GenOrderedControls() => _controlsFromLogKey;
		}
	}
}
