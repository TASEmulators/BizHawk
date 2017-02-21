using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// An implementation of ITraceable that is implementation using only methods
	/// from IDebuggable, IMemoryDomains, and IDisassemblable
	/// Useful for ported cores that have these hooks but no trace logging hook,
	/// This allows for a traceable implementation without the need for additional API
	/// Note that this technique will always be significantly slower than a direct implementation
	/// </summary>
	/// <seealso cref="ITraceable"/> 
	/// <seealso cref="IDebuggable"/> 
	/// <seealso cref="IMemoryDomains"/> 
	/// /// <seealso cref="IDisassemblable"/> 
	public abstract class CallbackBasedTraceBuffer : ITraceable
	{
		public CallbackBasedTraceBuffer(IDebuggable debuggableCore, IMemoryDomains memoryDomains, IDisassemblable disassembler)
		{
			if (!debuggableCore.MemoryCallbacksAvailable())
			{
				throw new InvalidOperationException("Memory callbacks are required");
			}

			try
			{
				debuggableCore.GetCpuFlagsAndRegisters();
			}
			catch (NotImplementedException)
			{
				throw new InvalidOperationException("GetCpuFlagsAndRegisters is required");
			}

			Header = "Instructions";
			DebuggableCore = debuggableCore;
			MemoryDomains = memoryDomains;
			Disassembler = disassembler;
		}

		protected readonly IMemoryDomains MemoryDomains;
		protected readonly IDisassemblable Disassembler;
		protected readonly IDebuggable DebuggableCore;

		protected readonly List<TraceInfo> Buffer = new List<TraceInfo>();

		public abstract void TraceFromCallback();

		private ITraceSink _sink;

		public bool Enabled
		{
			get { return Sink != null; }
		}

		public void Put(TraceInfo info)
		{
			Sink.Put(info);
		}

		public ITraceSink Sink
		{
			get
			{
				return _sink;
			}
			set
			{
				_sink = value;
				DebuggableCore.MemoryCallbacks.Remove(TraceFromCallback);

				if (_sink != null)
				{
					DebuggableCore.MemoryCallbacks.Add(new TracingMemoryCallback(TraceFromCallback));
				}
			}
		}

		public string Header { get; set; }

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

			public uint? AddressMask
			{
				get { return null; }
			}
		}
	}
}
