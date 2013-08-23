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

		public void Get(Stream s)
		{
			if (thr != null)
				throw new Exception("Can only serve one thing at a time!");
			if (e != null)
				throw new Exception("Previous attempt failed!", e);
			if (!s.CanWrite)
				throw new ArgumentException("Stream must be readable!");

			using (var evt = new ManualResetEventSlim())
			{
				thr = new Thread(delegate()
					{
						try
						{
							using (var srv = new NamedPipeServerStream(PipeName, PipeDirection.In))
							{
								evt.Set();
								srv.WaitForConnection();
								srv.CopyTo(s);
								//srv.Flush();
							}
						}
						catch (Exception ee)
						{
							e = ee;
						}
					});
				thr.Start();
				evt.Wait();
			}
		}

		public void Offer(Stream s)
		{
			if (thr != null)
				throw new Exception("Can only serve one thing at a time!");
			if (e != null)
				throw new Exception("Previous attempt failed!", e);
			if (!s.CanRead)
				throw new ArgumentException("Stream must be readable!");

			using (var evt = new ManualResetEventSlim())
			{
				thr = new Thread(delegate()
					{
						try
						{
							using (var srv = new NamedPipeServerStream(PipeName, PipeDirection.Out))
							{
								evt.Set();
								srv.WaitForConnection();
								s.CopyTo(srv);
								srv.WaitForPipeDrain();
							}
						}
						catch (Exception ee)
						{
							e = ee;
						}
					});
				thr.Start();
				evt.Wait();
			}
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
