using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal class Bk2LogEntryGenerator : ILogEntryGenerator
	{
		private readonly string _systemId;
		private readonly IController _source;

		public Bk2LogEntryGenerator(string systemId, IController source)
		{
			_systemId = systemId;
			_source = source;
		}

		public bool IsEmpty => EmptyEntry == GenerateLogEntry();

		public string EmptyEntry => CreateLogEntry(createEmpty: true);

		public string GenerateLogEntry() => CreateLogEntry();

		public string GenerateLogKey()
		{
			var sb = new StringBuilder();
			sb.Append("LogKey:");

			foreach (var group in _source.Definition.ControlsOrdered.Where(c => c.Any()))
			{
				sb.Append('#');
				foreach (var button in group)
				{
					sb.Append(button).Append('|');
				}
			}

			return sb.ToString();
		}

		public IDictionary<string, string> Map()
		{
			var dict = new Dictionary<string, string>();
			foreach (var group in _source.Definition.ControlsOrdered.Where(c => c.Any()))
			{
				foreach (var button in group)
				{
					if (_source.Definition.BoolButtons.Contains(button))
					{
						dict.Add(button, Bk2MnemonicLookup.Lookup(button, _systemId).ToString());
					}
					else if (_source.Definition.Axes.ContainsKey(button))
					{
						dict.Add(button, Bk2MnemonicLookup.LookupAxis(button, _systemId));
					}
				}
			}

			return dict;
		}

		private string CreateLogEntry(bool createEmpty = false)
		{
			var sb = new StringBuilder();

			sb.Append('|');

			foreach (var group in _source.Definition.ControlsOrdered)
			{
				if (group.Any())
				{
					foreach (var button in group)
					{
						if (_source.Definition.Axes.TryGetValue(button, out var range))
						{
							var val = createEmpty ? range.Neutral : _source.AxisValue(button);

							sb.Append(val.ToString().PadLeft(5, ' ')).Append(',');
						}
						else if (_source.Definition.BoolButtons.Contains(button))
						{
							if (createEmpty)
							{
								sb.Append('.');
							}
							else
							{
								sb.Append(_source.IsPressed(button)
									? Bk2MnemonicLookup.Lookup(button, _systemId)
									: '.');
							}
						}
					}

					sb.Append('|');
				}
			}

			return sb.ToString();
		}
	}
}
