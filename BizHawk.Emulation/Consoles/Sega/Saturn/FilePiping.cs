using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace BizHawk.Emulation.Consoles.Sega.Saturn
{
	/// <summary>
	/// helpers for moving files across named pipes
	/// </summary>
	public class FilePiping
	{
		public void Offer(byte[] data)
		{
			MemoryStream ms = new MemoryStream(data, false);
			Offer(ms);
		}

		string PipeName;
		Thread thr;
		Exception e;

		public string GetPipeName()
		{
			return PipeName;
		}

		public string GetPipeNameNative()
		{
			return @"\\.\pipe\" + PipeName;
		}

		public FilePiping()
		{
			PipeName = "BizHawk-" + Guid.NewGuid().ToString();
		}

		public void Offer(Stream s)
		{
			if (thr != null)
				throw new Exception("Can only serve one thing at a time!");
			if (e != null)
				throw new Exception("Previous attempt failed!", e);
			if (!s.CanRead)
				throw new ArgumentException("Stream must be readable!");

			thr = new Thread(delegate()
				{
					try
					{
						using (var srv = new NamedPipeServerStream(PipeName, PipeDirection.Out))
						{
							srv.WaitForConnection();
							s.CopyTo(srv);
							srv.WaitForPipeDrain();
						}
					}
					catch (Exception ee)// might want to do something about this...
					{
						e = ee;
					}
				});
			thr.Start();
		}

		public Exception GetResults()
		{
			if (thr == null)
				throw new Exception("No pending!");
			thr.Join();
			thr = null;
			Exception ret = e;
			e = null;
			return ret;
		}
	}
}
