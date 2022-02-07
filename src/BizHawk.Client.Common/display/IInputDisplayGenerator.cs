using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// An implementation of <see cref="IInputDisplayGenerator"/> that
	/// uses .bk2 mnemonics as the basis for display
	/// </summary>
	public class Bk2InputDisplayGenerator : IInputDisplayGenerator
	{
		private readonly IReadOnlyList<(string Name, AxisSpec? Range, char? Mnemonic)> _cachedInputSpecs;

		private readonly IController _source;

		public Bk2InputDisplayGenerator(string systemId, IController source)
		{
			const string ERR_MSG = nameof(ControllerDefinition.OrderedControlsFlat) + "/" + nameof(ControllerDefinition.ControlsOrdered) + " contains an input name which is neither a button nor an axis";
			_cachedInputSpecs = source.Definition.OrderedControlsFlat.Select(button =>
			{
				if (source.Definition.Axes.TryGetValue(button, out var range)) return (button, range, null);
				if (source.Definition.BoolButtons.Contains(button)) return (button, (AxisSpec?) null, (char?) Bk2MnemonicLookup.Lookup(button, systemId));
				throw new Exception(ERR_MSG);
			}).ToList();
			_source = source;
		}

		public string Generate()
		{
			var sb = new StringBuilder();

			foreach (var (button, range, mnemonicChar) in _cachedInputSpecs)
			{
				if (range is not null)
				{
					var val = _source.AxisValue(button);

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
					sb.Append(_source.IsPressed(button)
						? mnemonicChar
						: ' ');
				}
			}

			return sb.ToString();
		}
	}
}
