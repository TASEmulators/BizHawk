using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Bk2LogEntryGenerator : ILogEntryGenerator
	{
		private readonly Bk2MnemonicConstants _mnemonics = new Bk2MnemonicConstants();
		private readonly Bk2FloatConstants _floatLookup = new Bk2FloatConstants();

		private readonly string _logKey;
		private IController _source;

		public Bk2LogEntryGenerator(string logKey)
		{
			_logKey = logKey;
		}

		public IMovieController MovieControllerAdapter => new Bk2ControllerAdapter(_logKey);

		#region ILogEntryGenerator Implementation

		public void SetSource(IController source)
		{
			_source = source;
		}

		public string GenerateInputDisplay()
		{
			return CreateLogEntry(forInputDisplay: true);
		}

		public bool IsEmpty => EmptyEntry == GenerateLogEntry();

		public string EmptyEntry => CreateLogEntry(createEmpty: true);

		public string GenerateLogEntry()
		{
			return CreateLogEntry();
		}

		#endregion

		public string GenerateLogKey()
		{
			var groupStrings = _source.Definition.ControlsOrdered.Select(group =>
				string.Concat(group.Select(button => $"{button}|")));
			var s = $"#{string.Join("#", groupStrings.Where(groupStr => !string.IsNullOrEmpty(groupStr)))}";
			return s.Length > 1 ? $"LogKey:{s}" : "LogKey:";
		}

		public Dictionary<string, string> Map()
		{
			var dict = new Dictionary<string, string>();
			foreach (var group in _source.Definition.ControlsOrdered)
			{
				foreach (var button in group)
				{
					if (_source.Definition.BoolButtons.Contains(button))
					{
						dict.Add(button, _mnemonics[button].ToString());
					}
					else if (_source.Definition.FloatControls.Contains(button))
					{
						dict.Add(button, _floatLookup[button]);
					}
				}
			}

			return dict;
		}

		private string CreateLogEntry(bool createEmpty = false, bool forInputDisplay = false)
		{
			var list = _source.Definition.ControlsOrdered.Select(group => string.Concat(group.Select(
				button => {
					if (_source.Definition.FloatControls.Contains(button))
					{
						var mid = (int)_source.Definition.FloatRanges[_source.Definition.FloatControls.IndexOf(button)].Mid;
						var val = createEmpty ? mid : (int)_source.GetFloat(button);
						return forInputDisplay && val == mid ? "      " : $"{val,5},";
					}
					else if (_source.Definition.BoolButtons.Contains(button))
						return (createEmpty
							? '.'
							: _source.IsPressed(button)
								? _mnemonics[button]
								: forInputDisplay
									? ' '
									: '.').ToString();
					else return string.Empty;
				})));
			return forInputDisplay ? string.Concat(list) : $"|{string.Join("|", list)}|";
		}
	}
}
