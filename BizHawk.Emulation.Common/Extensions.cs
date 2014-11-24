using System;
using System.Linq;
using System.Reflection;

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

		// TODO: a better place for these
		public static bool IsImplemented(this MethodInfo info)
		{
			return !info.GetCustomAttributes(false).OfType<FeatureNotImplemented>().Any();
		}

		public static bool IsImplemented(this PropertyInfo info)
		{
			return !info.GetCustomAttributes(false).OfType<FeatureNotImplemented>().Any();
		}
	}
}
