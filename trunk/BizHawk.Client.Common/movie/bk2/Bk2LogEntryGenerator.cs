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

				foreach (var floatBtn in _source.Type.FloatControls)
				{
					sb.Append("000 ");
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

			foreach (var floatBtn in _source.Type.FloatControls)
			{
				var val = (int)_source.GetFloat(floatBtn);
				sb.Append(val).Append(' ');
			}

			sb.Append('|');
			return sb.ToString();
		}
	}
}
