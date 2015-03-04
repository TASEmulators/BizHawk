using System.Text;

namespace BizHawk.Emulation.Common
{
	public class TraceBuffer : ITraceable
	{
		private readonly StringBuilder buffer;

		public TraceBuffer()
		{
			buffer = new StringBuilder();
			Header = "Instructions";
		}

		public string TakeContents()
		{
			string s = buffer.ToString();
			buffer.Clear();
			return s;
		}

		public string Contents
		{
			get { return buffer.ToString(); }
		}

		public void Put(string content)
		{
			if (Enabled)
			{
				buffer.AppendLine(content);
			}
		}

		public bool Enabled { get; set; }

		public string Header { get; set; }
	}
}
