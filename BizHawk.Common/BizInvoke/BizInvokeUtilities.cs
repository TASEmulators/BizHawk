using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.Common.BizInvoke
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
				"DelegateType" + method.Name,
				TypeAttributes.Class | TypeAttributes.NestedPrivate | TypeAttributes.Sealed,
				typeof(MulticastDelegate));

			var delegateCtor = delegateType.DefineConstructor(
				MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public,
				CallingConventions.Standard,
				new[] { typeof(object), typeof(IntPtr) });

			delegateCtor.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

			var delegateInvoke = delegateType.DefineMethod(
				"Invoke",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
				returnType,
				paramTypes);

			// we have to project all of the attributes from the baseMethod to the delegateInvoke
			// so for something like [Out], the interop engine will see it and use it
			for (int i = 0; i < paramInfos.Length; i++)
			{
				var p = delegateInvoke.DefineParameter(i + 1, ParameterAttributes.None, paramInfos[i].Name);
				foreach (var a in paramInfos[i].GetCustomAttributes(false))
				{
					p.SetCustomAttribute(GetAttributeBuilder(a));
				}
			}

			{
				var p = delegateInvoke.DefineParameter(0, ParameterAttributes.Retval, method.ReturnParameter.Name);
				foreach (var a in method.ReturnParameter.GetCustomAttributes(false))
				{
					p.SetCustomAttribute(GetAttributeBuilder(a));
				}
			}

			delegateInvoke.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

			// add the [UnmanagedFunctionPointer] to the delegate so interop will know how to call it
			var attr = new CustomAttributeBuilder(typeof(UnmanagedFunctionPointerAttribute).GetConstructor(new[] { typeof(CallingConvention) }), new object[] { nativeCall });
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
			var t = o.GetType();
			if (t == typeof(OutAttribute) || t == typeof(InAttribute))
			{
				return new CustomAttributeBuilder(t.GetConstructor(Type.EmptyTypes), new object[0]);
			}

			throw new InvalidOperationException("Unknown parameter attribute " + t.Name);
		}
	}
}
