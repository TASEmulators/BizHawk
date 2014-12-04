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

		public static bool HasMemoryDomains(this IEmulator core)
		{
			return core is IMemoryDomains;
		}

		public static bool HasSaveRam(this IEmulator core)
		{
			return core is ISaveRam;
		}

		public static bool HasSavestates(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<IStatable>();
		}

		public static bool CanPollInput(this IEmulator core)
		{
			return core is IInputPollable;
		}

		public static bool IsNull(this IEmulator core)
		{
			return core == null || core is NullEmulator;
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
