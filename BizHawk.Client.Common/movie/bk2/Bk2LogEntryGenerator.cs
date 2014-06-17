using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Bk2LogEntryGenerator : ILogEntryGenerator
	{
		private readonly Bk2MnemonicConstants Mnemonics = new Bk2MnemonicConstants();
		private IController _source;

		public IMovieController MovieControllerAdapter
		{
			get { return new Bk2ControllerAdapter(); }
		}

		#region ILogEntryGenerator Implementation

		public void SetSource(IController source)
		{
			_source = source;
		}

		public string GenerateInputDisplay()
		{
			return GenerateLogEntry()
				.Replace(".", " ")
				.Replace("|", "")
				.Replace(" 000, 000", "         ");
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
			StringBuilder sb = new StringBuilder();
			sb.Append("LogKey:");

			foreach (var button in _source.Type.BoolButtons)
			{
				sb.Append(button).Append('|');
			}

			foreach (var button in _source.Type.FloatControls)
			{
				sb.Append(button).Append('|');
			}

			return sb.ToString();
		}

		private string CreateLogEntry(bool createEmpty = false)
		{
			var sb = new StringBuilder();
			sb.Append('|');

			foreach (var button in _source.Type.BoolButtons)
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

			sb.Append('|');
			if (_source.Type.FloatControls.Any())
			{
				foreach (var floatBtn in _source.Type.FloatControls)
				{
					if (createEmpty)
					{
						sb.Append("000,");
					}
					else
					{
						var val = (int)_source.GetFloat(floatBtn);
						sb.Append(val.ToString().PadLeft(3, '0')).Append(',');
					}
				}

				sb.Remove(sb.Length - 1, 1);
			}

			sb.Append('|');
			return sb.ToString();
		}
	}
}
