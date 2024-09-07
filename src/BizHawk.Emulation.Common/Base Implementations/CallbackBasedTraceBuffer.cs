using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// An implementation of <see cref="ITraceable"/> that is implementation using only methods
	/// from <see cref="IDebuggable"/>, <see cref="IMemoryDomains"/>, and <see cref="IDisassemblable"/>
	/// Useful for ported cores that have these hooks but no trace logging hook,
	/// This allows for a traceable implementation without the need for additional API
	/// Note that this technique will always be significantly slower than a direct implementation
	/// </summary>
	public abstract class CallbackBasedTraceBuffer : ITraceable
	{
		private const string DEFAULT_HEADER = "Instructions";

		/// <exception cref="InvalidOperationException"><paramref name="debuggableCore"/> does not provide memory callback support or does not implement <see cref="IDebuggable.GetCpuFlagsAndRegisters"/></exception>
		protected CallbackBasedTraceBuffer(IDebuggable debuggableCore, IMemoryDomains memoryDomains, IDisassemblable disassembler, string header = DEFAULT_HEADER)
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
				throw new InvalidOperationException($"{nameof(IDebuggable.GetCpuFlagsAndRegisters)} is required");
			}

			Header = header;
			DebuggableCore = debuggableCore;
			MemoryDomains = memoryDomains;
			Disassembler = disassembler;
		}

		protected readonly IMemoryDomains MemoryDomains;
		protected readonly IDisassemblable Disassembler;
		protected readonly IDebuggable DebuggableCore;

		protected readonly List<TraceInfo> Buffer = new List<TraceInfo>();

		protected abstract void TraceFromCallback(uint addr, uint value, uint flags);

		private ITraceSink? _sink;

		public ITraceSink? Sink
		{
			get => _sink;
			set
			{
				_sink = value;
				DebuggableCore.MemoryCallbacks.Remove(TraceFromCallback);

				if (_sink != null)
				{
					var scope = DebuggableCore.MemoryCallbacks.AvailableScopes[0]; // This will be an issue when cores use this trace buffer and utilize multiple scopes
					DebuggableCore.MemoryCallbacks.Add(new TracingMemoryCallback(TraceFromCallback, scope));
				}
			}
		}

		public string Header { get; }

#nullable disable
		private class TracingMemoryCallback : IMemoryCallback
		{
			public TracingMemoryCallback(MemoryCallbackDelegate callback, string scope)
			{
				Callback = callback;
				Scope = scope;
			}

			public MemoryCallbackType Type => MemoryCallbackType.Execute;

			public string Name => "Trace Logging";

			public MemoryCallbackDelegate Callback { get; }

			public uint? Address => null;

			public uint? AddressMask => null;

			public string Scope { get; }
		}
#nullable restore
	}
}
