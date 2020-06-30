using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using BizHawk.Common;

namespace BizHawk.BizInvoke
{
	public static class BizInvoker
	{
		/// <summary>
		/// holds information about a proxy implementation, including type and setup hooks
		/// </summary>
		private class InvokerImpl
		{
			public Type ImplType;
			public List<Action<object, IImportResolver, ICallingConventionAdapter>> Hooks;
			public Action<object, IMonitor> ConnectMonitor;
			public Action<object, ICallingConventionAdapter> ConnectCallingConventionAdapter;

			public object Create(IImportResolver dll, IMonitor monitor, ICallingConventionAdapter adapter)
			{
				var ret = Activator.CreateInstance(ImplType);
				ConnectCallingConventionAdapter(ret, adapter);
				foreach (var f in Hooks)
				{
					f(ret, dll, adapter);
				}
				ConnectMonitor?.Invoke(ret, monitor);
				return ret;
			}
		}

		/// <summary>
		/// dictionary of all generated proxy implementations and their base types
		/// </summary>
		private static readonly IDictionary<Type, InvokerImpl> Impls = new Dictionary<Type, InvokerImpl>();

		/// <summary>
		/// the assembly that all proxies are placed in
		/// </summary>
		private static readonly AssemblyBuilder ImplAssemblyBuilder;

		/// <summary>
		/// the module that all proxies are placed in
		/// </summary>
		private static readonly ModuleBuilder ImplModuleBuilder;

		/// <summary>
		/// How far into a class pointer the first field is.  Different on mono and fw.
		/// </summary>
		private static readonly int ClassFieldOffset;
		/// <summary>
		/// How far into a string pointer the first chair is.
		/// </summary>
		private static readonly int StringOffset;

		static BizInvoker()
		{
			var aname = new AssemblyName("BizInvokeProxyAssembly");
			ImplAssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(aname, AssemblyBuilderAccess.Run);
			ImplModuleBuilder = ImplAssemblyBuilder.DefineDynamicModule("BizInvokerModule");
			ClassFieldOffset = BizInvokerUtilities.ComputeClassFieldOffset();
			StringOffset = BizInvokerUtilities.ComputeStringOffset();
		}

		/// <summary>
		/// get an implementation proxy for an interop class
		/// </summary>
		/// <typeparam name="T">The class type that represents the DLL</typeparam>
		/// <exception cref="InvalidOperationException"><see cref="GetInvoker{T}(BizHawk.Common.IImportResolver,BizHawk.Common.IMonitor,BizHawk.BizInvoke.ICallingConventionAdapter)"/> overload previously called with <paramref name="dll"/></exception>
		public static T GetInvoker<T>(IImportResolver dll, ICallingConventionAdapter adapter)
			where T : class
		{
			InvokerImpl impl;
			lock (Impls)
			{
				var baseType = typeof(T);
				if (!Impls.TryGetValue(baseType, out impl))
				{
					impl = CreateProxy(baseType, false);
					Impls.Add(baseType, impl);
				}
			}

			if (impl.ConnectMonitor != null)
			{
				throw new InvalidOperationException("Class was previously proxied with a monitor!");
			}

			return (T)impl.Create(dll, null, adapter);
		}

		/// <exception cref="InvalidOperationException">this method was previously called with <paramref name="dll"/></exception>
		public static T GetInvoker<T>(IImportResolver dll, IMonitor monitor, ICallingConventionAdapter adapter)
			where T : class
		{
			InvokerImpl impl;
			lock (Impls)
			{
				var baseType = typeof(T);
				if (!Impls.TryGetValue(baseType, out impl))
				{
					impl = CreateProxy(baseType, true);
					Impls.Add(baseType, impl);
				}
			}

			if (impl.ConnectMonitor == null)
			{
				throw new InvalidOperationException("Class was previously proxied without a monitor!");
			}

			return (T)impl.Create(dll, monitor, adapter);
		}

		private static InvokerImpl CreateProxy(Type baseType, bool monitor)
		{
			if (baseType.IsSealed)
			{
				throw new InvalidOperationException("Can't proxy a sealed type");
			}

			if (!baseType.IsPublic)
			{
				// the proxy type will be in a new assembly, so public is required here
				throw new InvalidOperationException("Type must be public");
			}

			var baseConstructor = baseType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
			if (baseConstructor == null)
			{
				throw new InvalidOperationException("Base type must have a zero arg constructor");
			}

			var baseMethods = baseType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
				.Select(m => new
				{
					Info = m,
					Attr = m.GetCustomAttributes(true).OfType<BizImportAttribute>().FirstOrDefault()
				})
				.Where(a => a.Attr != null)
				.ToList();

			if (baseMethods.Count == 0)
			{
				throw new InvalidOperationException("Couldn't find any [BizImport] methods to proxy");
			}

			{
				var uo = baseMethods.FirstOrDefault(a => !a.Info.IsVirtual || a.Info.IsFinal);
				if (uo != null)
				{
					throw new InvalidOperationException($"Method {uo.Info.Name} cannot be overriden!");
				}

				// there's no technical reason to disallow this, but we wouldn't be doing anything
				// with the base implementation, so it's probably a user error
				var na = baseMethods.FirstOrDefault(a => !a.Info.IsAbstract);
				if (na != null)
				{
					throw new InvalidOperationException($"Method {na.Info.Name} is not abstract!");
				}
			}

			// hooks that will be run on the created proxy object
			var postCreateHooks = new List<Action<object, IImportResolver, ICallingConventionAdapter>>();

			var type = ImplModuleBuilder.DefineType($"Bizhawk.BizInvokeProxy{baseType.Name}", TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed, baseType);

			var monitorField = monitor ? type.DefineField("MonitorField", typeof(IMonitor), FieldAttributes.Public) : null;

			var adapterField = type.DefineField("CallingConvention", typeof(ICallingConventionAdapter), FieldAttributes.Public);

			foreach (var mi in baseMethods)
			{
				var entryPointName = mi.Attr.EntryPoint ?? mi.Info.Name;

				var hook = mi.Attr.Compatibility
					? ImplementMethodDelegate(type, mi.Info, mi.Attr.CallingConvention, entryPointName, monitorField)
					: ImplementMethodCalli(type, mi.Info, mi.Attr.CallingConvention, entryPointName, monitorField, adapterField);

				postCreateHooks.Add(hook);
			}

			var ret = new InvokerImpl
			{
				Hooks = postCreateHooks,
				ImplType = type.CreateType()
			};
			if (monitor)
			{
				ret.ConnectMonitor = (o, m) => o.GetType().GetField(monitorField.Name).SetValue(o, m);
			}
			ret.ConnectCallingConventionAdapter = (o, a) => o.GetType().GetField(adapterField.Name).SetValue(o, a);

			return ret;
		}

		/// <summary>
		/// create a method implementation that uses GetDelegateForFunctionPointer internally
		/// </summary>
		private static Action<object, IImportResolver, ICallingConventionAdapter> ImplementMethodDelegate(
			TypeBuilder type, MethodInfo baseMethod, CallingConvention nativeCall, string entryPointName, FieldInfo monitorField)
		{
			// create the delegate type
			var delegateType = BizInvokeUtilities.CreateDelegateType(baseMethod, nativeCall, type, out var delegateInvoke);

			var paramInfos = baseMethod.GetParameters();
			var paramTypes = paramInfos.Select(p => p.ParameterType).ToArray();
			var returnType = baseMethod.ReturnType;

			if (paramTypes.Concat(new[] { returnType }).Any(typeof(Delegate).IsAssignableFrom))
			{
				// this isn't a problem if CallingConventionAdapters.Waterbox is a no-op
				if (CallingConventionAdapters.Waterbox.GetType() != CallingConventionAdapters.Native.GetType())
					throw new InvalidOperationException("Compatibility call mode cannot use ICallingConventionAdapters for automatically marshalled delegate types!");
			}

			// define a field on the class to hold the delegate
			var field = type.DefineField(
				$"DelegateField{baseMethod.Name}",
				delegateType,
				FieldAttributes.Public);

			var method = type.DefineMethod(
				baseMethod.Name,
				MethodAttributes.Virtual | MethodAttributes.Public,
				CallingConventions.HasThis,
				returnType,
				paramTypes);

			var il = method.GetILGenerator();

			Label exc = new Label();
			if (monitorField != null) // monitor: enter and then begin try
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, monitorField);
				il.Emit(OpCodes.Callvirt, typeof(IMonitor).GetMethod("Enter"));
				exc = il.BeginExceptionBlock();
			}

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, field);
			for (int i = 0; i < paramTypes.Length; i++)
			{
				il.Emit(OpCodes.Ldarg, (short)(i + 1));
			}

			il.Emit(OpCodes.Callvirt, delegateInvoke);

			if (monitorField != null) // monitor: finally exit
			{
				LocalBuilder loc = null;
				if (returnType != typeof(void))
				{
					loc = il.DeclareLocal(returnType);
					il.Emit(OpCodes.Stloc, loc);
				}

				il.Emit(OpCodes.Leave, exc);
				il.BeginFinallyBlock();
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, monitorField);
				il.Emit(OpCodes.Callvirt, typeof(IMonitor).GetMethod("Exit"));
				il.EndExceptionBlock();

				if (returnType != typeof(void))
				{
					il.Emit(OpCodes.Ldloc, loc);
				}
			}

			il.Emit(OpCodes.Ret);

			type.DefineMethodOverride(method, baseMethod);

			return (o, dll, adapter) =>
			{
				var entryPtr = dll.GetProcAddrOrThrow(entryPointName);
				var interopDelegate = adapter.GetDelegateForFunctionPointer(entryPtr, delegateType.CreateType());
				o.GetType().GetField(field.Name).SetValue(o, interopDelegate);
			};
		}

		private class ParameterLoadInfo
		{
			/// <summary>
			/// The native type for this parameter, to pass to calli
			/// </summary>
			public Type NativeType;
			/// <summary>
			/// Closure that will actually emit the parameter load to the il stream.  The evaluation stack will
			/// already have other parameters on it at this time.
			/// </summary>
			public Action EmitLoad;
		}

		/// <summary>
		/// create a method implementation that uses calli internally
		/// </summary>
		private static Action<object, IImportResolver, ICallingConventionAdapter> ImplementMethodCalli(
			TypeBuilder type, MethodInfo baseMethod,
			CallingConvention nativeCall, string entryPointName, FieldInfo monitorField, FieldInfo adapterField)
		{
			var paramInfos = baseMethod.GetParameters();
			var paramTypes = paramInfos.Select(p => p.ParameterType).ToArray();
			var paramLoadInfos = new List<ParameterLoadInfo>();
			var returnType = baseMethod.ReturnType;
			if (returnType != typeof(void) && !returnType.IsPrimitive && !returnType.IsPointer && !returnType.IsEnum)
			{
				throw new InvalidOperationException("Only primitive return types are supported");
			}

			// define a field on the type to hold the entry pointer
			var field = type.DefineField(
				$"EntryPtrField{baseMethod.Name}",
				typeof(IntPtr),
				FieldAttributes.Public);

			var method = type.DefineMethod(
				baseMethod.Name,
				MethodAttributes.Virtual | MethodAttributes.Public,
				CallingConventions.HasThis,
				returnType,
				paramTypes);

			var il = method.GetILGenerator();

			Label exc = new Label();
			if (monitorField != null) // monitor: enter and then begin try
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, monitorField);
				il.Emit(OpCodes.Callvirt, typeof(IMonitor).GetMethod("Enter"));
				exc = il.BeginExceptionBlock();
			}

			// phase 1:  empty eval stack and each parameter load thunk does any prep work it needs to do
			for (int i = 0; i < paramTypes.Length; i++)
			{
				// arg 0 is this, so + 1
				paramLoadInfos.Add(EmitParamterLoad(il, i + 1, paramTypes[i], adapterField));
			}
			// phase 2:  actually load the individual params, leaving each one on the stack
			foreach (var pli in paramLoadInfos)
			{
				pli.EmitLoad();
			}

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, field);
			il.EmitCalli(OpCodes.Calli, 
				nativeCall, 
				returnType == typeof(bool) ? typeof(byte) : returnType, // undo winapi style bool garbage
				paramLoadInfos.Select(p => p.NativeType).ToArray());

			if (monitorField != null) // monitor: finally exit
			{
				LocalBuilder loc = null;
				if (returnType != typeof(void))
				{
					loc = il.DeclareLocal(returnType);
					il.Emit(OpCodes.Stloc, loc);
				}

				il.Emit(OpCodes.Leave, exc);
				il.BeginFinallyBlock();
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, monitorField);
				il.Emit(OpCodes.Callvirt, typeof(IMonitor).GetMethod("Exit"));
				il.EndExceptionBlock();

				if (returnType != typeof(void))
				{
					il.Emit(OpCodes.Ldloc, loc);
				}
			}

			// either there's a primitive on the stack and we're expected to return that primitive,
			// or there's nothing on the stack and we're expected to return nothing
			il.Emit(OpCodes.Ret);

			type.DefineMethodOverride(method, baseMethod);

			return (o, dll, adapter) =>
			{
				var entryPtr = dll.GetProcAddrOrThrow(entryPointName);
				o.GetType().GetField(field.Name).SetValue(
					o, adapter.GetDepartureFunctionPointer(entryPtr, new ParameterInfo(returnType, paramTypes), o));
			};
		}

		/// <summary>
		/// load an IntPtr constant in an IL stream
		/// </summary>
		private static void LoadConstant(ILGenerator il, IntPtr p)
		{
			if (p == IntPtr.Zero)
			{
				il.Emit(OpCodes.Ldc_I4_0);
			}
			else if (IntPtr.Size == 4)
			{
				il.Emit(OpCodes.Ldc_I4, (int)p);
			}
			else
			{
				il.Emit(OpCodes.Ldc_I8, (long)p);
			}

			il.Emit(OpCodes.Conv_I);
		}

		/// <summary>
		/// load a UIntPtr constant in an IL stream
		/// </summary>
		private static void LoadConstant(ILGenerator il, UIntPtr p)
		{
			if (p == UIntPtr.Zero)
			{
				il.Emit(OpCodes.Ldc_I4_0);
			}
			else if (UIntPtr.Size == 4)
			{
				il.Emit(OpCodes.Ldc_I4, (int)p);
			}
			else
			{
				il.Emit(OpCodes.Ldc_I8, (long)p);
			}

			il.Emit(OpCodes.Conv_U);
		}

		/// <summary>
		/// emit a single parameter load with unmanaged conversions.  The evaluation stack will be empty when the IL generated here runs,
		/// and should end as empty.
		/// </summary>
		private static ParameterLoadInfo EmitParamterLoad(ILGenerator il, int idx, Type type, FieldInfo adapterField)
		{
			if (type.IsGenericType)
			{
				throw new NotImplementedException("Generic types not supported");
			}

			if (type.IsByRef)
			{
				var et = type.GetElementType();
				if (!et.IsPrimitive && !et.IsEnum)
				{
					throw new NotImplementedException("Only refs of primitive or enum types are supported!");
				}
				return new ParameterLoadInfo
				{
					NativeType = typeof(IntPtr),
					EmitLoad = () =>
					{
						var loc = il.DeclareLocal(type, true);
						il.Emit(OpCodes.Ldarg, (short)idx);
						il.Emit(OpCodes.Dup);
						il.Emit(OpCodes.Stloc, loc);
						il.Emit(OpCodes.Conv_I);
					}
				};
			}

			if (type.IsArray)
			{
				var et = type.GetElementType();
				if (!et.IsValueType)
				{
					throw new NotImplementedException("Only arrays of value types are supported!");
				}

				// these two cases aren't too hard to add
				if (type.GetArrayRank() > 1)
				{
					throw new NotImplementedException("Multidimensional arrays are not supported!");
				}

				if (type.Name.Contains('*'))
				{
					throw new NotImplementedException("Only 0-based 1-dimensional arrays are supported!");
				}

				return new ParameterLoadInfo
				{
					NativeType = typeof(IntPtr),
					EmitLoad = () => 
					{
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
					}
				};
			}

			if (typeof(Delegate).IsAssignableFrom(type))
			{
				// callback -- use the same callingconventionadapter on it that the invoker is being made from
				return new ParameterLoadInfo
				{
					NativeType = typeof(IntPtr),
					EmitLoad = () =>
					{
						var mi = typeof(ICallingConventionAdapter).GetMethod("GetFunctionPointerForDelegate");
						var end = il.DefineLabel();
						var isNull = il.DefineLabel();

						il.Emit(OpCodes.Ldarg, (short)idx);
						il.Emit(OpCodes.Brfalse, isNull);

						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Ldfld, adapterField);
						il.Emit(OpCodes.Ldarg, (short)idx);
						il.EmitCall(OpCodes.Callvirt, mi, Type.EmptyTypes);
						il.Emit(OpCodes.Br, end);

						il.MarkLabel(isNull);
						LoadConstant(il, IntPtr.Zero);
						il.MarkLabel(end);
					}
				};
			}

			if (type == typeof(string))
			{
				var end = il.DefineLabel();
				var isNull = il.DefineLabel();

				il.Emit(OpCodes.Ldarg, (short)idx);
				il.Emit(OpCodes.Brfalse, isNull);

				var encoding = il.DeclareLocal(typeof(Encoding), false);
				il.EmitCall(OpCodes.Call, typeof(Encoding).GetProperty("UTF8").GetGetMethod(), Type.EmptyTypes);
				il.Emit(OpCodes.Stloc, encoding);

				var strlenbytes = il.DeclareLocal(typeof(int), false);
				il.Emit(OpCodes.Ldloc, encoding);
				il.Emit(OpCodes.Ldarg, (short)idx);
				il.Emit(OpCodes.Call, typeof(Encoding).GetMethod("GetByteCount", new[] { typeof(string) }));
				il.Emit(OpCodes.Stloc, strlenbytes);

				var strval = il.DeclareLocal(typeof(string), true); // pin!
				il.Emit(OpCodes.Ldarg, (short)idx);
				il.Emit(OpCodes.Stloc, strval);

				var bytes = il.DeclareLocal(typeof(IntPtr));
				il.Emit(OpCodes.Ldloc, strlenbytes);
				il.Emit(OpCodes.Ldc_I4_1);
				il.Emit(OpCodes.Add); // +1 for null byte
				il.Emit(OpCodes.Conv_U);
				// NB: The evaluation stack must be entirely empty, except for the size argument, when calling localloc.
				// That's why we have to split every parameter load into two parts, the first of which runs on an empty stack.
				il.Emit(OpCodes.Localloc);
				il.Emit(OpCodes.Stloc, bytes);

				// this
				il.Emit(OpCodes.Ldloc, encoding);
				// chars
				il.Emit(OpCodes.Ldloc, strval);
				il.Emit(OpCodes.Conv_U);
				il.Emit(OpCodes.Ldc_I4, StringOffset);
				il.Emit(OpCodes.Add);
				// charcount
				il.Emit(OpCodes.Ldloc, strval);
				il.Emit(OpCodes.Call, typeof(string).GetProperty("Length").GetGetMethod());
				// bytes
				il.Emit(OpCodes.Ldloc, bytes);
				// bytelength
				il.Emit(OpCodes.Ldloc, strlenbytes);
				// call
				il.Emit(OpCodes.Call, typeof(Encoding).GetMethod("GetBytes", new[] { typeof(char*), typeof(int), typeof(byte*), typeof(int) }));
				// unused ret
				il.Emit(OpCodes.Pop);

				il.Emit(OpCodes.Br, end);

				il.MarkLabel(isNull);
				LoadConstant(il, IntPtr.Zero);
				il.Emit(OpCodes.Stloc, bytes);
				il.MarkLabel(end);

				return new ParameterLoadInfo
				{
					NativeType = typeof(IntPtr),
					EmitLoad = () =>
					{
						il.Emit(OpCodes.Ldloc, bytes);
					}
				};
			}

			if (type.IsClass)
			{
				// non ref of class can just be passed as pointer
				return new ParameterLoadInfo
				{
					NativeType = typeof(IntPtr),
					EmitLoad = () =>
					{
						var loc = il.DeclareLocal(type, true);
						var end = il.DefineLabel();
						var isNull = il.DefineLabel();

						il.Emit(OpCodes.Ldarg, (short)idx);
						il.Emit(OpCodes.Brfalse, isNull);

						il.Emit(OpCodes.Ldarg, (short)idx);
						il.Emit(OpCodes.Dup);
						il.Emit(OpCodes.Stloc, loc);
						il.Emit(OpCodes.Conv_I);
						// skip past the methodtable pointer to the first field
						il.Emit(OpCodes.Ldc_I4, ClassFieldOffset);
						il.Emit(OpCodes.Conv_I);
						il.Emit(OpCodes.Add);
						il.Emit(OpCodes.Br, end);

						il.MarkLabel(isNull);
						LoadConstant(il, IntPtr.Zero);
						il.MarkLabel(end);
					}
				};
			}

			if (type.IsPrimitive || type.IsEnum || type.IsPointer)
			{
				return new ParameterLoadInfo
				{
					NativeType = type,
					EmitLoad = () =>
					{
						il.Emit(OpCodes.Ldarg, (short)idx);
					}
				};
			}

			throw new NotImplementedException("Unrecognized parameter type!");
		}
	}

	/// <summary>Indicates that an abstract method is to be proxied by BizInvoker.</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class BizImportAttribute : Attribute
	{
		public CallingConvention CallingConvention { get; }

		/// <remarks>The annotated method's name is used iff <see langword="null"/>.</remarks>
		public string EntryPoint { get; set; }

		/// <summary><see langword="true"/> iff a compatibility interop should be used, which is slower but supports more argument types.</summary>
		public bool Compatibility { get; set; }

		public BizImportAttribute(CallingConvention c)
		{
			CallingConvention = c;
		}
	}
}
