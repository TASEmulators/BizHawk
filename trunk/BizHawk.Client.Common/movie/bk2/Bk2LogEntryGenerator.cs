using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public class Bk2LogEntryGenerator : ILogEntryGenerator
	{
		private readonly Bk2MnemonicConstants Mnemonics = new Bk2MnemonicConstants();
		private IController _source;
		private readonly string _logKey = string.Empty;

		public Bk2LogEntryGenerator(string logKey)
		{
			_logKey = logKey;
		}

		public IMovieController MovieControllerAdapter
		{
			get
			{
				return new Bk2ControllerAdapter(_logKey);
			}
		}

		#region ILogEntryGenerator Implementation

		public void SetSource(IController source)
		{
			_source = source;
		}

		public string GenerateInputDisplay()
		{
			var le = GenerateLogEntry();
			if (le == EmptyEntry)
			{
				return string.Empty;
			}
			
			return GenerateLogEntry()
				.Replace(".", " ")
				.Replace("|", "")
				.Replace("    0,    0,", "            ");
		}

		public bool IsEmpty
		{
			get
			{
				return EmptyEntry == GenerateLogEntry();
			}
		}

		public string EmptyEntry
		{
			get
			{
				return CreateLogEntry(createEmpty: true);
			}
		}

		public string GenerateLogEntry()
		{
			return CreateLogEntry();
		}

		#endregion

		public string GenerateLogKey()
		{
			var sb = new StringBuilder();
			sb.Append("LogKey:");

			foreach (var group in _source.Type.ControlsOrdered.Where(c => c.Any()))
			{
				sb.Append("#");
				foreach (var button in group)
				{
					sb
						.Append(button)
						.Append('|');
				}
			}

			return sb.ToString();
		}

		public Dictionary<string, string> Map()
		{
			var dict = new Dictionary<string, string>();
			foreach (var group in _source.Type.ControlsOrdered.Where(c => c.Any()))
			{
				foreach (var button in group)
				{
					dict.Add(button, Mnemonics[button].ToString()); // TODO: floats should be a float lookup that returns a string, floats by convention should always be more than one character to distinguish from boolean input
				}
			}

			return dict;
		}

		private string CreateLogEntry(bool createEmpty = false)
		{
			var sb = new StringBuilder();
			sb.Append('|');

			foreach (var group in _source.Type.ControlsOrdered)
			{
				if (group.Any())
				{
					foreach (var button in group)
					{
						if (_source.Type.FloatControls.Contains(button))
						{
							if (createEmpty)
							{
								sb.Append("    0,");
							}
							else
							{
								var val = (int)_source.GetFloat(button);
								sb.Append(val.ToString().PadLeft(5, ' ')).Append(',');
							}
						}
						else if (_source.Type.BoolButtons.Contains(button))
						{
							if (createEmpty)
							{
								sb.Append('.');
							}
							else
							{
								sb.Append(_source.IsPressed(button) ? Mnemonics[button] : '.');
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
