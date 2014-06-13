using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie : IMovie
	{
		private readonly BkmLog _log = new BkmLog();

		public string GetInputLog()
		{
			var sb = new StringBuilder();

			sb.AppendLine("[Input]");
			sb.Append(RawInputLog());
			sb.AppendLine("[/Input]");

			return sb.ToString();
		}

		public bool ExtractInputLog(TextReader reader, out string errorMessage)
		{
			throw new NotImplementedException();
		}

		public bool CheckTimeLines(TextReader reader, out string errorMessage)
		{
			throw new NotImplementedException();
		}

		private StringBuilder RawInputLog()
		{
			var sb = new StringBuilder();
			foreach (var record in _log)
			{
				sb.AppendLine(record);
			}

			return sb;
		}
	}
}
