using BizHawk.Emulation.Common;
using System;
using System.Text;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX
	{
		private readonly ITraceable Tracer;

		// TODO: move this to BaseImplementations and make the TraceFromCallback settable by the core
		public class CallbackBasedTraceBuffer : ITraceable
		{
			public CallbackBasedTraceBuffer(IDebuggable debuggableCore)
			{
				if (!debuggableCore.MemoryCallbacksAvailable())
				{
					throw new InvalidOperationException("Memory callbacks are required");
				}

				try
				{
					var dummy = debuggableCore.GetCpuFlagsAndRegisters();
				}
				catch(NotImplementedException)
				{
					throw new InvalidOperationException("GetCpuFlagsAndRegisters is required");
				}

				Buffer = new StringBuilder();
				Header = "Instructions";
				DebuggableCore = debuggableCore;

			}

			private readonly IDebuggable DebuggableCore;

			private readonly StringBuilder Buffer;

			private bool _enabled;

			private void TraceFromCallback()
			{
				var regs = DebuggableCore.GetCpuFlagsAndRegisters();
				foreach(var r in regs)
				{
					Buffer.Append(string.Format("{0} {1}", r.Key, r.Value.Value));
				}

				Buffer.AppendLine();
			}

			public bool Enabled
			{
				get
				{
					return _enabled;
				}

				set
				{
					_enabled = value;
					DebuggableCore.MemoryCallbacks.Remove(TraceFromCallback);

					if (_enabled)
					{
						DebuggableCore.MemoryCallbacks.Add(new TracingMemoryCallback(TraceFromCallback));
					}
				}
			}

			public string Header { get; set; }

			public string Contents
			{
				get { return Buffer.ToString(); }
			}

			public string TakeContents()
			{
				string s = Buffer.ToString();
				Buffer.Clear();
				return s;
			}

			public void Put(string content)
			{
				if (_enabled)
				{
					Buffer.AppendLine(content);
				}
			}

			public class TracingMemoryCallback : IMemoryCallback
			{
				public TracingMemoryCallback(Action callback)
				{
					Callback = callback;
				}

				public MemoryCallbackType Type
				{
					get { return MemoryCallbackType.Execute; }
				}

				public string Name
				{
					get { return "Trace Logging"; }
				}

				public Action Callback { get; private set; }

				public uint? Address
				{
					get { return null; }
				}
			}
		}
	}
}
