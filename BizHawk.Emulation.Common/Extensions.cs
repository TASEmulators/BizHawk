using System;
using System.Linq;
using System.Reflection;
using BizHawk.Common.ReflectionExtensions;

namespace BizHawk.Emulation.Common.IEmulatorExtensions
{
	public static class Extensions
	{
		public static CoreAttributes Attributes(this IEmulator core)
		{
			return (CoreAttributes)Attribute.GetCustomAttribute(core.GetType(), typeof(CoreAttributes));
		}

		public static bool IsNull(this IEmulator core)
		{
			return core == null || core is NullEmulator;
		}

		public static bool HasMemoryDomains(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<IMemoryDomains>();
		}

		public static bool HasSaveRam(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<ISaveRam>();
		}

		public static bool HasSavestates(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<IStatable>();
		}

		public static bool CanPollInput(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<IInputPollable>();
		}

		public static bool CpuTraceAvailable(this IEmulator core)
		{
			// TODO: this is a pretty ugly way to handle this
			var debuggable = (IDebuggable)core.ServiceProvider.GetService<IDebuggable>();
			if (debuggable != null)
			{
				try
				{
					var tracer = debuggable.Tracer;
					return true;
				}
				catch(NotImplementedException)
				{
					return false;
				}
			}

			return false;
		}

		public static bool CanDebug(this IEmulator core)
		{
			return core.ServiceProvider.HasService<IDebuggable>();
		}

		public static IDebuggable GetDebugger(this IEmulator core)
		{
			return (IDebuggable)core.ServiceProvider.GetService<IDebuggable>();
		}

		// TODO: a better place for these
		public static bool IsImplemented(this MethodInfo info)
		{
			// If a method is marked as Not implemented, it is not implemented no matter what the body is
			if (info.GetCustomAttributes(false).Any(a => a is FeatureNotImplemented))
			{
				return false;
			}

			// If a method is not marked but all it does is throw an exception, consider it not implemented
			return !info.ThrowsError();
		}

		public static bool IsImplemented(this PropertyInfo info)
		{
			return !info.GetCustomAttributes(false).Any(a => a is FeatureNotImplemented);
		}
	}
}
