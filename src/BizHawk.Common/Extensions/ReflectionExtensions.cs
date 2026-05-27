using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace BizHawk.Common.ReflectionExtensions
{
	/// <summary>
	/// Reflection based helper methods
	/// </summary>
	public static class ReflectionExtensions
	{
		/// <summary>filter used when looking for <c>[RequiredApi]</c> et al. by reflection for dependency injection</summary>
		public const BindingFlags DI_TARGET_PROPS = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

		public static IEnumerable<PropertyInfo> GetPropertiesWithAttrib(this Type type, Type attributeType)
		{
			return type.GetProperties(DI_TARGET_PROPS)
				.Where(p => p.GetCustomAttributes(attributeType, false).Length > 0);
		}

		public static IEnumerable<MethodInfo> GetMethodsWithAttrib(this Type type, Type attributeType)
		{
			return type.GetMethods(DI_TARGET_PROPS)
				.Where(p => p.GetCustomAttributes(attributeType, false).Length > 0);
		}

		/// <summary>
		/// Gets the description attribute from an object
		/// </summary>
		public static string GetDescription(this object obj)
		{
			var type = obj.GetType();

			var memInfo = type.GetMember(obj.ToString());

			if (memInfo.Length > 0)
			{
				var attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

				if (attrs.Length > 0)
				{
					return ((DescriptionAttribute)attrs[0]).Description;
				}
			}

			return obj.ToString();
		}

		/// <returns><see cref="DisplayNameAttribute">[DisplayName]</see>, falling back to <see cref="MemberInfo.Name"/></returns>
		public static string DisplayName(this MemberInfo type)
		{
			var attr = type.GetCustomAttributes(typeof(DisplayNameAttribute), false).FirstOrDefault();
			return attr is DisplayNameAttribute displayName ? displayName.DisplayName : type.Name;
		}

		/// <summary>
		/// Gets an enum from a description attribute
		/// </summary>
		/// <param name="description">The description attribute value</param>
		/// <typeparam name="T">The type of the enum</typeparam>
		/// <returns>An enum value with the given description attribute, if no suitable description is found then a default value of the enum is returned</returns>
		/// <remarks>implementation from https://stackoverflow.com/a/4367868/7467292</remarks>
		public static T GetEnumFromDescription<T>(this string description)
			where T : struct, Enum
		{
			var type = typeof(T);

			foreach (var fi in type.GetFields())
			{
				var memberDesc = fi.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute descAttr
					? descAttr.Description
					: fi.Name;
				if (memberDesc == description) return (T) fi.GetValue(null);
			}

			return default(T);
		}

		/// <summary>
		/// Takes an enum Type and generates a list of strings from the description attributes
		/// </summary>
		public static IEnumerable<string> GetEnumDescriptions(this Type type)
			=> Enum.GetValues(type).Cast<Enum>().Select(static v => v.GetDescription());

		public static T GetAttribute<T>(this object o)
		{
			return (T)o.GetType().GetCustomAttributes(typeof(T), false)[0];
		}

		/// <returns>
		/// <see langword="true"/> if <paramref name="parameter"/>'s type was referenced within <c>#nullable enable</c> with a <c>?</c>,
		/// <see langword="false"/> if it was referenced within <c>#nullable enable</c> without a <c>?</c>,
		/// or <see langword="null"/> if it was referenced within <c>#nullable disable</c>
		/// </returns>
		/// <exception cref="ArgumentException"><paramref name="parameter"/> is { <see cref="ParameterInfo.ParameterType" />.<see cref="Type.IsValueType" />: <see langword="true"/> }</exception>
		public static bool? IsNRT(this ParameterInfo parameter)
		{
			if (parameter.ParameterType.IsValueType) throw new ArgumentException(paramName: nameof(parameter), message: "this parameter is a value type; it does not participate in nullable reference types");

			// Check [Nullable] on the parameter first
			const byte AnnotatedNotNull = 1; // https://github.com/dotnet/roslyn/blob/main/docs/features/nullable-metadata.md
			var nrtAttr = parameter.GetCustomAttributes().FirstOrDefault(attr => attr.GetType().FullName is "System.Runtime.CompilerServices.NullableAttribute"); // reflection required because the type is Source Generated for each assembly
			if (nrtAttr?.GetType().GetField("NullableFlags").GetValue(nrtAttr) is byte[] flags) // have to use reflection here for the same reason
			{
				return flags[0] is not AnnotatedNotNull;
			}

			// Check [NullableContext] on the method and containing type(s)
			MemberInfo parent = parameter.Member;
			while (parent is not null)
			{
				var contextAttr = parent.GetCustomAttributes().FirstOrDefault(attr => attr.GetType().FullName is "System.Runtime.CompilerServices.NullableContextAttribute"); // ditto
				if (contextAttr?.GetType().GetField("Flag").GetValue(contextAttr) is byte flag)
				{
					return flag is not AnnotatedNotNull;
				}
				parent = parent.DeclaringType;
			}

			return null;
		}

		/// <returns>
		/// (when <paramref name="pi"/> is { <see cref="ParameterInfo.ParameterType" />.<see cref="Type.IsValueType" />: <see langword="true"/> })
		/// <see langword="true"/> iff <paramref name="pi"/>'s type is any parameterisation of <see cref="Nullable{T}"/>
		/// <br/>(when <paramref name="pi"/> is { <see cref="ParameterInfo.ParameterType" />.<see cref="Type.IsValueType" />: <see langword="false"/> })
		/// <see langword="true"/> if <paramref name="pi"/>'s type was referenced within <c>#nullable enable</c> with a <c>?</c>,
		/// <see langword="false"/> if it was referenced within <c>#nullable enable</c> without a <c>?</c>,
		/// or <see langword="null"/> if it was referenced within <c>#nullable disable</c>
		/// </returns>
		/// <remarks>simple wrapper over <see cref="IsNRT"/> and <see cref="IsNullableT(ParameterInfo)"/></remarks>
		public static bool? IsNRTOrNullableT(this ParameterInfo pi)
		{
			var type = pi.ParameterType;
			return type.IsValueType ? IsNullableT(type) : IsNRT(pi);
		}

		/// <returns><see langword="true"/> iff <paramref name="pi"/>'s type is any parameterisation of <see cref="Nullable{T}"/></returns>
		/// <exception cref="ArgumentException"><paramref name="pi"/> is { <see cref="ParameterInfo.ParameterType" />.<see cref="Type.IsValueType" />: <see langword="false"/> }</exception>
		public static bool IsNullableT(this ParameterInfo pi)
		{
			var type = pi.ParameterType;
			return type.IsValueType ? type.IsNullableT() : throw new ArgumentException(paramName: nameof(pi), message: "this parameter is a reference type so it's obviously not Nullable<T>");
		}

		/// <returns><see langword="true"/> iff <paramref name="type"/> is any parameterisation of <see cref="Nullable{T}"/></returns>
		public static bool IsNullableT(this Type type)
			=> type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
	}
}
