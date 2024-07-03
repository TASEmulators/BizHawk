using System.Linq;

using BizHawk.Common.ReflectionExtensions;

namespace BizHawk.Client.Common
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
			foreach (var mi in targetType.GetProperties(ReflectionExtensions.DI_TARGET_PROPS)
				.Where(static pi => pi.PropertyType == typeof(ApiContainer))
				.Select(static pi => pi.SetMethod))
			{
				mi?.Invoke(target, new object[] { null });
			}
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
		public static bool UpdateApis(Func<IExternalApiProvider> getProvider, object target)
		{
			Type targetType = target.GetType();
			object[] tmp = new object[1];

			foreach (var mi in targetType.GetProperties(ReflectionExtensions.DI_TARGET_PROPS)
				.Where(static pi => pi.PropertyType == typeof(ApiContainer))
				.Select(static pi => pi.SetMethod))
			{
				if (mi is null) continue;
				tmp[0] ??= getProvider().Container;
				mi.Invoke(target, tmp);
			}

			foreach (var propinfo in targetType.GetPropertiesWithAttrib(typeof(RequiredApiAttribute)))
			{
				tmp[0] = getProvider().GetApi(propinfo.PropertyType);
				if (tmp[0] == null)
				{
					return false;
				}

				propinfo.GetSetMethod(true).Invoke(target, tmp);
			}

			foreach (var propinfo in targetType.GetPropertiesWithAttrib(typeof(OptionalApiAttribute)))
			{
				tmp[0] = getProvider().GetApi(propinfo.PropertyType);
				propinfo.GetSetMethod(true).Invoke(target, tmp);
			}

			return true;
		}

		/// <summary>
		/// Determines whether a target is available, considering its dependencies
		/// and the Apis provided by the emulator core.
		/// </summary>
		public static bool IsAvailable(Func<IExternalApiProvider> getProvider, Type targetType)
		{
			return targetType.GetPropertiesWithAttrib(typeof(RequiredApiAttribute))
				.All(pi => getProvider().HasApi(pi.PropertyType));
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
