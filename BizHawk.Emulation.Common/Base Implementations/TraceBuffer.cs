using System.Collections.Generic;
using System.Linq;

//garbage

namespace BizHawk.Emulation.Common
{
	public class TraceBuffer : ITraceable
	{
		public TraceBuffer()
		{
			Header = "Instructions";
		}

		public string Header { get; set; }

		public ITraceSink Sink { get; set; }

		public bool Enabled { get { return Sink != null; } }

		public void Put(TraceInfo info) { Sink.Put(info); }
	}
}
