using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IInputDisplayGenerator
	{
		/// <summary>
		/// Generates a display friendly version of the input log entry
		/// </summary>
		string Generate();
	}

	/// <summary>
	/// An implementation of <see cref="IInputDisplayGenerator"/> that
	/// uses .bk2 mnemonics as the basis for display
	/// </summary>
	public class Bk2InputDisplayGenerator
	{
		private readonly string _systemId;
		private readonly IController _source;

		public Bk2InputDisplayGenerator(string systemId, IController source)
		{
			_systemId = systemId;
			_source = source;
		}

		public string Generate()
		{
			var sb = new StringBuilder();

			foreach (var group in _source.Definition.ControlsOrdered)
			{
				foreach (var button in group)
				{
					if (_source.Definition.Axes.TryGetValue(button, out var range))
					{
						var val = _source.AxisValue(button);

						if (val == range.Neutral)
						{
							sb.Append("      ");
						}
						else
						{
							sb.Append(val.ToString().PadLeft(5, ' ')).Append(',');
						}
					}
					else if (_source.Definition.BoolButtons.Contains(button))
					{
						sb.Append(_source.IsPressed(button)
							? Bk2MnemonicLookup.Lookup(button, _systemId)
							: ' ');
					}
				}
			}

			return sb.ToString();
		}
	}
}
