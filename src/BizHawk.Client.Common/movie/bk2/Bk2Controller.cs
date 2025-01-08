using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal class Bk2Controller : IMovieController
	{
		private readonly Dictionary<string, int> _myAxisControls = new();

		private readonly Dictionary<string, bool> _myBoolButtons = new();

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

		public int AxisValue(string name)
			=> _myAxisControls.GetValueOrDefault(name);

		public bool IsPressed(string button)
			=> _myBoolButtons.GetValueOrDefault(button);

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

		public void SetFromMnemonic(string mnemonic)
		{
			if (string.IsNullOrWhiteSpace(mnemonic)) return;
			var iterator = 0;

			foreach (var playerControls in Definition.ControlsOrdered)
			foreach ((string buttonName, AxisSpec? axisSpec) in playerControls)
			{
				while (mnemonic[iterator] == '|') iterator++;

				if (axisSpec.HasValue)
				{
					var commaIndex = mnemonic.IndexOf(',', iterator);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
					var val = int.Parse(mnemonic.AsSpan(start: iterator, length: commaIndex - iterator));
#else
					var axisValueString = mnemonic.Substring(startIndex: iterator, length: commaIndex - iterator);
					var val = int.Parse(axisValueString);
#endif
					_myAxisControls[buttonName] = val;

					iterator = commaIndex + 1;
				}
				else
				{
					_myBoolButtons[buttonName] = mnemonic[iterator] != '.';
					iterator++;
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
