using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.BizInvoke;
using BizHawk.Common;

using NLua.Exceptions;

namespace NLua
{
	/// <summary>
	/// Lua state class, main interface to use Lua library.
	/// </summary>
	public class LuaState : IDisposable
	{
		private static readonly LuaNativeMethods NativeMethods;

		public static bool IsAvailable => NativeMethods != null;
		public static bool IsLua53 => NativeMethods is Lua53NativeMethods;

		static LuaState()
		{
			DynamicLibraryImportResolver resolver = null;

			// linux is tricky as we want to use the system lua library
			// but old (but not yet EOL) distros may not have lua 5.4
			// we can safely use lua 5.3 for our purposes, hope the
			// user's distro at least provides lua 5.3!
			if (OSTailoredCode.IsUnixHost)
			{
				bool TryLoad(string name)
				{
					try
					{
						resolver = new(name, hasLimitedLifetime: false);
						return true;
					}
					catch
					{
						return false;
					}
				}

				foreach (var (luaSo, isLua54) in new[]
				{
					("liblua.so.5.4.4", true),
					("liblua.so.5.4", true),
					("liblua5.4.so", true),
					("liblua5.4.so.0", true),
					("liblua.so.5.3.6", false),
					("liblua.so.5.3", false),
					("liblua5.3.so", false),
					("liblua5.3.so.0", false),
				})
				{
					if (TryLoad(luaSo))
					{
						resolver.UnixMakeGlobal(luaSo);
						NativeMethods = isLua54
							? BizInvoker.GetInvoker<Lua54NativeMethods>(resolver, CallingConventionAdapters.Native)
							: BizInvoker.GetInvoker<Lua53NativeMethods>(resolver, CallingConventionAdapters.Native);
						break;
					}
				}
			}
			else
			{
				// we provide the lua dll on windows
				// if this crashes the user's dll folder is probably fubar'd
				resolver = new("lua54.dll", hasLimitedLifetime: false);
				NativeMethods = BizInvoker.GetInvoker<Lua54NativeMethods>(resolver, CallingConventionAdapters.Native);
			}
		}

		private IntPtr _luaState;
		private readonly LuaState _mainState;

		/// <summary>
		/// Internal Lua handle pointer.
		/// </summary>
		public IntPtr Handle => _luaState;

		/// <summary>
		/// Encoding for the string conversions
		/// UTF8 by default.
		/// </summary>
		public Encoding Encoding { get; set; }

		/// <summary>
		/// Returns a pointer to a raw memory area associated with the given Lua state. The application can use this area for any purpose; Lua does not use it for anything.
		/// Each new thread has this area initialized with a copy of the area of the main thread. 
		/// </summary>
		/// <returns></returns>
		public IntPtr ExtraSpace => _luaState - IntPtr.Size;

		/// <summary>
		/// Get the main thread object, if the object is the main thread will be equal this
		/// </summary>
		public LuaState MainThread => _mainState ?? this;

		/// <summary>
		/// Initialize Lua state, and open the default libs
		/// </summary>
		/// <param name="openLibs">flag to enable/disable opening the default libs</param>
		public LuaState(bool openLibs = true)
		{
			Encoding = Encoding.UTF8;

			_luaState = NativeMethods.luaL_newstate();

			if (openLibs)
				OpenLibs();

			SetExtraObject(this, true);
		}

		/// <summary>
		/// Initialize Lua state with allocator function and user data value
		/// This method will NOT open the default libs.
		/// Creates a new thread running in a new, independent state. Returns NULL if it cannot create the thread or the state (due to lack of memory). The argument f is the allocator function; Lua does all memory allocation for this state through this function (see lua_Alloc). The second argument, ud, is an opaque pointer that Lua passes to the allocator in every call. 
		/// </summary>
		/// <param name="allocator">LuaAlloc allocator function called to alloc/free memory</param>
		/// <param name="ud">opaque pointer passed to allocator</param>
		public LuaState(LuaAlloc allocator, IntPtr ud)
		{
			Encoding = Encoding.UTF8;

			_luaState = NativeMethods.lua_newstate(allocator.ToFunctionPointer(), ud);

			SetExtraObject(this, true);
		}

		private LuaState(IntPtr luaThread, LuaState mainState)
		{
			_mainState = mainState;
			_luaState = luaThread;
			Encoding = mainState.Encoding;

			SetExtraObject(this, false);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Get the Lua object from IntPtr
		/// Useful for LuaFunction callbacks, if the Lua object was already collected will return null.
		/// </summary>
		/// <param name="luaState"></param>
		/// <returns></returns>
		public static LuaState FromIntPtr(IntPtr luaState)
		{
			if (luaState == IntPtr.Zero)
				return null;

			LuaState state = GetExtraObject<LuaState>(luaState);
			if (state != null && state._luaState == luaState)
				return state;

			return new LuaState(luaState, state.MainThread);
		}

		/// <summary>
		/// Finalizes an instance of the <see cref="LuaState"/> class.
		/// </summary>
		~LuaState()
		{
			Dispose(false);
		}

		/// <summary>
		/// Dispose lua state
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			Close();
		}

		/// <summary>
		/// Destroys all objects in the given Lua state (calling the corresponding garbage-collection metamethods, if any) and frees all dynamic memory used by this state
		/// </summary>
		public void Close()
		{
			if (_luaState == IntPtr.Zero || _mainState != null)
				return;

			NativeMethods.lua_close(_luaState);
			_luaState = IntPtr.Zero;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose the lua context (calling Close)
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}

		private void SetExtraObject<T>(T obj, bool weak) where T : class
		{
			var handle = GCHandle.Alloc(obj, weak ? GCHandleType.Weak : GCHandleType.Normal);
			IntPtr extraSpace = _luaState - IntPtr.Size;
			Marshal.WriteIntPtr(extraSpace, GCHandle.ToIntPtr(handle));
		}

		private static T GetExtraObject<T>(IntPtr luaState) where T : class
		{
			IntPtr extraSpace = luaState - IntPtr.Size;
			IntPtr pointer = Marshal.ReadIntPtr(extraSpace);
			var handle = GCHandle.FromIntPtr(pointer);
			if (!handle.IsAllocated)
				return null;

			return (T)handle.Target;
		}


		/// <summary>
		/// Converts the acceptable index idx into an equivalent absolute index (that is, one that does not depend on the stack top). 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public int AbsIndex(int index)
		{
			return NativeMethods.lua_absindex(_luaState, index);
		}
		/// <summary>
		/// Performs an arithmetic or bitwise operation over the two values (or one, in the case of negations) at the top of the stack, with the value at the top being the second operand, pops these values, and pushes the result of the operation. The function follows the semantics of the corresponding Lua operator (that is, it may call metamethods). 
		/// </summary>
		/// <param name="operation"></param>
		public void Arith(LuaOperation operation)
		{
			NativeMethods.lua_arith(_luaState, (int)operation);
		}

		/// <summary>
		/// Sets a new panic function and returns the old one
		/// </summary>
		/// <param name="panicFunction"></param>
		/// <returns></returns>
		public LuaNativeFunction AtPanic(LuaNativeFunction panicFunction)
		{
			IntPtr newPanicPtr = panicFunction.ToFunctionPointer();
			return NativeMethods.lua_atpanic(_luaState, newPanicPtr).ToLuaFunction();
		}

		/// <summary>
		/// Calls a function. 
		/// To call a function you must use the following protocol: first, the function to be called is pushed onto the stack; then, the arguments to the function are pushed in direct order;
		/// that is, the first argument is pushed first. Finally you call lua_call; nargs is the number of arguments that you pushed onto the stack.
		/// All arguments and the function value are popped from the stack when the function is called. The function results are pushed onto the stack when the function returns.
		/// The number of results is adjusted to nresults, unless nresults is LUA_MULTRET. In this case, all results from the function are pushed;
		/// Lua takes care that the returned values fit into the stack space, but it does not ensure any extra space in the stack. The function results are pushed onto the stack in direct order (the first result is pushed first), so that after the call the last result is on the top of the stack. 
		/// </summary>
		/// <param name="arguments"></param>
		/// <param name="results"></param>
		public void Call(int arguments, int results)
		{
			NativeMethods.lua_callk(_luaState, arguments, results, IntPtr.Zero, IntPtr.Zero);
		}

		/// <summary>
		/// This function behaves exactly like lua_call, but allows the called function to yield 
		/// </summary>
		/// <param name="arguments"></param>
		/// <param name="results"></param>
		/// <param name="context"></param>
		/// <param name="continuation"></param>
		public void CallK(int arguments, int results, int context, LuaKFunction continuation)
		{
			IntPtr k = continuation.ToFunctionPointer();
			NativeMethods.lua_callk(_luaState, arguments, results, (IntPtr)context, k);
		}

		/// <summary>
		/// Ensures that the stack has space for at least n extra slots (that is, that you can safely push up to n values into it). It returns false if it cannot fulfill the request,
		/// </summary>
		/// <param name="nExtraSlots"></param>
		public bool CheckStack(int nExtraSlots)
		{
			return NativeMethods.lua_checkstack(_luaState, nExtraSlots) != 0;
		}

		/// <summary>
		/// Compares two Lua values. Returns 1 if the value at index index1 satisfies op when compared with the value at index index2
		/// </summary>
		/// <param name="index1"></param>
		/// <param name="index2"></param>
		/// <param name="comparison"></param>
		/// <returns></returns>
		public bool Compare(int index1, int index2, LuaCompare comparison)
		{
			return NativeMethods.lua_compare(_luaState, index1, index2, (int)comparison) != 0;
		}

		/// <summary>
		/// Concatenates the n values at the top of the stack, pops them, and leaves the result at the top. If n is 1, the result is the single value on the stack (that is, the function does nothing);
		/// </summary>
		/// <param name="n"></param>
		public void Concat(int n)
		{
			NativeMethods.lua_concat(_luaState, n);
		}
		/// <summary>
		/// Copies the element at index fromidx into the valid index toidx, replacing the value at that position
		/// </summary>
		/// <param name="fromIndex"></param>
		/// <param name="toIndex"></param>
		public void Copy(int fromIndex, int toIndex)
		{
			NativeMethods.lua_copy(_luaState, fromIndex, toIndex);
		}

		/// <summary>
		/// Creates a new empty table and pushes it onto the stack. Parameter narr is a hint for how many elements the table will have as a sequence; parameter nrec is a hint for how many other elements the table will have
		/// </summary>
		/// <param name="elements"></param>
		/// <param name="records"></param>
		public void CreateTable(int elements, int records)
		{
			NativeMethods.lua_createtable(_luaState, elements, records);
		}

		/// <summary>
		/// Dumps a function as a binary chunk. Receives a Lua function on the top of the stack and produces a binary chunk that, if loaded again, results in a function equivalent to the one dumped
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="data"></param>
		/// <param name="stripDebug"></param>
		/// <returns></returns>
		public int Dump(LuaWriter writer, IntPtr data, bool stripDebug)
		{
			return NativeMethods.lua_dump(_luaState, writer.ToFunctionPointer(), data, stripDebug ? 1 : 0);
		}

		/// <summary>
		/// Generates a Lua error, using the value at the top of the stack as the error object. This function does a long jump
		/// (We want it to be inlined to avoid issues with managed stack)
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Error()
		{
			return NativeMethods.lua_error(_luaState);
		}

		/// <summary>
		/// Returns the memory-allocation function of a given state. If ud is not NULL, Lua stores in *ud the opaque pointer given when the memory-allocator function was set. 
		/// </summary>
		/// <param name="ud"></param>
		/// <returns></returns>
		public LuaAlloc GetAllocFunction(ref IntPtr ud)
		{
			return NativeMethods.lua_getallocf(_luaState, ref ud).ToLuaAlloc();
		}

		/// <summary>
		/// Pushes onto the stack the value t[k], where t is the value at the given index. As in Lua, this function may trigger a metamethod for the "index" event (see §2.4).
		/// Returns the type of the pushed value. 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public LuaType GetField(int index, string key)
		{
			return (LuaType)NativeMethods.lua_getfield(_luaState, index, key);
		}

		/// <summary>
		/// Pushes onto the stack the value t[k], where t is the value at the given index. As in Lua, this function may trigger a metamethod for the "index" event (see §2.4).
		/// Returns the type of the pushed value. 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public LuaType GetField(LuaRegistry index, string key)
		{
			return (LuaType)NativeMethods.lua_getfield(_luaState, (int)index, key);
		}

		/// <summary>
		/// Pushes onto the stack the value of the global name. Returns the type of that value
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public LuaType GetGlobal(string name)
		{
			return (LuaType)NativeMethods.lua_getglobal(_luaState, name);
		}

		/// <summary>
		/// Pushes onto the stack the value t[i], where t is the value at the given index
		/// </summary>
		/// <param name="index"></param>
		/// <param name="i"></param>
		/// <returns> Returns the type of the pushed value</returns>
		public LuaType GetInteger(int index, long i)
		{
			return (LuaType)NativeMethods.lua_geti(_luaState, index, i);
		}

		/// <summary>
		/// If the value at the given index has a metatable, the function pushes that metatable onto the stack and returns 1
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool GetMetaTable(int index)
		{
			return NativeMethods.lua_getmetatable(_luaState, index) != 0;
		}

		/// <summary>
		/// Pushes onto the stack the value t[k], where t is the value at the given index and k is the value at the top of the stack. 
		/// </summary>
		/// <param name="index"></param>
		/// <returns>Returns the type of the pushed value</returns>
		public LuaType GetTable(int index)
		{
			return (LuaType)NativeMethods.lua_gettable(_luaState, index);
		}

		/// <summary>
		/// Pushes onto the stack the value t[k], where t is the value at the given index and k is the value at the top of the stack. 
		/// </summary>
		/// <param name="index"></param>
		/// <returns>Returns the type of the pushed value</returns>
		public LuaType GetTable(LuaRegistry index)
		{
			return (LuaType)NativeMethods.lua_gettable(_luaState, (int)index);
		}


		/// <summary>
		/// Returns the index of the top element in the stack. 0 means an empty stack.
		/// </summary>
		/// <returns>Returns the index of the top element in the stack.</returns>
		public int GetTop() => NativeMethods.lua_gettop(_luaState);

		/// <summary>
		/// Pushes onto the stack the user value associated with the full userdata at the given index and returns the type of the pushed value.
		/// If the userdata does not have that value, pushes nil and returns LUA_TNONE.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public int GetUserValue(int index)
			=> NativeMethods switch
			{
				Lua54NativeMethods lua54 => lua54.lua_getiuservalue(_luaState, index, 1),
				Lua53NativeMethods lua53 => lua53.lua_getuservalue(_luaState, index),
				null => throw new LuaException($"{nameof(NativeMethods)} is null?"),
				_ => throw new InvalidOperationException()
			};

		/// <summary>
		/// Gets information about the n-th upvalue of the closure at index funcindex. It pushes the upvalue's value onto the stack and returns its name. Returns NULL (and pushes nothing) when the index n is greater than the number of upvalues.
		/// For C functions, this function uses the empty string "" as a name for all upvalues. (For Lua functions, upvalues are the external local variables that the function uses, and that are consequently included in its closure.)
		/// Upvalues have no particular order, as they are active through the whole function. They are numbered in an arbitrary order. 
		/// </summary>
		/// <param name="functionIndex"></param>
		/// <param name="n"></param>
		/// <returns>Returns the type of the pushed value. </returns>
		public string GetUpValue(int functionIndex, int n)
		{
			IntPtr ptr = NativeMethods.lua_getupvalue(_luaState, functionIndex, n);
			return Marshal.PtrToStringAnsi(ptr);
		}

		/// <summary>
		/// Moves the top element into the given valid index, shifting up the elements above this index to open space. This function cannot be called with a pseudo-index, because a pseudo-index is not an actual stack position. 
		/// </summary>
		/// <param name="index"></param>
		public void Insert(int index) => NativeMethods.lua_rotate(_luaState, index, 1);

		/// <summary>
		/// Returns if the value at the given index is a boolean
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsBoolean(int index) => Type(index) == LuaType.Boolean;

		/// <summary>
		/// Returns if the value at the given index is a C(#) function
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsCFunction(int index) => NativeMethods.lua_iscfunction(_luaState, index) != 0;

		/// <summary>
		/// Returns if the value at the given index is a function
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsFunction(int index) => Type(index) == LuaType.Function;

		/// <summary>
		/// Returns if the value at the given index is an integer
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsInteger(int index) => NativeMethods.lua_isinteger(_luaState, index) != 0;

		/// <summary>
		/// Returns if the value at the given index is light user data
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsLightUserData(int index) => Type(index) == LuaType.LightUserData;

		/// <summary>
		/// Returns if the value at the given index is nil
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsNil(int index) => Type(index) == LuaType.Nil;

		/// <summary>
		/// Returns if the value at the given index is none
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsNone(int index) => Type(index) == LuaType.None;

		/// <summary>
		/// Check if the value at the index is none or nil
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsNoneOrNil(int index) => IsNone(index) || IsNil(index);

		/// <summary>
		/// Returns if the value at the given index is a number
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsNumber(int index) => NativeMethods.lua_isnumber(_luaState, index) != 0;

		/// <summary>
		/// Returns if the value at the given index is a string or a number (which is always convertible to a string)
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsStringOrNumber(int index)
		{
			return NativeMethods.lua_isstring(_luaState, index) != 0;
		}

		/// <summary>
		/// Returns if the value at the given index is a string
		/// NOTE: This is different from the lua_isstring, which return true if the value is a number
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsString(int index) => Type(index) == LuaType.String;

		/// <summary>
		/// Returns if the value at the given index is a table. 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsTable(int index) => Type(index) == LuaType.Table;

		/// <summary>
		/// Returns if the value at the given index is a thread. 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsThread(int index) => Type(index) == LuaType.Thread;

		/// <summary>
		/// Returns if the value at the given index is a user data. 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsUserData(int index) => NativeMethods.lua_isuserdata(_luaState, index) != 0;

		/// <summary>
		/// Returns if the given coroutine can yield, and 0 otherwise
		/// </summary>
		public bool IsYieldable => NativeMethods.lua_isyieldable(_luaState) != 0;

		/// <summary>
		/// Push the length of the value at the given index on the stack. It is equivalent to the '#' operator in Lua (see §3.4.7) and may trigger a metamethod for the "length" event (see §2.4). The result is pushed on the stack. 
		/// </summary>
		/// <param name="index"></param>
		public void PushLength(int index) => NativeMethods.lua_len(_luaState, index);

		/// <summary>
		/// Loads a Lua chunk without running it. If there are no errors, lua_load pushes the compiled chunk as a Lua function on top of the stack. Otherwise, it pushes an error message. 
		/// The lua_load function uses a user-supplied reader function to read the chunk (see lua_Reader). The data argument is an opaque value passed to the reader function. 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="data"></param>
		/// <param name="chunkName"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public LuaStatus Load
			(LuaReader reader,
				IntPtr data,
				string chunkName,
				string mode)
		{
			return (LuaStatus)NativeMethods.lua_load(_luaState,
														reader.ToFunctionPointer(),
														data,
														chunkName,
														mode);
		}

		/// <summary>
		/// Creates a new empty table and pushes it onto the stack
		/// </summary>
		public void NewTable() => NativeMethods.lua_createtable(_luaState, 0, 0);

		/// <summary>
		/// Creates a new thread, pushes it on the stack, and returns a pointer to a lua_State that represents this new thread. The new thread returned by this function shares with the original thread its global environment, but has an independent execution stack. 
		/// </summary>
		/// <returns></returns>
		public LuaState NewThread()
		{
			IntPtr thread = NativeMethods.lua_newthread(_luaState);
			return new LuaState(thread, this);
		}

		/// <summary>
		/// This function creates and pushes on the stack a new full userdata,
		/// with nuvalue associated Lua values, called user values, plus an
		/// associated block of raw memory with size bytes. (The user values
		/// can be set and read with the functions lua_setiuservalue and lua_getiuservalue.)
		/// The function returns the address of the block of memory.
		/// </summary>
		public IntPtr NewUserData(int size)
		{
			return NativeMethods switch
			{
				Lua54NativeMethods lua54 => lua54.lua_newuserdatauv(_luaState, (UIntPtr)size, 1),
				Lua53NativeMethods lua53 => lua53.lua_newuserdata(_luaState, (UIntPtr)size),
				null => throw new LuaException($"{nameof(NativeMethods)} is null?"),
				_ => throw new InvalidOperationException()
			};
		}

		/// <summary>
		/// Pops a key from the stack, and pushes a key–value pair from the table at the given index (the "next" pair after the given key).
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool Next(int index) => NativeMethods.lua_next(_luaState, index) != 0;

		/// <summary>
		/// Calls a function in protected mode. 
		/// </summary>
		/// <param name="arguments"></param>
		/// <param name="results"></param>
		/// <param name="errorFunctionIndex"></param>
		public LuaStatus PCall(int arguments, int results, int errorFunctionIndex)
		{
			return (LuaStatus)NativeMethods.lua_pcallk(_luaState, arguments, results, errorFunctionIndex, IntPtr.Zero, IntPtr.Zero);
		}

		/// <summary>
		/// This function behaves exactly like lua_pcall, but allows the called function to yield
		/// </summary>
		/// <param name="arguments"></param>
		/// <param name="results"></param>
		/// <param name="errorFunctionIndex"></param>
		/// <param name="context"></param>
		/// <param name="k"></param>
		public LuaStatus PCallK(int arguments,
			int results,
			int errorFunctionIndex,
			int context,
			LuaKFunction k)
		{
			return (LuaStatus)NativeMethods.lua_pcallk(_luaState,
				arguments,
				results,
				errorFunctionIndex,
				(IntPtr)context,
				k.ToFunctionPointer());
		}

		/// <summary>
		/// Pops n elements from the stack. 
		/// </summary>
		/// <param name="n"></param>
		public void Pop(int n) => NativeMethods.lua_settop(_luaState, -n - 1);

		/// <summary>
		/// Pushes a boolean value with value b onto the stack. 
		/// </summary>
		/// <param name="b"></param>
		public void PushBoolean(bool b) => NativeMethods.lua_pushboolean(_luaState, b ? 1 : 0);

		/// <summary>
		/// Pushes a new C closure onto the stack. When a C function is created, it is possible to associate 
		/// some values with it, thus creating a C closure (see §4.4); these values are then accessible to the function 
		/// whenever it is called. To associate values with a C function, first these values must be pushed onto the 
		/// stack (when there are multiple values, the first value is pushed first). 
		/// Then lua_pushcclosure is called to create and push the C function onto the stack, 
		/// with the argument n telling how many values will be associated with the function. 
		/// lua_pushcclosure also pops these values from the stack. 
		/// </summary>
		/// <param name="function"></param>
		/// <param name="n"></param>
		public void PushCClosure(LuaNativeFunction function, int n)
		{
			NativeMethods.lua_pushcclosure(_luaState, function.ToFunctionPointer(), n);
		}

		/// <summary>
		/// Pushes a C function onto the stack. This function receives a pointer to a C function and pushes onto the stack a Lua value of type function that, when called, invokes the corresponding C function. 
		/// </summary>
		/// <param name="function"></param>
		public void PushCFunction(LuaNativeFunction function)
		{
			PushCClosure(function, 0);
		}

		/// <summary>
		/// Pushes the global environment onto the stack. 
		/// </summary>
		public void PushGlobalTable()
		{
			_ = NativeMethods.lua_rawgeti(_luaState, (int)LuaRegistry.Index, (int)LuaRegistryIndex.Globals);
		}

		/// <summary>
		/// Pushes an integer with value n onto the stack. 
		/// </summary>
		/// <param name="n"></param>
		public void PushInteger(long n) => NativeMethods.lua_pushinteger(_luaState, n);

		/// <summary>
		/// Pushes a light userdata onto the stack.
		/// Userdata represent C values in Lua. A light userdata represents a pointer, a void*. It is a value (like a number): you do not create it, it has no individual metatable, and it is not collected (as it was never created). A light userdata is equal to "any" light userdata with the same C address. 
		/// </summary>
		/// <param name="data"></param>
		public void PushLightUserData(IntPtr data)
		{
			NativeMethods.lua_pushlightuserdata(_luaState, data);
		}

		/// <summary>
		/// Pushes a reference data (C# object)  onto the stack. 
		/// This function uses lua_pushlightuserdata, but uses a GCHandle to store the reference inside the Lua side.
		/// The CGHandle is create as Normal, and will be freed when the value is pop
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		public void PushObject<T>(T obj)
		{
			if (obj == null)
			{
				PushNil();
				return;
			}

			var handle = GCHandle.Alloc(obj);
			PushLightUserData(GCHandle.ToIntPtr(handle));
		}


		/// <summary>
		/// Pushes binary buffer onto the stack (usually UTF encoded string) or any arbitraty binary data
		/// </summary>
		/// <param name="buffer"></param>
		public void PushBuffer(byte[] buffer)
		{
			if (buffer == null)
			{
				PushNil();
				return;
			}

			NativeMethods.lua_pushlstring(_luaState, buffer, (UIntPtr)buffer.Length);
		}

		/// <summary>
		/// Pushes a string onto the stack
		/// </summary>
		/// <param name="value"></param>
		public void PushString(string value)
		{
			if (value == null)
			{
				PushNil();
				return;
			}

			byte[] buffer = Encoding.GetBytes(value);
			PushBuffer(buffer);
		}

		/// <summary>
		/// Push a instring using string.Format 
		/// PushString("Foo {0}", 10);
		/// </summary>
		/// <param name="value"></param>
		/// <param name="args"></param>
		public void PushString(string value, params object[] args)
		{
			PushString(string.Format(value, args));
		}

		/// <summary>
		/// Pushes a nil value onto the stack. 
		/// </summary>
		public void PushNil() => NativeMethods.lua_pushnil(_luaState);

		/// <summary>
		/// Pushes a double with value n onto the stack. 
		/// </summary>
		/// <param name="number"></param>
		public void PushNumber(double number) => NativeMethods.lua_pushnumber(_luaState, number);


		/// <summary>
		/// Pushes the thread represented by L onto the stack. Returns true if this thread is the main thread of its state. 
		/// </summary>
		/// <returns></returns>
		public bool PushThread()
		{
			return NativeMethods.lua_pushthread(_luaState) == 1;
		}

		/// <summary>
		/// Pushes a copy of the element at the given index onto the stack. (lua_pushvalue)
		/// The method was renamed, since pushvalue is a bit vague
		/// </summary>
		/// <param name="index"></param>
		public void PushCopy(int index)
		{
			NativeMethods.lua_pushvalue(_luaState, index);
		}

		/// <summary>
		/// Returns true if the two values in indices index1 and index2 are primitively equal (that is, without calling the __eq metamethod). Otherwise returns false. Also returns false if any of the indices are not valid. 
		/// </summary>
		/// <param name="index1"></param>
		/// <param name="index2"></param>
		/// <returns></returns>
		public bool RawEqual(int index1, int index2)
		{
			return NativeMethods.lua_rawequal(_luaState, index1, index2) != 0;
		}

		/// <summary>
		/// Similar to GetTable, but does a raw access (i.e., without metamethods). 
		/// </summary>
		/// <param name="index"></param>
		/// <returns>Returns the type of the pushed value</returns>
		public LuaType RawGet(int index)
		{
			return (LuaType)NativeMethods.lua_rawget(_luaState, index);
		}

		/// <summary>
		/// Similar to GetTable, but does a raw access (i.e., without metamethods). 
		/// </summary>
		/// <param name="index"></param>
		/// <returns>Returns the type of the pushed value</returns>
		public LuaType RawGet(LuaRegistry index)
		{
			return (LuaType)NativeMethods.lua_rawget(_luaState, (int)index);
		}

		/// <summary>
		/// Pushes onto the stack the value t[n], where t is the table at the given index. The access is raw, that is, it does not invoke the __index metamethod. 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="n"></param>
		/// <returns>Returns the type of the pushed value</returns>
		public LuaType RawGetInteger(int index, long n)
		{
			return (LuaType)NativeMethods.lua_rawgeti(_luaState, index, n);
		}

		/// <summary>
		/// Pushes onto the stack the value t[n], where t is the table at the given index. The access is raw, that is, it does not invoke the __index metamethod. 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public LuaType RawGetInteger(LuaRegistry index, long n)
		{
			return (LuaType)NativeMethods.lua_rawgeti(_luaState, (int)index, n);
		}


		/// <summary>
		/// Pushes onto the stack the value t[k], where t is the table at the given index and k is the pointer p represented as a light userdata. The access is raw; that is, it does not invoke the __index metamethod. 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="obj"></param>
		/// <returns>Returns the type of the pushed value. </returns>
		public LuaType RawGetByHashCode(int index, object obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "obj shouldn't be null");

			return (LuaType)NativeMethods.lua_rawgetp(_luaState, index, (IntPtr)obj.GetHashCode());
		}

		/// <summary>
		/// Returns the raw "length" of the value at the given index: for strings, this is the string length; for tables, this is the result of the length operator ('#') with no metamethods; for userdata, this is the size of the block of memory allocated for the userdata; for other values, it is 0. 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public int RawLen(int index)
		{
			return (int)NativeMethods.lua_rawlen(_luaState, index);
		}

		/// <summary>
		/// Similar to lua_settable, but does a raw assignment (i.e., without metamethods).
		/// </summary>
		/// <param name="index"></param>
		public void RawSet(int index)
		{
			NativeMethods.lua_rawset(_luaState, index);
		}

		/// <summary>
		/// Similar to lua_settable, but does a raw assignment (i.e., without metamethods).
		/// </summary>
		/// <param name="index"></param>
		public void RawSet(LuaRegistry index)
		{
			NativeMethods.lua_rawset(_luaState, (int)index);
		}

		/// <summary>
		/// Does the equivalent of t[i] = v, where t is the table at the given index and v is the value at the top of the stack.
		/// This function pops the value from the stack. The assignment is raw, that is, it does not invoke the __newindex metamethod. 
		/// </summary>
		/// <param name="index">index of table</param>
		/// <param name="i">value</param>
		public void RawSetInteger(int index, long i)
		{
			NativeMethods.lua_rawseti(_luaState, index, i);
		}

		/// <summary>
		/// Does the equivalent of t[i] = v, where t is the table at the given index and v is the value at the top of the stack.
		/// This function pops the value from the stack. The assignment is raw, that is, it does not invoke the __newindex metamethod. 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="i"></param>
		public void RawSetInteger(LuaRegistry index, long i)
		{
			NativeMethods.lua_rawseti(_luaState, (int)index, i);
		}


		/// <summary>
		/// Does the equivalent of t[p] = v, where t is the table at the given index, p is encoded as a light userdata, and v is the value at the top of the stack. 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="obj"></param>
		public void RawSetByHashCode(int index, object obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj), "obj shouldn't be null");

			NativeMethods.lua_rawsetp(_luaState, index, (IntPtr)obj.GetHashCode());
		}

		/// <summary>
		/// Sets the C# delegate f as the new value of global name
		/// </summary>
		/// <param name="name"></param>
		/// <param name="function"></param>
		public void Register(string name, LuaNativeFunction function)
		{
			PushCFunction(function);
			SetGlobal(name);
		}


		/// <summary>
		/// Removes the element at the given valid index, shifting down the elements above this index to fill the gap. This function cannot be called with a pseudo-index, because a pseudo-index is not an actual stack position. 
		/// </summary>
		/// <param name="index"></param>
		public void Remove(int index)
		{
			Rotate(index, -1);
			Pop(1);
		}

		/// <summary>
		/// Moves the top element into the given valid index without shifting any element (therefore replacing the value at that given index), and then pops the top element.
		/// </summary>
		/// <param name="index"></param>
		public void Replace(int index)
		{
			Copy(-1, index);
			Pop(1);
		}

		/// <summary>
		/// Starts and resumes a coroutine in the given thread L.
		/// To start a coroutine, you push onto the thread stack
		/// the main function plus any arguments; then you call lua_resume,
		/// with nargs being the number of arguments.This call returns when
		/// the coroutine suspends or finishes its execution.
		/// lua_resume returns LUA_YIELD if the coroutine yields,
		/// LUA_OK if the coroutine finishes its execution without errors,
		/// or an error code in case of errors (see lua_pcall).
		/// In case of errors, the error object is on the top of the stack.
		/// </summary>
		public LuaStatus Resume(LuaState from, int arguments)
			=> NativeMethods switch
			{
				Lua54NativeMethods lua54 => (LuaStatus)lua54.lua_resume(_luaState, from?._luaState ?? IntPtr.Zero, arguments, out _),
				Lua53NativeMethods lua53 => (LuaStatus)lua53.lua_resume(_luaState, from?._luaState ?? IntPtr.Zero, arguments),
				null => throw new LuaException($"{nameof(NativeMethods)} is null?"),
				_ => throw new InvalidOperationException()
			};

		/// <summary>
		/// Rotates the stack elements between the valid index idx and the top of the stack. The elements are rotated n positions in the direction of the top, for a positive n, or -n positions in the direction of the bottom, for a negative n. The absolute value of n must not be greater than the size of the slice being rotated. This function cannot be called with a pseudo-index, because a pseudo-index is not an actual stack position. 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="n"></param>
		public void Rotate(int index, int n)
		{
			NativeMethods.lua_rotate(_luaState, index, n);
		}

		/// <summary>
		/// Changes the allocator function of a given state to f with user data ud. 
		/// </summary>
		/// <param name="alloc"></param>
		/// <param name="ud"></param>
		public void SetAllocFunction(LuaAlloc alloc, ref IntPtr ud)
		{
			NativeMethods.lua_setallocf(_luaState, alloc.ToFunctionPointer(), ud);
		}

		/// <summary>
		/// Does the equivalent to t[k] = v, where t is the value at the given index and v is the value at the top of the stack.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="key"></param>
		public void SetField(int index, string key)
		{
			NativeMethods.lua_setfield(_luaState, index, key);
		}

		/// <summary>
		/// Pops a value from the stack and sets it as the new value of global name. 
		/// </summary>
		/// <param name="name"></param>
		public void SetGlobal(string name)
		{
			NativeMethods.lua_setglobal(_luaState, name);
		}

		/// <summary>
		/// Does the equivalent to t[n] = v, where t is the value at the given index and v is the value at the top of the stack. 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="n"></param>
		public void SetInteger(int index, long n)
		{
			NativeMethods.lua_seti(_luaState, index, n);
		}

		/// <summary>
		/// Pops a table from the stack and sets it as the new metatable for the value at the given index. 
		/// </summary>
		/// <param name="index"></param>
		public void SetMetaTable(int index)
		{
			NativeMethods.lua_setmetatable(_luaState, index);
		}

		/// <summary>
		/// Does the equivalent to t[k] = v, where t is the value at the given index, v is the value at the top of the stack, and k is the value just below the top
		/// </summary>
		/// <param name="index"></param>
		public void SetTable(int index)
		{
			NativeMethods.lua_settable(_luaState, index);
		}

		/// <summary>
		/// Accepts any index, or 0, and sets the stack top to this index. If the new top is larger than the old one, then the new elements are filled with nil. If index is 0, then all stack elements are removed. 
		/// </summary>
		/// <param name="newTop"></param>
		public void SetTop(int newTop)
		{
			NativeMethods.lua_settop(_luaState, newTop);
		}

		/// <summary>
		/// Sets the value of a closure's upvalue. It assigns the value at the top of the stack to the upvalue and returns its name. It also pops the value from the stack. 
		/// </summary>
		/// <param name="functionIndex"></param>
		/// <param name="n"></param>
		/// <returns>Returns NULL (and pops nothing) when the index n is greater than the number of upvalues. </returns>
		public string SetUpValue(int functionIndex, int n)
		{
			IntPtr ptr = NativeMethods.lua_setupvalue(_luaState, functionIndex, n);
			return Marshal.PtrToStringAnsi(ptr);
		}

		/// <summary>
		/// Pops a value from the stack and sets it as the new user value associated to the full userdata at the given index. Returns 0 if the userdata does not have that value. 
		/// </summary>
		/// <param name="index"></param>
		public void SetUserValue(int index)
		{
			switch (NativeMethods)
			{
				case Lua54NativeMethods lua54: lua54.lua_setiuservalue(_luaState, index, 1); break;
				case Lua53NativeMethods lua53: lua53.lua_setuservalue(_luaState, index); break;
			}

			throw new LuaException($"{nameof(NativeMethods)} is null?");
		}

		/// <summary>
		/// The status can be 0 (LUA_OK) for a normal thread, an error code if the thread finished the execution of a lua_resume with an error, or LUA_YIELD if the thread is suspended. 
		/// You can only call functions in threads with status LUA_OK. You can resume threads with status LUA_OK (to start a new coroutine) or LUA_YIELD (to resume a coroutine). 
		/// </summary>
		public LuaStatus Status => (LuaStatus)NativeMethods.lua_status(_luaState);

		/// <summary>
		/// Converts the zero-terminated string s to a number, pushes that number into the stack,
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public bool StringToNumber(string s)
		{
			return NativeMethods.lua_stringtonumber(_luaState, s) != UIntPtr.Zero;
		}

		/// <summary>
		/// Converts the Lua value at the given index to a C# boolean value
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool ToBoolean(int index)
		{
			return NativeMethods.lua_toboolean(_luaState, index) != 0;
		}

		/// <summary>
		/// Converts a value at the given index to a C# function. That value must be a C# function; otherwise, returns NULL
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public LuaNativeFunction ToCFunction(int index)
		{
			return NativeMethods.lua_tocfunction(_luaState, index).ToLuaFunction();
		}

		/// <summary>
		/// Converts the Lua value at the given index to the signed integral type lua_Integer. The Lua value must be an integer, or a number or string convertible to an integer (see §3.4.3); otherwise, lua_tointegerx returns 0. 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public long ToInteger(int index)
		{
			return NativeMethods.lua_tointegerx(_luaState, index, out _);
		}

		/// <summary>
		/// Converts the Lua value at the given index to the signed integral type lua_Integer. The Lua value must be an integer, or a number or string convertible to an integer (see §3.4.3); otherwise, lua_tointegerx returns 0. 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public long? ToIntegerX(int index)
		{
			long value = NativeMethods.lua_tointegerx(_luaState, index, out var isInteger);
			if (isInteger != 0)
				return value;
			return null;
		}

		/// <summary>
		/// Converts the Lua value at the given as byte array
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public byte[] ToBuffer(int index)
		{
			return ToBuffer(index, true);
		}
		/// <summary>
		/// Converts the Lua value at the given index to a byte array.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="callMetamethod">Calls __tostring field if present</param>
		/// <returns></returns>
		public byte[] ToBuffer(int index, bool callMetamethod)
		{
			UIntPtr len;
			IntPtr buff;

			if (callMetamethod)
			{
				buff = NativeMethods.luaL_tolstring(_luaState, index, out len);
				Pop(1);
			}
			else
			{
				buff = NativeMethods.lua_tolstring(_luaState, index, out len);
			}

			if (buff == IntPtr.Zero)
				return null;

			int length = (int)len;
			if (length == 0)
				return Array.Empty<byte>();

			byte[] output = new byte[length];
			Marshal.Copy(buff, output, 0, length);
			return output;
		}

		/// <summary>
		/// Converts the Lua value at the given index to a C# string
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public string ToString(int index)
		{
			return ToString(index, true);
		}
		/// <summary>
		/// Converts the Lua value at the given index to a C# string
		/// </summary>
		/// <param name="index"></param>
		/// <param name="callMetamethod">Calls __tostring field if present</param>
		/// <returns></returns>
		public string ToString(int index, bool callMetamethod)
		{
			byte[] buffer = ToBuffer(index, callMetamethod);
			if (buffer == null)
				return null;
			return Encoding.GetString(buffer);
		}

		/// <summary>
		/// Converts the Lua value at the given index to a C# double
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public double ToNumber(int index)
		{
			return NativeMethods.lua_tonumberx(_luaState, index, out _);
		}

		/// <summary>
		/// Converts the Lua value at the given index to a C# double?
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public double? ToNumberX(int index)
		{
			double value = NativeMethods.lua_tonumberx(_luaState, index, out var isNumber);
			if (isNumber != 0)
				return value;
			return null;
		}

		/// <summary>
		/// Converts the value at the given index to a generic C pointer (void*). The value can be a userdata, a table, a thread, or a function; otherwise, lua_topointer returns NULL. Different objects will give different pointers. There is no way to convert the pointer back to its original value.
		/// Typically this function is used only for hashing and debug information. 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public IntPtr ToPointer(int index)
		{
			return NativeMethods.lua_topointer(_luaState, index);
		}


		/// <summary>
		/// Converts the value at the given index to a Lua thread
		/// or return the self if is the main thread
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public LuaState ToThread(int index)
		{
			IntPtr state = NativeMethods.lua_tothread(_luaState, index);
			if (state == _luaState)
				return this;

			return FromIntPtr(state);
		}

		/// <summary>
		/// Return an object (refence) at the index
		/// Important if a object was push the object need to fetched using
		/// this method, otherwise the C# object will never be collected
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="index"></param>
		/// <param name="freeGCHandle">True to free the GCHandle</param>
		/// <returns></returns>
		public T ToObject<T>(int index, bool freeGCHandle = true)
		{
			if (IsNil(index) || !IsLightUserData(index))
				return default;

			IntPtr data = ToUserData(index);
			if (data == IntPtr.Zero)
				return default;

			var handle = GCHandle.FromIntPtr(data);
			if (!handle.IsAllocated)
				return default;

			var reference = (T)handle.Target;

			if (freeGCHandle)
				handle.Free();

			return reference;
		}

		/// <summary>
		/// If the value at the given index is a full userdata, returns its block address. If the value is a light userdata, returns its pointer. Otherwise, returns NULL
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public IntPtr ToUserData(int index)
		{
			return NativeMethods.lua_touserdata(_luaState, index);
		}


		/// <summary>
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public LuaType Type(int index)
		{
			return (LuaType)NativeMethods.lua_type(_luaState, index);
		}

		/// <summary>
		/// Returns the name of the type of the value at the given index. 
		/// </summary>
		/// <param name="type"></param>
		/// <returns>Name of the type of the value at the given index</returns>
		public string TypeName(LuaType type)
		{
			IntPtr ptr = NativeMethods.lua_typename(_luaState, (int)type);
			return Marshal.PtrToStringAnsi(ptr);
		}

		/// <summary>
		/// Returns a unique identifier for the upvalue numbered n from the closure at index funcindex.
		/// </summary>
		/// <param name="functionIndex"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public long UpValueId(int functionIndex, int n)
		{
			return (long)NativeMethods.lua_upvalueid(_luaState, functionIndex, n);
		}

		/// <summary>
		/// Returns the pseudo-index that represents the i-th upvalue of the running function 
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public static int UpValueIndex(int i)
		{
			return (int)LuaRegistry.Index - i;
		}

		/// <summary>
		/// Make the n1-th upvalue of the Lua closure at index funcindex1 refer to the n2-th upvalue of the Lua closure at index funcindex2
		/// </summary>
		/// <param name="functionIndex1"></param>
		/// <param name="n1"></param>
		/// <param name="functionIndex2"></param>
		/// <param name="n2"></param>
		public void UpValueJoin(int functionIndex1, int n1, int functionIndex2, int n2)
		{
			NativeMethods.lua_upvaluejoin(_luaState, functionIndex1, n1, functionIndex2, n2);
		}

		/// <summary>
		/// Return the version of Lua (e.g 504)
		/// </summary>
		/// <returns></returns>
		public double Version()
			=> NativeMethods switch
			{
				Lua54NativeMethods => 504,
				Lua53NativeMethods => 503,
				null => throw new LuaException($"{nameof(NativeMethods)} is null?"),
				_ => throw new InvalidOperationException()
			};

		/// <summary>
		/// Exchange values between different threads of the same state.
		/// This function pops n values from the current stack, and pushes them onto the stack to. 
		/// </summary>
		/// <param name="to"></param>
		/// <param name="n"></param>
		public void XMove(LuaState to, int n)
		{
			if (to == null)
				throw new ArgumentNullException(nameof(to), "to shouldn't be null");

			NativeMethods.lua_xmove(_luaState, to._luaState, n);
		}

		/// <summary>
		/// This function is equivalent to lua_yieldk, but it has no continuation (see §4.7). Therefore, when the thread resumes, it continues the function that called the function calling lua_yield. 
		/// </summary>
		/// <param name="results"></param>
		/// <returns></returns>
		public int Yield(int results)
		{
			return NativeMethods.lua_yieldk(_luaState, results, IntPtr.Zero, IntPtr.Zero);
		}

		/// <summary>
		/// Yields a coroutine (thread). When a C function calls lua_yieldk, the running coroutine suspends its execution, and the call to lua_resume that started this coroutine returns
		/// </summary>
		/// <param name="results">Number of values from the stack that will be passed as results to lua_resume.</param>
		/// <param name="context"></param>
		/// <param name="continuation"></param>
		/// <returns></returns>
		public int YieldK(int results, int context, LuaKFunction continuation)
		{
			IntPtr k = continuation.ToFunctionPointer();
			return NativeMethods.lua_yieldk(_luaState, results, (IntPtr)context, k);
		}

		// Auxialiary Library Functions

		/// <summary>
		/// Checks whether cond is true. If it is not, raises an error with a standard message
		/// </summary>
		/// <param name="condition"></param>
		/// <param name="argument"></param>
		/// <param name="message"></param>
		public void ArgumentCheck(bool condition, int argument, string message)
		{
			if (condition)
				return;
			ArgumentError(argument, message);
		}

		/// <summary>
		/// Raises an error reporting a problem with argument arg of the C function that called it, using a standard message that includes extramsg as a comment: 
		/// </summary>
		/// <param name="argument"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public int ArgumentError(int argument, string message)
		{
			// TODO: Use C# exception for errors?
			return NativeMethods.luaL_argerror(_luaState, argument, message);
		}

		/// <summary>
		/// If the object at index obj has a metatable and this metatable has a field e, this function calls this field passing the object as its only argument.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="field"></param>
		/// <returns>If there is no metatable or no metamethod, this function returns false (without pushing any value on the stack)</returns>
		public bool CallMetaMethod(int obj, string field)
		{
			return NativeMethods.luaL_callmeta(_luaState, obj, field) != 0;
		}

		/// <summary>
		/// Checks whether the function has an argument of any type (including nil) at position arg. 
		/// </summary>
		/// <param name="argument"></param>
		public void CheckAny(int argument)
		{
			NativeMethods.luaL_checkany(_luaState, argument);
		}

		/// <summary>
		/// Checks whether the function argument arg is an integer (or can be converted to an integer)
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public long CheckInteger(int argument)
		{
			return NativeMethods.luaL_checkinteger(_luaState, argument);
		}

		/// <summary>
		/// Checks whether the function argument arg is a string and returns this string;
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public byte[] CheckBuffer(int argument)
		{
			UIntPtr len;
			IntPtr buff = NativeMethods.luaL_checklstring(_luaState, argument, out len);
			if (buff == IntPtr.Zero)
				return null;

			int length = (int)len;
			if (length == 0)
				return Array.Empty<byte>();

			byte[] output = new byte[length];
			Marshal.Copy(buff, output, 0, length);
			return output;
		}

		/// <summary>
		/// Checks whether the function argument arg is a string and returns this string;
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public string CheckString(int argument)
		{
			byte[] buffer = CheckBuffer(argument);
			if (buffer == null)
				return null;
			return Encoding.GetString(buffer);
		}

		/// <summary>
		/// Checks whether the function argument arg is a number and returns this number. 
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public double CheckNumber(int argument)
		{
			return NativeMethods.luaL_checknumber(_luaState, argument);
		}


		/// <summary>
		/// Checks whether the function argument arg is a string and searches for this string in the array lst 
		/// </summary>
		/// <param name="argument"></param>
		/// <param name="def"></param>
		/// <param name="list"></param>
		/// <returns></returns>
		public int CheckOption(int argument, string def, string[] list)
		{
			return NativeMethods.luaL_checkoption(_luaState, argument, def, list);
		}


		/// <summary>
		/// Grows the stack size to top + sz elements, raising an error if the stack cannot grow 
		/// </summary>
		/// <param name="newSize"></param>
		/// <param name="message"></param>
		public void CheckStack(int newSize, string message)
		{
			NativeMethods.luaL_checkstack(_luaState, newSize, message);
		}

		/// <summary>
		/// Checks whether the function argument arg has type type
		/// </summary>
		/// <param name="argument"></param>
		/// <param name="type"></param>
		public void CheckType(int argument, LuaType type)
		{
			NativeMethods.luaL_checktype(_luaState, argument, (int)type);
		}

		/// <summary>
		/// Checks whether the function argument arg is a userdata of the type tname
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="argument"></param>
		/// <param name="typeName"></param>
		/// <param name="freeGCHandle">True to release the GCHandle</param>
		/// <returns></returns>
		public T CheckObject<T>(int argument, string typeName, bool freeGCHandle = true)
		{
			if (IsNil(argument) || !IsLightUserData(argument))
				return default;

			IntPtr data = CheckUserData(argument, typeName);
			if (data == IntPtr.Zero)
				return default;

			var handle = GCHandle.FromIntPtr(data);
			if (!handle.IsAllocated)
				return default;

			var reference = (T)handle.Target;

			if (freeGCHandle)
				handle.Free();

			return reference;
		}

		/// <summary>
		/// Checks whether the function argument arg is a userdata of the type tname (see luaL_newmetatable) and returns the userdata address
		/// </summary>
		/// <param name="argument"></param>
		/// <param name="typeName"></param>
		/// <returns></returns>
		public IntPtr CheckUserData(int argument, string typeName)
		{
			return NativeMethods.luaL_checkudata(_luaState, argument, typeName);
		}

		/// <summary>
		/// Loads and runs the given file
		/// </summary>
		/// <param name="file"></param>
		/// <returns>It returns false if there are no errors or true in case of errors. </returns>
		public bool DoFile(string file)
		{
			bool hasError = LoadFile(file) != LuaStatus.OK || PCall(0, -1, 0) != LuaStatus.OK;
			return hasError;
		}

		/// <summary>
		/// Loads and runs the given string
		/// </summary>
		/// <param name="file"></param>
		/// <returns>It returns false if there are no errors or true in case of errors. </returns>
		public bool DoString(string file)
		{
			bool hasError = LoadString(file) != LuaStatus.OK || PCall(0, -1, 0) != LuaStatus.OK;
			return hasError;
		}

		/// <summary>
		/// Raises an error. The error message format is given by fmt plus any extra arguments
		/// </summary>
		/// <param name="value"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public int Error(string value, params object[] v)
		{
			string message = string.Format(value, v);
			return NativeMethods.luaL_error(_luaState, message);
		}

		/// <summary>
		/// This function produces the return values for process-related functions in the standard library
		/// </summary>
		/// <param name="stat"></param>
		/// <returns></returns>
		public int ExecResult(int stat)
		{
			return NativeMethods.luaL_execresult(_luaState, stat);
		}

		/// <summary>
		/// This function produces the return values for file-related functions in the standard library
		/// </summary>
		/// <param name="stat"></param>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public int FileResult(int stat, string fileName)
		{
			return NativeMethods.luaL_fileresult(_luaState, stat, fileName);
		}

		/// <summary>
		/// Pushes onto the stack the field e from the metatable of the object at index obj and returns the type of the pushed value
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="field"></param>
		/// <returns></returns>
		public LuaType GetMetaField(int obj, string field)
		{
			return (LuaType)NativeMethods.luaL_getmetafield(_luaState, obj, field);
		}

		/// <summary>
		/// Pushes onto the stack the metatable associated with name tname in the registry (see luaL_newmetatable) (nil if there is no metatable associated with that name)
		/// </summary>
		/// <param name="tableName"></param>
		/// <returns>Returns the type of the pushed value. </returns>
		public LuaType GetMetaTable(string tableName)
		{
			return GetField(LuaRegistry.Index, tableName);
		}

		/// <summary>
		/// Ensures that the value t[fname], where t is the value at index idx, is a table, and pushes that table onto the stack. Returns true if it finds a previous table there and false if it creates a new table
		/// </summary>
		/// <param name="index"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool GetSubTable(int index, string name)
		{
			return NativeMethods.luaL_getsubtable(_luaState, index, name) != 0;
		}

		/// <summary>
		/// Returns the "length" of the value at the given index as a number; it is equivalent to the '#' operator in Lua
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public long Length(int index) => NativeMethods.luaL_len(_luaState, index);

		/// <summary>
		/// Loads a buffer as a Lua chunk
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="name"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public LuaStatus LoadBuffer(byte[] buffer, string name, string mode)
		{
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer), "buffer shouldn't be null");

			return (LuaStatus)NativeMethods.luaL_loadbufferx(_luaState, buffer, (UIntPtr)buffer.Length, name, mode);
		}

		/// <summary>
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public LuaStatus LoadBuffer(byte[] buffer, string name)
		{
			return LoadBuffer(buffer, name, null);
		}

		/// <summary>
		/// Loads a buffer as a Lua chunk
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		public LuaStatus LoadBuffer(byte[] buffer)
		{
			return LoadBuffer(buffer, null, null);

		}

		/// <summary>
		/// Loads a string as a Lua chunk
		/// </summary>
		/// <param name="chunk"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public LuaStatus LoadString(string chunk, string name)
		{
			byte[] buffer = Encoding.GetBytes(chunk);
			return LoadBuffer(buffer, name);
		}

		/// <summary>
		/// Loads a string as a Lua chunk
		/// </summary>
		/// <param name="chunk"></param>
		/// <returns></returns>
		public LuaStatus LoadString(string chunk)
		{
			return LoadString(chunk, null);
		}

		/// <summary>
		/// Loads a file as a Lua chunk. This function uses lua_load to load the chunk in the file named filename
		/// </summary>
		/// <param name="file"></param>
		/// <param name="mode"></param>
		/// <returns>The status of operation</returns>
		public LuaStatus LoadFile(string file, string mode)
		{
			return (LuaStatus)NativeMethods.luaL_loadfilex(_luaState, file, mode);
		}

		/// <summary>
		/// Loads a file as a Lua chunk.
		/// </summary>
		/// <param name="file"></param>
		/// <returns>Return the status</returns>
		public LuaStatus LoadFile(string file)
		{
			return LoadFile(file, null);
		}

		/// <summary>
		/// Creates a new table and registers there the functions in list library. 
		/// </summary>
		/// <param name="library"></param>
		public void NewLib(LuaRegister[] library)
		{
			NewLibTable(library);
			SetFuncs(library, 0);
		}

		/// <summary>
		/// Creates a new table with a size optimized to store all entries in the array l (but does not actually store them)
		/// </summary>
		/// <param name="library"></param>
		public void NewLibTable(LuaRegister[] library)
		{
			if (library == null)
				throw new ArgumentNullException(nameof(library), "library shouldn't be null");

			CreateTable(0, library.Length);
		}

		/// <summary>
		/// Creates a new table to be used as a metatable for userdata
		/// </summary>
		/// <param name="name"></param>
		/// <returns>If the registry already has the key tname, returns false.,</returns>
		public bool NewMetaTable(string name)
		{
			return NativeMethods.luaL_newmetatable(_luaState, name) != 0;
		}

		/// <summary>
		/// Opens all standard Lua libraries into the given state. 
		/// </summary>
		public void OpenLibs()
		{
			NativeMethods.luaL_openlibs(_luaState);
		}

		/// <summary>
		/// If the function argument arg is an integer (or convertible to an integer), returns this integer. If this argument is absent or is nil, returns d
		/// </summary>
		/// <param name="argument"></param>
		/// <param name="d">default value</param>
		/// <returns></returns>
		public long OptInteger(int argument, long d)
		{
			return NativeMethods.luaL_optinteger(_luaState, argument, d);
		}

		/// <summary>
		/// If the function argument arg is a string, returns this string. If this argument is absent or is nil, returns d        /// </summary>
		/// <param name="index"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		public byte[] OptBuffer(int index, byte[] def)
		{
			if (IsNoneOrNil(index))
				return def;

			return CheckBuffer(index);
		}

		/// <summary>
		/// If the function argument arg is a string, returns this string. If this argument is absent or is nil, returns d
		/// </summary>
		/// <param name="index"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		public string OptString(int index, string def)
		{
			if (IsNoneOrNil(index))
				return def;

			return CheckString(index);
		}


		/// <summary>
		/// If the function argument arg is a number, returns this number. If this argument is absent or is nil, returns d
		/// </summary>
		/// <param name="index"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		public double OptNumber(int index, double def)
		{
			return NativeMethods.luaL_optnumber(_luaState, index, def);
		}

		/// <summary>
		/// Creates and returns a reference, in the table at index t, for the object at the top of the stack (and pops the object). 
		/// </summary>
		/// <param name="tableIndex"></param>
		/// <returns></returns>
		public int Ref(LuaRegistry tableIndex)
		{
			return NativeMethods.luaL_ref(_luaState, (int)tableIndex);
		}

		/// <summary>
		/// If modname is not already present in package.loaded, calls function openf with string modname as an argument and sets the call result in package.loaded[modname], as if that function has been called through require
		/// </summary>
		/// <param name="moduleName"></param>
		/// <param name="openFunction"></param>
		/// <param name="global"></param>
		public void RequireF(string moduleName, LuaNativeFunction openFunction, bool global)
		{
			NativeMethods.luaL_requiref(_luaState, moduleName, openFunction.ToFunctionPointer(), global ? 1 : 0);
		}

		/// <summary>
		/// Registers all functions in the array l (see luaL_Reg) into the table on the top of the stack (below optional upvalues, see next).        /// </summary>
		/// <param name="library"></param>
		/// <param name="numberUpValues"></param>
		public void SetFuncs(LuaRegister[] library, int numberUpValues)
		{
			NativeMethods.luaL_setfuncs(_luaState, library, numberUpValues);
		}

		/// <summary>
		/// Sets the metatable of the object at the top of the stack as the metatable associated with name tname in the registry
		/// </summary>
		/// <param name="name"></param>
		public void SetMetaTable(string name)
		{
			NativeMethods.luaL_setmetatable(_luaState, name);
		}

		/// <summary>
		/// Test if the value at index is a reference data
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="argument"></param>
		/// <param name="typeName"></param>
		/// <param name="freeGCHandle">True to release the GCHandle of object</param>
		/// <returns></returns>
		public T TestObject<T>(int argument, string typeName, bool freeGCHandle = true)
		{
			if (IsNil(argument) || !IsLightUserData(argument))
				return default;

			IntPtr data = TestUserData(argument, typeName);
			if (data == IntPtr.Zero)
				return default;

			var handle = GCHandle.FromIntPtr(data);
			if (!handle.IsAllocated)
				return default;

			var reference = (T)handle.Target;
			if (freeGCHandle)
				handle.Free();

			return reference;
		}

		/// <summary>
		/// This function works like luaL_checkudata, except that, when the test fails, it returns NULL instead of raising an error.
		/// </summary>
		/// <param name="argument"></param>
		/// <param name="typeName"></param>
		/// <returns></returns>
		public IntPtr TestUserData(int argument, string typeName)
		{
			return NativeMethods.luaL_testudata(_luaState, argument, typeName);
		}

		/// <summary>
		/// Creates and pushes a traceback of the stack L1
		/// </summary>
		/// <param name="state"></param>
		/// <param name="level"> Tells at which level to start the traceback</param>
		public void Traceback(LuaState state, int level = 0)
		{
			Traceback(state, null, level);
		}

		/// <summary>
		/// Creates and pushes a traceback of the stack L1
		/// </summary>
		/// <param name="state"></param>
		/// <param name="message">appended at the beginning of the traceback</param>
		/// <param name="level"> Tells at which level to start the traceback</param>
		public void Traceback(LuaState state, string message, int level)
		{
			if (state == null)
				throw new ArgumentNullException(nameof(state), "state shouldn't be null");

			NativeMethods.luaL_traceback(_luaState, state._luaState, message, level);
		}

		/// <summary>
		/// Returns the name of the type of the value at the given index. 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public string TypeName(int index)
		{
			LuaType type = Type(index);
			return TypeName(type);
		}

		/// <summary>
		/// Releases reference ref from the table at index t (see luaL_ref). The entry is removed from the table, so that the referred object can be collected. The reference ref is also freed to be used again
		/// </summary>
		/// <param name="tableIndex"></param>
		/// <param name="reference"></param>
		public void Unref(LuaRegistry tableIndex, int reference)
		{
			NativeMethods.luaL_unref(_luaState, (int)tableIndex, reference);
		}

		/// <summary>
		/// Pushes onto the stack a string identifying the current position of the control at level lvl in the call stack
		/// </summary>
		/// <param name="level"></param>
		public void Where(int level)
		{
			NativeMethods.luaL_where(_luaState, level);
		}
	}
}

