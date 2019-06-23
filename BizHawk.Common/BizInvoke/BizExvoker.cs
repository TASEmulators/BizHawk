using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.Common.BizInvoke
{
	public static class BizExvoker
	{
		/// <summary>
		/// the assembly that all delegate types are placed in
		/// </summary>
		private static readonly AssemblyBuilder ImplAssemblyBuilder;

		/// <summary>
		/// the module that all delegate types are placed in
		/// </summary>
		private static readonly ModuleBuilder ImplModuleBuilder;

		static BizExvoker()
		{
			var aname = new AssemblyName("BizExvokeProxyAssembly");
			ImplAssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(aname, AssemblyBuilderAccess.Run);
			ImplModuleBuilder = ImplAssemblyBuilder.DefineDynamicModule("BizExvokerModule");
		}

		/// <summary>
		/// holds the delegate types for a type
		/// </summary>
		private class DelegateStorage
		{
			/// <summary>
			/// the type that this storage was made for
			/// </summary>
			public Type OriginalType { get; }
			/// <summary>
			/// the type that the delegate types reside in
			/// </summary>
			public Type StorageType { get; }

			public List<StoredDelegateInfo> DelegateTypes { get; } = new List<StoredDelegateInfo>();

			public class StoredDelegateInfo
			{
				public MethodInfo Method { get; }
				public Type DelegateType { get; }
				public string EntryPointName { get; }
				public StoredDelegateInfo(MethodInfo method, Type delegateType, string entryPointName)
				{
					Method = method;
					DelegateType = delegateType;
					EntryPointName = entryPointName;
				}
			}

			public DelegateStorage(Type type)
			{
				var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
				.Select(m => new
				{
					Info = m,
					Attr = m.GetCustomAttributes(true).OfType<BizExportAttribute>().FirstOrDefault()
				})
				.Where(a => a.Attr != null);

				var typeBuilder = ImplModuleBuilder.DefineType($"Bizhawk.BizExvokeHolder{type.Name}", TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);

				foreach (var a in methods)
				{
					MethodBuilder unused;
					var delegateType = BizInvokeUtilities.CreateDelegateType(a.Info, a.Attr.CallingConvention, typeBuilder, out unused).CreateType();
					DelegateTypes.Add(new StoredDelegateInfo(a.Info, delegateType, a.Attr.EntryPoint ?? a.Info.Name));
				}
				StorageType = typeBuilder.CreateType();
				OriginalType = type;
			}
		}

		private class ExvokerImpl : IImportResolver
		{
			private readonly Dictionary<string, IntPtr> EntryPoints = new Dictionary<string, IntPtr>();

			private readonly List<Delegate> Delegates = new List<Delegate>();
			
			public ExvokerImpl(object o, DelegateStorage d, ICallingConventionAdapter a)
			{
				foreach (var sdt in d.DelegateTypes)
				{
					var del = Delegate.CreateDelegate(sdt.DelegateType, o, sdt.Method);
					Delegates.Add(del); // prevent garbage collection of the delegate, which would invalidate the pointer
					EntryPoints.Add(sdt.EntryPointName, a.GetFunctionPointerForDelegate(del));
				}
			}

			public IntPtr Resolve(string entryPoint)
			{
				IntPtr ret;
				EntryPoints.TryGetValue(entryPoint, out ret);
				return ret;
			}
		}

		static readonly Dictionary<Type, DelegateStorage> Impls = new Dictionary<Type, DelegateStorage>();


		public static IImportResolver GetExvoker(object o, ICallingConventionAdapter a)
		{
			DelegateStorage ds;
			lock (Impls)
			{
				var type = o.GetType();
				if (!Impls.TryGetValue(type, out ds))
				{
					ds = new DelegateStorage(type);
					Impls.Add(type, ds);
				}
			}

			return new ExvokerImpl(o, ds, a);
		}
	}

	/// <summary>
	/// mark an instance method to be exported by BizExvoker
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class BizExportAttribute : Attribute
	{
		public CallingConvention CallingConvention { get; }

		/// <summary>
		/// Gets or sets the name of entry point; if not given, the method's name is used
		/// </summary>
		public string EntryPoint { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="BizImportAttribute"/> class. 
		/// </summary>
		/// <param name="c">unmanaged calling convention</param>
		public BizExportAttribute(CallingConvention c)
		{
			CallingConvention = c;
		}
	}
}
