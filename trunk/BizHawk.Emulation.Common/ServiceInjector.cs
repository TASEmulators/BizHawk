using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// injects services into other classes
	/// </summary>
	public static class ServiceInjector
	{
		private static IEnumerable<PropertyInfo> GetPropertiesWithAttr(Type type, Type attributeType)
		{
			return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
				.Where(p => p.GetCustomAttributes(attributeType, false).Length > 0);
		}

		/// <summary>
		/// Feeds the target its required services.
		/// </summary>
		/// <returns>false if update failed</returns>
		public static bool UpdateServices(IEmulatorServiceProvider source, object target)
		{
			Type toolType = target.GetType();
			object[] tmp = new object[1];

			foreach (var propinfo in GetPropertiesWithAttr(toolType, typeof(RequiredService)))
			{
				tmp[0] = source.GetService(propinfo.PropertyType);
				if (tmp[0] == null)
					return false;
				propinfo.GetSetMethod(true).Invoke(target, tmp);
			}

			foreach (var propinfo in GetPropertiesWithAttr(toolType, typeof(OptionalService)))
			{
				tmp[0] = source.GetService(propinfo.PropertyType);
				propinfo.GetSetMethod(true).Invoke(target, tmp);
			}
			return true;
		}

		/// <summary>
		/// Determines whether a target is available, considering its dependencies
		/// and the services provided by the emulator core.
		/// </summary>
		public static bool IsAvailable(IEmulatorServiceProvider source, Type targetType)
		{
			return GetPropertiesWithAttr(targetType, typeof(RequiredService))
				.Select(pi => pi.PropertyType)
				.All(t => source.HasService(t));
		}
	}


	[AttributeUsage(AttributeTargets.Property)]
	public class RequiredService : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class OptionalService : Attribute
	{
	}
}
