using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Generates a display friendly version of the input log entry
	/// using .bk2 mnemonics as the basis for display
	/// </summary>
	public class Bk2InputDisplayGenerator
	{
		/// <remarks>either <c>Range</c> or <c>Mnemonic</c> is always non-null</remarks>
		private readonly IReadOnlyList<(string Name, AxisSpec? Range, char? Mnemonic)> _cachedInputSpecs;
		private readonly ControllerDefinition _sourceDefinition;

		public Bk2InputDisplayGenerator(string systemId, ControllerDefinition sourceDefinition)
		{
			const string ERR_MSG = $"{nameof(ControllerDefinition.OrderedControlsFlat)}/{nameof(ControllerDefinition.ControlsOrdered)} contains an input name which is neither a button nor an axis: {{0}}";
			_cachedInputSpecs = sourceDefinition.OrderedControlsFlat.Select(button =>
			{
				if (sourceDefinition.Axes.TryGetValue(button, out var range)) return (button, range, null);
				if (sourceDefinition.BoolButtons.Contains(button)) return (button, (AxisSpec?) null, (char?) Bk2MnemonicLookup.Lookup(button, systemId));
				throw new InvalidOperationException(string.Format(ERR_MSG, button));
			}).ToArray();
			_sourceDefinition = sourceDefinition;
		}

		public string Generate(IController source)
		{
#if DEBUG
			if (!_sourceDefinition.OrderedControlsFlat.SequenceEqual(source.Definition.OrderedControlsFlat))
				throw new InvalidOperationException("Attempting to generate input display string for mismatched controller definition!");
#endif
			var sb = new StringBuilder();

			foreach (var (button, range, mnemonicChar) in _cachedInputSpecs)
			{
				if (range is not null)
				{
					var val = source.AxisValue(button);

					if (val == range.Value.Neutral)
					{
						sb.Append("      ");
					}
					else
					{
						sb.Append(val.ToString().PadLeft(5, ' ')).Append(',');
					}
				}
				else
				{
					sb.Append(source.IsPressed(button)
						? mnemonicChar.Value
						: ' ');
				}
			}

			return sb.ToString();
		}
	}
}
