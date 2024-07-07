using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BizHawk.BizInvoke
{
	public static class BizInvokeUtilities
	{
		/// <summary>
		/// create a delegate type to match a method type
		/// </summary>
		/// <param name="method">the method to "clone"</param>
		/// <param name="nativeCall">native calling convention to use</param>
		/// <param name="enclosingType">the type to define this delegate type as a nested type on</param>
		/// <param name="invokeMethod">the methodBuilder for the magic Invoke method on the resulting type</param>
		/// <returns>the resulting typeBuilder</returns>
		public static TypeBuilder CreateDelegateType(MethodInfo method, CallingConvention nativeCall, TypeBuilder enclosingType,
			out MethodBuilder invokeMethod)
		{
			var paramInfos = method.GetParameters();
			var paramTypes = paramInfos.Select(p => p.ParameterType).ToArray();
			var returnType = method.ReturnType;

			// create the delegate type
			var delegateType = enclosingType.DefineNestedType(
				$"DelegateType{method.Name}",
				TypeAttributes.Class | TypeAttributes.NestedPrivate | TypeAttributes.Sealed,
				typeof(MulticastDelegate));

			var delegateCtor = delegateType.DefineConstructor(
				MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public,
				CallingConventions.Standard,
				new[] { typeof(object), typeof(IntPtr) });

			// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
			delegateCtor.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

			var delegateInvoke = delegateType.DefineMethod(
				"Invoke",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
				returnType,
				paramTypes);

			// we have to project all of the attributes from the baseMethod to the delegateInvoke
			// so for something like [Out], the interop engine will see it and use it
			for (var i = 0; i < paramInfos.Length; i++)
			{
				var p = delegateInvoke.DefineParameter(i + 1, ParameterAttributes.None, paramInfos[i].Name);
				foreach (var a in paramInfos[i].GetCustomAttributes(false))
				{
					p.SetCustomAttribute(GetAttributeBuilder(a));
				}
			}

			{
				var p = delegateInvoke.DefineParameter(0, ParameterAttributes.Retval, method.ReturnParameter!.Name);
				foreach (var a in method.ReturnParameter.GetCustomAttributes(false))
				{
					p.SetCustomAttribute(GetAttributeBuilder(a));
				}
			}

			// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
			delegateInvoke.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

			// add the [UnmanagedFunctionPointer] to the delegate so interop will know how to call it
			var attr = new CustomAttributeBuilder(
				typeof(UnmanagedFunctionPointerAttribute).GetConstructor(new[] { typeof(CallingConvention) })!,
				new object[] { nativeCall });
			delegateType.SetCustomAttribute(attr);

			invokeMethod = delegateInvoke;
			return delegateType;
		}

		/// <summary>
		/// get an attribute builder to clone an attribute to a delegate type
		/// </summary>
		private static CustomAttributeBuilder GetAttributeBuilder(object o)
		{
			// anything more clever we can do here?
#if NETSTANDARD2_1_OR_GREATER || NET471_OR_GREATER || NETCOREAPP2_0_OR_GREATER
			if (o is OutAttribute or InAttribute or IsReadOnlyAttribute)
#else
			if (o is OutAttribute or InAttribute
				|| o.GetType().FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute") // I think without this, you'd still only hit the below runtime assertion when this project targets e.g. netstandard2.0 but a core targets e.g. net8.0, so unlikely --yoshi
#endif
			{
				return new(o.GetType().GetConstructor(Type.EmptyTypes)!, Array.Empty<object>());
			}
			if (o is MarshalAsAttribute marshalAsAttr)
			{
				return new(
					typeof(MarshalAsAttribute).GetConstructor(new[] { typeof(UnmanagedType) })!,
					new object[] { marshalAsAttr.Value }
				);
			}
			throw new InvalidOperationException($"parameter of a BizInvoke method had unknown attribute {o.GetType().FullName}");
		}
	}
}
