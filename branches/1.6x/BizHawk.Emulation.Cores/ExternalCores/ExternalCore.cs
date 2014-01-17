using System;
using System.Security;
using System.Threading;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

namespace BizHawk
{

	
	/// <summary>
	/// universal interface to a shared library
	/// </summary>
	public interface ILibAccessor : IDisposable
	{
		IntPtr GetProcAddress(string name);
		bool IsOpen { get; }
	}

	/// <summary>
	/// universal access to an external emulator core
	/// </summary>
	public interface IExternalCoreAccessor : IDisposable
	{
		IntPtr Signal(string type, IntPtr obj, string param, IntPtr value);
		bool IsOpen { get; }
		void RegisterCore(ExternalCore core, bool register);
	}


	public class CoreAccessor : IExternalCoreAccessor
	{
		ILibAccessor mLibAccessor;
		public CoreAccessor(ILibAccessor accessor)
		{
			mLibAccessor = accessor;
			if (accessor.IsOpen)
			{
				mSignal = (SignalCallbackDelegate)Marshal.GetDelegateForFunctionPointer(accessor.GetProcAddress("Core_signal"), typeof(SignalCallbackDelegate));
				IsOpen = true;
			}
		}

		public void RegisterCore(ExternalCore core, bool register)
		{
			if (core is StaticCoreCommon) return;

			//defer initialization until the core is needed, to avoid pointless costs for cores the user isnt using
			if (!IsInitialized)
			{
				IsInitialized = true;
				scc = new StaticCoreCommon(this);
				scc.RegisterClientSignal(new SignalCallbackDelegate(ClientSignal));
				scc.Initialize();
			}

			if (register)
			{
				mCoreRegistry[core.ManagedOpaque] = core;
			}
			else
			{
				mCoreRegistry.Remove(core.ManagedOpaque);
			}
		}

		StaticCoreCommon scc;
		Dictionary<IntPtr, ExternalCore> mCoreRegistry = new Dictionary<IntPtr, ExternalCore>();

		public void Dispose()
		{
			if (mLibAccessor == null) return;
			scc.Dispose();
			mLibAccessor.Dispose();
			mLibAccessor = null;
			IsOpen = false;
		}

		public bool IsOpen { get; private set; }
		bool IsInitialized;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr SignalCallbackDelegate(string type, IntPtr obj, string param, IntPtr value);
		SignalCallbackDelegate mSignal;

		//external cores call into the client from here
		public IntPtr ClientSignal(string type, IntPtr obj, string param, IntPtr value)
		{
			//static calls
			if (obj == IntPtr.Zero)
			{
				return scc.ClientSignal(type, obj, param, value);
			}
			else
			{
				return mCoreRegistry[obj].ClientSignal(type, obj, param, value);
			}
		}

		public IntPtr Signal(string type, IntPtr obj, string param, IntPtr value)
		{
			if (!IsOpen) throw new InvalidOperationException("core accessor is open");
			return mSignal(type, obj, param, value);
		}
	}

	//todo - make abstract
	public class ExternalCore : IDisposable
	{
		//rename to managed and unmanaged
		public IntPtr ManagedOpaque;
		public IntPtr UnmanagedOpaque;
		static int _ManagedOpaque_Counter = 1;
		private static object oLock = new object();

		protected IExternalCoreAccessor mAccessor;
		public ExternalCore(IExternalCoreAccessor accessor)
		{
			mAccessor = accessor;
			lock (oLock)
			{
				ManagedOpaque = new IntPtr(_ManagedOpaque_Counter);
				_ManagedOpaque_Counter++;
			}
			mAccessor.RegisterCore(this, true);
		}

		public virtual void Dispose()
		{
			mAccessor.RegisterCore(this, false);
			
			//universal delete mechanism?
			//probably not.
		}

		/// <summary>
		/// cores call into the client from here. this system is not fully baked yet, though
		/// </summary>
		public virtual IntPtr ClientSignal(string type, IntPtr obj, string param, IntPtr value)
		{
			//if (type == "FACTORY")
			//{
			//    return new DiscInterface(mAccessor).UnmanagedOpaque;
			//}
			//else return IntPtr.Zero;
			return IntPtr.Zero;
		}

		/// <summary>
		/// merely emits an integer of the current system int size to an ILGenerator
		/// </summary>
		static void EmitIntPtr(ILGenerator gen, IntPtr val)
		{
			if (IntPtr.Size == 4) gen.Emit(OpCodes.Ldc_I4, val.ToInt32());
			else gen.Emit(OpCodes.Ldc_I8, val.ToInt64());
		}

		/// <summary>
		/// retrieves a function pointer from the core and returns it as the specified delegate type
		/// </summary>
		protected void QueryCoreCall<T>(out T del, string name)
		{
			del = QueryCoreCall<T>(name);
		}

		/// <summary>
		/// retrieves a function pointer from the core and returns it as the specified delegate type
		/// </summary>
		protected T QueryCoreCall<T>(string name)
		{
			MethodInfo mi = typeof(T).GetMethod("Invoke");
			ParameterInfo[] pis = mi.GetParameters();
			Type[] unmanagedParamTypes = new Type[pis.Length + 1];
			Type[] managedParamTypes = new Type[pis.Length];
			unmanagedParamTypes[0] = typeof(int);
			for (int i = 0; i < pis.Length; i++)
			{
				unmanagedParamTypes[i + 1] = pis[i].ParameterType;
				managedParamTypes[i] = pis[i].ParameterType;
			}

			IntPtr fptr = mAccessor.Signal("QUERY_FUNCTION", IntPtr.Zero, name, IntPtr.Zero);
			if (fptr == IntPtr.Zero)
				throw new InvalidOperationException("external core was missing requested function: " + name);

			DynamicMethod dm = new DynamicMethod("", mi.ReturnType, managedParamTypes, GetType().Module);
			ILGenerator gen = dm.GetILGenerator();
			EmitIntPtr(gen, UnmanagedOpaque);
			for (int i = 0; i < pis.Length; i++) gen.Emit(OpCodes.Ldarg, i);
			EmitIntPtr(gen, fptr);
			gen.EmitCalli(OpCodes.Calli, CallingConvention.ThisCall, mi.ReturnType, unmanagedParamTypes);
			gen.Emit(OpCodes.Ret);

			Delegate d = dm.CreateDelegate(typeof(T));
			return (T)(object)d;
		}

		/// <summary>
		/// exports a delegate as an IntPtr for use in unmanaged code and manages its life cycle to keep it from getting freed
		/// </summary>
		protected IntPtr ExportDelegate(Delegate d)
		{
			IntPtr retPtr = Marshal.GetFunctionPointerForDelegate(d);
			listLiveDelegates.Add(d);
			return retPtr;
		}

		//need to hold on to these callbacks to make sure they dont get GCed while unmanaged code has a pointer to them
		List<Delegate> listLiveDelegates = new List<Delegate>();
	}


	public class StaticCoreCommon : ExternalCore
	{
		//keep in mind that we may need to make the console thread safe if we ever do any zany multithreaded things

		public StaticCoreCommon(IExternalCoreAccessor accessor)
			: base(accessor)
		{
		}

		EmuFile Console;
		public void Initialize()
		{
			Console = new EmuFile(mAccessor);
			if (Log.HACK_LOG_STREAM != null)
				Console.BaseStream = Log.HACK_LOG_STREAM;
			else
				Console.BaseStream = System.Console.OpenStandardOutput();
			mAccessor.Signal("SET_CONSOLE", IntPtr.Zero, null, Console.UnmanagedOpaque);
		}

		public void RegisterClientSignal(CoreAccessor.SignalCallbackDelegate ClientSignal)
		{
			mAccessor.Signal("SET_CLIENT_SIGNAL", IntPtr.Zero, null, ExportDelegate(ClientSignal));
		}

		public override IntPtr ClientSignal(string type, IntPtr obj, string param, IntPtr value)
		{
			return IntPtr.Zero;
		}

	}



}