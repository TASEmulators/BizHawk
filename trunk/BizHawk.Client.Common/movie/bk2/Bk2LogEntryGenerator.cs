using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Bk2LogEntryGenerator : ILogEntryGenerator
	{
		private IController _source;

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
				var sb = new StringBuilder('|');
				foreach (var button in _source.Type.BoolButtons)
				{
					sb.Append('.');
				}

				if (_source.Type.FloatControls.Any())
				{
					foreach (var floatBtn in _source.Type.FloatControls)
					{
						sb.Append(" 000,");
					}

					sb.Remove(sb.Length - 1, 1);
				}

				sb.Append('|');
				return sb.ToString();
			}
		}

		public string GenerateLogEntry()
		{
			var sb = new StringBuilder('|');
			foreach (var button in _source.Type.BoolButtons)
			{
				sb.Append(_source.IsPressed(button) ? '1' : '.');
			}

			if (_source.Type.FloatControls.Any())
			{
				foreach (var floatBtn in _source.Type.FloatControls)
				{
					var val = (int)_source.GetFloat(floatBtn);
					sb.Append(' ').Append(val).Append(',');
				}

				sb.Remove(sb.Length - 1, 1);
			}

			sb.Append('|');
			return sb.ToString();
		}
	}
}
