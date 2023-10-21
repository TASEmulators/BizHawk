using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;

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

		private static readonly List<KeyEvent> PendingKeyEvents = new();
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
						lock (PendingKeyEvents)
						{
							PendingKeyEvents.Add(new((DistinctKey)(e & 0x7FFFFFFF), pressed));
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
			var keyEvents = new List<KeyEvent>();

			lock (PendingKeyEvents)
			{
				keyEvents.AddRange(PendingKeyEvents);
				PendingKeyEvents.Clear();
			}

			return keyEvents;
		}
	}
}
