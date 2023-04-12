using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.IO.Pipes;

using BizHawk.Client.Common;

using DInputKey = Vortice.DirectInput.Key;

// this is not a very safe or pretty protocol, I'm not proud of it
namespace BizHawk.Bizware.DirectX
{
	internal static class IPCKeyInput
	{
		public static void Initialize()
		{
			var t = new Thread(IPCThread) { IsBackground = true };
			t.Start();
		}

		private static readonly List<KeyEvent> PendingEventList = new();
		private static readonly List<KeyEvent> EventList = new();

		private static void IPCThread()
		{
			var pipeName = $"bizhawk-pid-{System.Diagnostics.Process.GetCurrentProcess().Id}-IPCKeyInput";

			while (true)
			{
				using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 1024, 1024);
				try
				{
					pipe.WaitForConnection();

					var br = new BinaryReader(pipe);

					while (true)
					{
						var e = br.ReadInt32();
						var pressed = (e & 0x80000000) != 0;
						lock (PendingEventList)
						{
							PendingEventList.Add(new(KeyInput.KeyEnumMap[(DInputKey)(e & 0x7FFFFFFF)], pressed));
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
