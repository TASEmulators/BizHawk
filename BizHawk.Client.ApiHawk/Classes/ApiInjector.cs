using System;
using System.Linq;

using BizHawk.Common.ReflectionExtensions;

namespace BizHawk.Client.ApiHawk
{
	/// <summary>
	/// injects Apis into other classes
	/// </summary>
	public static class ApiInjector
	{
		/// <summary>
		/// clears all Apis from a target
		/// </summary>
		public static void ClearApis(object target)
		{
			Type targetType = target.GetType();
			object[] tmp = new object[1];

			foreach (var propinfo in
				targetType.GetPropertiesWithAttrib(typeof(RequiredApiAttribute))
				.Concat(targetType.GetPropertiesWithAttrib(typeof(OptionalApiAttribute))))
			{
				propinfo.GetSetMethod(true).Invoke(target, tmp);
			}
		}

		/// <summary>
		/// Feeds the target its required Apis.
		/// </summary>
		/// <returns>false if update failed</returns>
		public static bool UpdateApis(IExternalApiProvider source, object target)
		{
			Type targetType = target.GetType();
			object[] tmp = new object[1];

			foreach (var propinfo in targetType.GetPropertiesWithAttrib(typeof(RequiredApiAttribute)))
			{
				tmp[0] = source.GetApi(propinfo.PropertyType);
				if (tmp[0] == null)
				{
					return false;
				}

				propinfo.GetSetMethod(true).Invoke(target, tmp);
			}

			foreach (var propinfo in targetType.GetPropertiesWithAttrib(typeof(OptionalApiAttribute)))
			{
				tmp[0] = source.GetApi(propinfo.PropertyType);
				propinfo.GetSetMethod(true).Invoke(target, tmp);
			}

			return true;
		}

		/// <summary>
		/// Determines whether a target is available, considering its dependencies
		/// and the Apis provided by the emulator core.
		/// </summary>
		public static bool IsAvailable(IExternalApiProvider source, Type targetType)
		{
			return targetType.GetPropertiesWithAttrib(typeof(RequiredApiAttribute))
				.Select(pi => pi.PropertyType)
				.All(source.HasApi);
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public sealed class RequiredApiAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Property)]
	public sealed class OptionalApiAttribute : Attribute
	{
	}
}
