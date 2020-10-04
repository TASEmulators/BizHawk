using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.IO.Pipes;

using BizHawk.Client.Common;

using SlimDX.DirectInput;

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


		private static readonly List<KeyEvent> PendingEventList = new List<KeyEvent>();
		private static readonly List<KeyEvent> EventList = new List<KeyEvent>();

		private static void IPCThread()
		{
			string pipeName = $"bizhawk-pid-{System.Diagnostics.Process.GetCurrentProcess().Id}-IPCKeyInput";


			for (; ; )
			{
				using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 1024, 1024);
				try
				{
					pipe.WaitForConnection();

					BinaryReader br = new BinaryReader(pipe);

					for (; ; )
					{
						int e = br.ReadInt32();
						bool pressed = (e & 0x80000000) != 0;
						lock (PendingEventList)
							PendingEventList.Add(new KeyEvent { Key = KeyInput.KeyEnumMap[(Key)(e & 0x7FFFFFFF)], Pressed = pressed });
					}
				}
				catch { }
			}
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
