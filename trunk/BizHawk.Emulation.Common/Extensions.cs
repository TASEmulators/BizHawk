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

		// todo: most of the special cases involving the NullEmulator should probably go away
		public static bool IsNull(this IEmulator core)
		{
			return core == null || core is NullEmulator;
		}

		public static bool HasMemoryDomains(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<IMemoryDomains>();
		}

		public static IMemoryDomains AsMemoryDomains(this IEmulator core)
		{
			return (IMemoryDomains)core.ServiceProvider.GetService<IMemoryDomains>();
		}

		public static bool HasSaveRam(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<ISaveRam>();
		}

		public static ISaveRam AsSaveRam(this IEmulator core)
		{
			return (ISaveRam)core.ServiceProvider.GetService<ISaveRam>();
		}

		public static bool HasSavestates(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<IStatable>();
		}

		public static IStatable AsStatable(this IEmulator core)
		{
			return (IStatable)core.ServiceProvider.GetService<IStatable>();
		}

		public static bool CanPollInput(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<IInputPollable>();
		}

		public static IInputPollable AsInputPollable(this IEmulator core)
		{
			return (IInputPollable)core.ServiceProvider.GetService<IInputPollable>();
		}

		public static bool HasDriveLight(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<IDriveLight>();
		}

		public static IDriveLight AsDriveLight(this IEmulator core)
		{
			return (IDriveLight)core.ServiceProvider.GetService<IDriveLight>();
		}

		public static bool CanDebug(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<IDebuggable>();
		}

		public static IDebuggable AsDebuggable(this IEmulator core)
		{
			return (IDebuggable)core.ServiceProvider.GetService<IDebuggable>();
		}

		public static bool CpuTraceAvailable(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<ITraceable>();
		}

		public static ITraceable AsTracer(this IEmulator core)
		{
			return (ITraceable)core.ServiceProvider.GetService<ITraceable>();
		}

		public static bool MemoryCallbacksAvailable(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			// TODO: this is a pretty ugly way to handle this
			var debuggable = (IDebuggable)core.ServiceProvider.GetService<IDebuggable>();
			if (debuggable != null)
			{
				try
				{
					var callbacks = debuggable.MemoryCallbacks;
					return true;
				}
				catch (NotImplementedException)
				{
					return false;
				}
			}

			return false;
		}

		public static bool MemoryCallbacksAvailable(this IDebuggable core)
		{
			if (core == null)
			{
				return false;
			}

			try
			{
				var callbacks = core.MemoryCallbacks;
				return true;
			}
			catch (NotImplementedException)
			{
				return false;
			}
		}

		public static bool CanDisassemble(this IEmulator core)
		{
			if (core == null)
			{
				return false;
			}

			return core.ServiceProvider.HasService<IDisassemblable>();
		}

		public static IDisassemblable AsDissassembler(this IEmulator core)
		{
			return (IDisassemblable)core.ServiceProvider.GetService<IDisassemblable>();
		}

		public static bool CanPoke(this MemoryDomain d)
		{
			if (d.PokeByte == null)
			{
				return false;
			}
			
			try
			{
				d.PokeByte(0, d.PeekByte(0));
			}
			catch (NotImplementedException)
			{
				return false;
			}

			return true;
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
