using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public class Bk2LogEntryGenerator : ILogEntryGenerator
	{
		private readonly Bk2MnemonicConstants Mnemonics = new Bk2MnemonicConstants();
		private readonly Bk2FloatConstants FloatLookup = new Bk2FloatConstants();

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
			return CreateLogEntry(forInputDisplay: true);
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

			foreach (var group in _source.Definition.ControlsOrdered.Where(c => c.Any()))
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
			foreach (var group in _source.Definition.ControlsOrdered.Where(c => c.Any()))
			{
				foreach (var button in group)
				{
					if (_source.Definition.BoolButtons.Contains(button))
					{
						dict.Add(button, Mnemonics[button].ToString());
					}
					else if (_source.Definition.FloatControls.Contains(button))
					{
						dict.Add(button, FloatLookup[button]);
					}
				}
			}

			return dict;
		}

		private string CreateLogEntry(bool createEmpty = false, bool forInputDisplay = false)
		{
			var sb = new StringBuilder();

			if (!forInputDisplay)
				sb.Append('|');

			foreach (var group in _source.Definition.ControlsOrdered)
			{
				if (group.Any())
				{
					foreach (var button in group)
					{
						if (_source.Definition.FloatControls.Contains(button))
						{
							int val;
							int i = _source.Definition.FloatControls.IndexOf(button);
							int mid = (int)_source.Definition.FloatRanges[i].Mid;

							if (createEmpty)
							{
								val = mid;
							}
							else
							{
								val = (int)_source.GetFloat(button);
							}

							if (forInputDisplay && val == mid)
								sb.Append("      ");
							else
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
								sb.Append(_source.IsPressed(button) ? Mnemonics[button] : forInputDisplay ? ' ' : '.');
							}
						}
					}

					if (!forInputDisplay)
						sb.Append('|');
				}
			}

			return sb.ToString();
		}
	}
}
