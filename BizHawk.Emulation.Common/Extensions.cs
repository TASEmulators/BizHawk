using System;

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
	}
}
