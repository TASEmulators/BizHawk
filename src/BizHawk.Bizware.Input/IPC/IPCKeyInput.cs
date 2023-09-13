using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.IO.Pipes;

using BizHawk.Client.Common;

// this is not a very safe or pretty protocol, I'm not proud of it
namespace BizHawk.Bizware.Input
{
	internal static class IPCKeyInput
	{
		public static void Initialize()
		{
			if (!IPCActive)
			{
				var t = new Thread(IPCThread) { IsBackground = true };
				t.Start();
				IPCActive = true;
			}
		}

		private static readonly List<KeyEvent> PendingEventList = new();
		private static readonly List<KeyEvent> EventList = new();
		private static bool IPCActive;

		private static void IPCThread()
		{
			var pipeName = $"bizhawk-pid-{Process.GetCurrentProcess().Id}-IPCKeyInput";

			while (true)
			{
				using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 1024, 1024);
				try
				{
					pipe.WaitForConnection();

					var br = new BinaryReader(pipe);

					while (true)
					{
						var e = br.ReadUInt32();
						var pressed = (e & 0x80000000) != 0;
						lock (PendingEventList)
						{
							PendingEventList.Add(new((DistinctKey)(e & 0x7FFFFFFF), pressed));
						}
					}
				}
				catch
				{
					// ignored
				}
			}

			// ReSharper disable once FunctionNeverReturns
		}

		public static IEnumerable<KeyEvent> Update()
		{
			EventList.Clear();

			lock (PendingEventList)
			{
				EventList.AddRange(PendingEventList);
				PendingEventList.Clear();
			}

			return EventList;
		}
	}
}
