using System;
using System.Linq;

using BizHawk.Common.ReflectionExtensions;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// injects services into other classes
	/// </summary>
	public static class ServiceInjector
	{
		/// <summary>
		/// clears all services from a target
		/// </summary>
		public static void ClearServices(object target)
		{
			Type targetType = target.GetType();
			object[] tmp = new object[1];

			foreach (var propInfo in
				targetType.GetPropertiesWithAttrib(typeof(RequiredServiceAttribute))
				.Concat(targetType.GetPropertiesWithAttrib(typeof(OptionalServiceAttribute))))
			{
				propInfo.GetSetMethod(true).Invoke(target, tmp);
			}
		}

		/// <summary>
		/// Feeds the target its required services.
		/// </summary>
		/// <returns>false if update failed</returns>
		public static bool UpdateServices(IEmulatorServiceProvider source, object target)
		{
			Type targetType = target.GetType();
			object[] tmp = new object[1];

			foreach (var propInfo in targetType.GetPropertiesWithAttrib(typeof(RequiredServiceAttribute)))
			{
				tmp[0] = source.GetService(propInfo.PropertyType);
				if (tmp[0] == null)
				{
					return false;
				}

				propInfo.GetSetMethod(true).Invoke(target, tmp);
			}

			foreach (var propInfo in targetType.GetPropertiesWithAttrib(typeof(OptionalServiceAttribute)))
			{
				tmp[0] = source.GetService(propInfo.PropertyType);
				propInfo.GetSetMethod(true).Invoke(target, tmp);
			}

			return true;
		}

		/// <summary>
		/// Determines whether a target is available, considering its dependencies
		/// and the services provided by the emulator core.
		/// </summary>
		public static bool IsAvailable(IEmulatorServiceProvider source, Type targetType)
		{
			return targetType.GetPropertiesWithAttrib(typeof(RequiredServiceAttribute))
				.Select(pi => pi.PropertyType)
				.All(source.HasService);
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class RequiredServiceAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class OptionalServiceAttribute : Attribute
	{
	}
}
