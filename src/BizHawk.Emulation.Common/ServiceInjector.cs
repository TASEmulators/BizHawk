using System.Collections.Generic;
using System.Reflection;

using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// injects services into other classes
	/// </summary>
	public static class ServiceInjector
	{
		private readonly struct ServicePropInfo
		{
			public readonly Type PropType;

			public readonly MethodInfo? Setter;

			public ServicePropInfo(Type propType, MethodInfo? setter)
			{
				PropType = propType;
				Setter = setter;
			}
		}

		private static readonly Dictionary<Type, (List<ServicePropInfo> Req, List<ServicePropInfo> Opt)> _cache = new();

		private static (List<ServicePropInfo> Req, List<ServicePropInfo> Opt) GetServicePropsFor(
			this Type @class,
			bool mayCache)
		{
			const string ERR_FMT_STR_GETONLY_PROP = "prop `[{0}] {1}.{2}` is get-only";
			if (_cache.TryGetValue(@class, out var pair)) return pair;
			pair = (new(), new());
			foreach (var pi in @class.GetProperties(ReflectionExtensions.DI_TARGET_PROPS))
			{
				foreach (var attr in pi.GetCustomAttributes())
				{
					if (attr is RequiredServiceAttribute)
					{
						var setter = pi.GetSetMethod(nonPublic: true);
						if (setter is null) Util.DebugWriteLine(ERR_FMT_STR_GETONLY_PROP, "RequiredService", @class.Name, pi.Name);
						pair.Req.Add(new(pi.PropertyType, setter)); // pass through anyway, and `UpdateServices` will see it and `return false;`
						break;
					}
					else if (attr is OptionalServiceAttribute)
					{
						if (pi.GetSetMethod(nonPublic: true) is MethodInfo setter) pair.Req.Add(new(pi.PropertyType, setter));
						else Util.DebugWriteLine(ERR_FMT_STR_GETONLY_PROP, "OptionalService", @class.Name, pi.Name);
						break;
					}
				}
			}
			if (mayCache) _cache[@class] = pair;
			return pair;
		}

		/// <summary>
		/// Feeds the target its required services.
		/// </summary>
		/// <param name="mayCache">
		/// <see langword="true"/> if the properties of <paramref name="target"/> may be written to cache,
		/// i.e. if it's known to come from a first-party assembly and not an ext. tool.<br/>
		/// Cache will still be read from regardless.
		/// </param>
		/// <returns>false if update failed</returns>
		/// <remarks>don't think having a genericised overload would be helpful, but TODO pass in type to save <c>target.GetType()</c> call</remarks>
		public static bool UpdateServices(IEmulatorServiceProvider source, object target, bool mayCache = false)
		{
			Type targetType = target.GetType();
			object?[] tmp = new object?[1];
			var (req, opt) = GetServicePropsFor(targetType, mayCache: mayCache);
			foreach (var info in req)
			{
				tmp[0] = source.GetService(info.PropType);
				if (tmp[0] == null)
				{
					return false;
				}
				if (info.Setter is null) return false;
				info.Setter.Invoke(target, tmp);
			}
			foreach (var info in opt)
			{
				tmp[0] = source.GetService(info.PropType);
				info.Setter!.Invoke(target, tmp);
			}

			return true;
		}

		/// <summary>
		/// Determines whether a target is available, considering its dependencies
		/// and the services provided by the emulator core.
		/// </summary>
		public static bool IsAvailable(IEmulatorServiceProvider source, Type targetType)
			=> GetServicePropsFor(targetType, mayCache: false).Req.TrueForAll(info => source.HasService(info.PropType));
	}

	[AttributeUsage(AttributeTargets.Property)]
	public sealed class RequiredServiceAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Property)]
	public sealed class OptionalServiceAttribute : Attribute
	{
	}
}
