using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Common.BizInvoke
{
	public static class BizInvoker
	{
		public static T GetInvoker<T>(IImportResolver dll)
			where T : class
		{
			var baseType = typeof(T);
			if (baseType.IsSealed)
				throw new InvalidOperationException("Can't proxy a sealed type");
			if (!baseType.IsPublic)
				// the proxy type will be in a new assembly, so public is required here
				throw new InvalidOperationException("Type must be public");

			var baseConstructor = baseType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
			if (baseConstructor == null)
				throw new InvalidOperationException("Base type must have a zero arg constructor");

			var baseMethods = baseType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
				.Select(m => new
				{
					Info = m,
					Attr = m.GetCustomAttributes(true).OfType<BizImportAttribute>().FirstOrDefault()
				})
				.Where(a => a.Attr != null)
				.ToList();

			if (baseMethods.Count == 0)
				throw new InvalidOperationException("Couldn't find any [BizImport] methods to proxy");

			{
				var uo = baseMethods.FirstOrDefault(a => !a.Info.IsVirtual || a.Info.IsFinal);
				if (uo != null)
					throw new InvalidOperationException("Method " + uo.Info.Name + " cannot be overriden!");

				// there's no technical reason to disallow this, but we wouldn't be doing anything
				// with the base implementation, so it's probably a user error
				var na = baseMethods.FirstOrDefault(a => !a.Info.IsAbstract);
				if (na != null)
					throw new InvalidOperationException("Method " + na.Info.Name + " is not abstract!");
			}

			// hooks that will be run on the created proxy object
			var postCreateHooks = new List<Action<object>>();

			var aname = new AssemblyName(baseType.Name + Guid.NewGuid().ToString("N"));
			var assy = AppDomain.CurrentDomain.DefineDynamicAssembly(aname, AssemblyBuilderAccess.Run);
			var module = assy.DefineDynamicModule("BizInvoker");
			var type = module.DefineType("Bizhawk.BizInvokeProxy", TypeAttributes.Class | TypeAttributes.Public, baseType);

			foreach (var mi in baseMethods)
			{
				var entryPointName = mi.Attr.EntryPoint ?? mi.Info.Name;
				var entryPtr = dll.Resolve(entryPointName);
				if (entryPtr == IntPtr.Zero)
					throw new InvalidOperationException("Resolver returned NULL for entry point " + entryPointName);

				if (false)
				{
					ImplementMethodCalli(type, mi.Info, entryPtr, mi.Attr.CallingConvention);
				}
				else
				{
					var hook = ImplementMethodDelegate(type, mi.Info, entryPtr, mi.Attr.CallingConvention);
					postCreateHooks.Add(hook);
				}
			}

			var ret = Activator.CreateInstance(type.CreateType());
			foreach (var hook in postCreateHooks)
				hook(ret);
			return (T)ret;
		}

		private static Action<object> ImplementMethodDelegate(TypeBuilder type, MethodInfo baseMethod, IntPtr entryPtr, CallingConvention nativeCall)
		{
			var paramInfos = baseMethod.GetParameters();
			var paramTypes = paramInfos.Select(p => p.ParameterType).ToArray();
			var returnType = baseMethod.ReturnType;

			// create the delegate type
			var delegateType = type.DefineNestedType("DelegateType" + baseMethod.Name,
				TypeAttributes.Class | TypeAttributes.NestedPrivate | TypeAttributes.Sealed, typeof(MulticastDelegate));
			var delegateCtor = delegateType.DefineConstructor(
				MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public,
				CallingConventions.Standard, new Type[] { typeof(object), typeof(IntPtr) });
			delegateCtor.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
			var delegateInvoke = delegateType.DefineMethod("Invoke",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, returnType, paramTypes);
			delegateInvoke.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
			// add the [UnmanagedFunctionPointer] to the delegate so interop will know how to call it
			var attr = new CustomAttributeBuilder(typeof(UnmanagedFunctionPointerAttribute).GetConstructor(new[] { typeof(CallingConvention) }), new object[] { nativeCall });
			delegateType.SetCustomAttribute(attr);

			// define a field on the class to hold the delegate
			var field = type.DefineField("DelegateField" + baseMethod.Name, delegateType,
				FieldAttributes.Public);

			var method = type.DefineMethod(baseMethod.Name, MethodAttributes.Virtual | MethodAttributes.Public,
				CallingConventions.HasThis, returnType, paramTypes);
			var il = method.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, field);
			for (int i = 0; i < paramTypes.Length; i++)
				il.Emit(OpCodes.Ldarg, (short)(i + 1));
			il.Emit(OpCodes.Callvirt, delegateInvoke);

			il.Emit(OpCodes.Ret);

			type.DefineMethodOverride(method, baseMethod);

			return (o) =>
			{
				var interopDelegate = Marshal.GetDelegateForFunctionPointer(entryPtr, delegateType.CreateType());
				o.GetType().GetField(field.Name).SetValue(o, interopDelegate);
			};
		}

		private static void ImplementMethodCalli(TypeBuilder type, MethodInfo baseMethod, IntPtr entryPtr, CallingConvention nativeCall)
		{
			var paramInfos = baseMethod.GetParameters();
			var paramTypes = paramInfos.Select(p => p.ParameterType).ToArray();
			var nativeParamTypes = new List<Type>();
			var returnType = baseMethod.ReturnType;
			var method = type.DefineMethod(baseMethod.Name, MethodAttributes.Virtual | MethodAttributes.Public,
				CallingConventions.HasThis, returnType, paramTypes);

			if (returnType != typeof(void) && !returnType.IsPrimitive)
				throw new InvalidOperationException("Only primitive return types are supported");

			var il = method.GetILGenerator();
			for (int i = 0; i < paramTypes.Length; i++)
			{
				// arg 0 is this, so + 1
				nativeParamTypes.Add(EmitParamterLoad(il, i + 1, paramTypes[i]));
			}
			LoadConstant(il, entryPtr);
			il.EmitCalli(OpCodes.Calli, nativeCall, returnType, nativeParamTypes.ToArray());

			// either there's a primitive on the stack and we're expected to return that primitive,
			// or there's nothing on the stack and we're expected to return nothing
			il.Emit(OpCodes.Ret);

			type.DefineMethodOverride(method, baseMethod);
		}

		/// <summary>
		/// load an IntPtr constant in an il stream
		/// </summary>
		private static void LoadConstant(ILGenerator il, IntPtr p)
		{
			if (p == IntPtr.Zero)
				il.Emit(OpCodes.Ldc_I4_0);
			else if (IntPtr.Size == 4)
				il.Emit(OpCodes.Ldc_I4, (int)p);
			else
				il.Emit(OpCodes.Ldc_I8, (long)p);
			il.Emit(OpCodes.Conv_I);
		}

		/// <summary>
		/// load a UIntPtr constant in an il stream
		/// </summary>
		private static void LoadConstant(ILGenerator il, UIntPtr p)
		{
			if (p == UIntPtr.Zero)
				il.Emit(OpCodes.Ldc_I4_0);
			else if (UIntPtr.Size == 4)
				il.Emit(OpCodes.Ldc_I4, (int)p);
			else
				il.Emit(OpCodes.Ldc_I8, (long)p);
			il.Emit(OpCodes.Conv_U);
		}

		private static Type EmitParamterLoad(ILGenerator il, int idx, Type type)
		{
			if (type.IsGenericType)
				throw new InvalidOperationException("Generic types not supported");
			if (type.IsByRef)
			{
				var et = type.GetElementType();
				if (!et.IsPrimitive)
					throw new InvalidOperationException("Only refs of primitive types are supported!");
				var loc = il.DeclareLocal(type, true);
				il.Emit(OpCodes.Ldarg, (short)idx);
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Stloc, loc);
				il.Emit(OpCodes.Conv_I);
				return typeof(IntPtr);
			}
			else if (type.IsArray)
			{
				var et = type.GetElementType();
				if (!et.IsPrimitive)
					throw new InvalidOperationException("Only arrays of primitive types are supported!");

				// these two cases aren't too hard to add
				if (type.GetArrayRank() > 1)
					throw new InvalidOperationException("Multidimensional arrays are not supported!");
				if (type.Name.Contains('*'))
					throw new InvalidOperationException("Only 0-based 1-dimensional arrays are supported!");

				var loc = il.DeclareLocal(type, true);
				var end = il.DefineLabel();
				var isNull = il.DefineLabel();

				il.Emit(OpCodes.Ldarg, (short)idx);
				il.Emit(OpCodes.Brfalse, isNull);

				il.Emit(OpCodes.Ldarg, (short)idx);
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Stloc, loc);
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Ldelema, et);
				il.Emit(OpCodes.Conv_I);
				il.Emit(OpCodes.Br, end);

				il.MarkLabel(isNull);
				LoadConstant(il, IntPtr.Zero);
				il.MarkLabel(end);

				return typeof(IntPtr);
			}
			else if (typeof(Delegate).IsAssignableFrom(type))
			{
				var mi = typeof(Marshal).GetMethod("GetFunctionPointerForDelegate", new[] { typeof(Delegate) });
				var end = il.DefineLabel();
				var isNull = il.DefineLabel();

				il.Emit(OpCodes.Ldarg, (short)idx);
				il.Emit(OpCodes.Brfalse, isNull);

				il.Emit(OpCodes.Ldarg, (short)idx);
				il.Emit(OpCodes.Call, mi);
				il.Emit(OpCodes.Br, end);

				il.MarkLabel(isNull);
				LoadConstant(il, IntPtr.Zero);
				il.MarkLabel(end);
				return typeof(IntPtr);
			}
			else if (type.IsPrimitive)
			{
				il.Emit(OpCodes.Ldarg, (short)idx);
				return type;
			}
			else
			{
				throw new InvalidOperationException("Unrecognized parameter type!");
			}
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class BizImportAttribute : Attribute
	{
		public CallingConvention CallingConvention
		{
			get { return _callingConvention; }
		}
		private readonly CallingConvention _callingConvention;

		public string EntryPoint { get; set; }

		public BizImportAttribute(CallingConvention c)
		{
			_callingConvention = c;
		}
	}
}
