using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NLua.Extensions
{
	internal static class TypeExtensions
	{
		public static bool HasMethod(this Type t, string name)
		{
			var op = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			return op.Any(m => m.Name == name);
		}

		public static bool HasAdditionOperator(this Type t)
			=> t.IsPrimitive || t.HasMethod("op_Addition");

		public static bool HasSubtractionOperator(this Type t)
			=> t.IsPrimitive || t.HasMethod("op_Subtraction");

		public static bool HasMultiplyOperator(this Type t)
			=> t.IsPrimitive || t.HasMethod("op_Multiply");

		public static bool HasDivisionOperator(this Type t)
			=> t.IsPrimitive || t.HasMethod("op_Division");

		public static bool HasModulusOperator(this Type t)
			=> t.IsPrimitive || t.HasMethod("op_Modulus");

		public static bool HasUnaryNegationOperator(this Type t)
		{
			if (t.IsPrimitive)
			{
				return true;
			}

			// Unary - will always have only one version.
			var op = t.GetMethod("op_UnaryNegation", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			return op != null;
		}

		public static bool HasEqualityOperator(this Type t)
			=> t.IsPrimitive || t.HasMethod("op_Equality");

		public static bool HasLessThanOperator(this Type t)
			=> t.IsPrimitive || t.HasMethod("op_LessThan");

		public static bool HasLessThanOrEqualOperator(this Type t)
			=> t.IsPrimitive || t.HasMethod("op_LessThanOrEqual");

		public static MethodInfo[] GetMethods(this Type t, string name, BindingFlags flags)
			=> t.GetMethods(flags).Where(m => m.Name == name).ToArray();

		public static MethodInfo[] GetExtensionMethods(this Type type, string name, IEnumerable<Assembly> assemblies = null)
		{
			var types = new List<Type>();
			types.AddRange(type.Assembly.GetTypes().Where(t => t.IsPublic));

			if (assemblies != null)
			{
				foreach (var item in assemblies)
				{
					if (item == type.Assembly)
					{
						continue;
					}

					types.AddRange(item.GetTypes().Where(t => t.IsPublic && t.IsClass && t.IsSealed && t.IsAbstract && !t.IsNested));
				}
			}

			// this thing is complex and not sure recommended changes are safe
#pragma warning disable MA0029 // Combine LINQ methods
#pragma warning disable BHI1002 // Do not use anonymous types (classes)
			var query = types
				.SelectMany(extensionType => extensionType.GetMethods(name, BindingFlags.Static | BindingFlags.Public),
					(extensionType, method) => new {extensionType, method})
				.Where(t => t.method.IsDefined(typeof(ExtensionAttribute), false))
				.Where(t =>
					t.method.GetParameters()[0].ParameterType == type ||
						t.method.GetParameters()[0].ParameterType.IsAssignableFrom(type) ||
						type.GetInterfaces().Contains(t.method.GetParameters()[0].ParameterType))
				.Select(t => t.method);
#pragma warning restore BHI1002 // Do not use anonymous types (classes)
#pragma warning restore MA0029 // Combine LINQ methods

			return query.ToArray();
		}

		/// <summary>
		/// Extends the System.Type-type to search for a given extended MethodeName.
		/// </summary>
		/// <param name="t"></param>
		/// <param name="name"></param>
		/// <param name="assemblies"></param>
		/// <returns></returns>
		public static MethodInfo GetExtensionMethod(this Type t, string name, IEnumerable<Assembly> assemblies = null)
		{
			var mi = t.GetExtensionMethods(name, assemblies);
			return mi.Length == 0 ? null : mi[0];
		}
	}
}
