using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Common
{
	public class TraceBuffer : ITraceable
	{
		private readonly List<TraceInfo> Buffer = new List<TraceInfo>();

		public TraceBuffer()
		{
			Header = "Instructions";
		}

		public IEnumerable<TraceInfo> TakeContents()
		{
			var contents = Buffer.ToList();
			Buffer.Clear();
			return contents;
		}

		public IEnumerable<TraceInfo> Contents
		{
			get { return Buffer; }
		}

		public void Put(TraceInfo content)
		{
			if (Enabled)
			{
				Buffer.Add(content);
			}
		}

		public bool Enabled { get; set; }

		public string Header { get; set; }
	}
}
