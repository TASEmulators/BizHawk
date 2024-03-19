using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace NLua.Native
{
	/// <summary>
	/// Lua state class, main interface to use Lua library.
	/// </summary>
	internal class LuaState : IDisposable
	{
		private readonly LuaNativeMethods NativeMethods;

		private readonly LuaState _mainState;

		/// <summary>
		/// Internal Lua handle pointer.
		/// </summary>
		public IntPtr Handle { get; private set; }

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
			NativeMethods = LuaNativeMethodLoader.GetNativeMethods();
			Handle = NativeMethods.LNewState();

			if (openLibs)
			{
				OpenLibs();
			}

			SetExtraObject(this, true);
		}

		private LuaState(IntPtr luaThread, LuaState mainState)
		{
			NativeMethods = LuaNativeMethodLoader.GetNativeMethods();
			_mainState = mainState;
			Handle = luaThread;

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
			{
				return null;
			}

			var state = GetExtraObject<LuaState>(luaState);
			if (state != null && state.Handle == luaState)
			{
				return state;
			}

			return new(luaState, state?.MainThread);
		}

		/// <summary>
		/// Finalizes an instance of the <see cref="LuaState"/> class.
		/// </summary>
		~LuaState()
		{
			Dispose();
		}

		/// <summary>
		/// Destroys all objects in the given Lua state (calling the corresponding garbage-collection metamethods, if any) and frees all dynamic memory used by this state
		/// </summary>
		public void Close()
		{
			if (Handle == IntPtr.Zero || _mainState != null)
			{
				return;
			}

			NativeMethods.Close(Handle);
			Handle = IntPtr.Zero;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose the lua context (calling Close)
		/// </summary>
		public void Dispose()
			=> Close();

		private void SetExtraObject<T>(T obj, bool weak) where T : class
		{
			var handle = GCHandle.Alloc(obj, weak ? GCHandleType.Weak : GCHandleType.Normal);
			var extraSpace = Handle - IntPtr.Size;
			Marshal.WriteIntPtr(extraSpace, GCHandle.ToIntPtr(handle));
		}

		private static T GetExtraObject<T>(IntPtr luaState) where T : class
		{
			var extraSpace = luaState - IntPtr.Size;
			var pointer = Marshal.ReadIntPtr(extraSpace);
			var handle = GCHandle.FromIntPtr(pointer);
			if (!handle.IsAllocated)
			{
				return null;
			}

			return (T)handle.Target;
		}

		/// <summary>
		/// Sets a new panic function
		/// </summary>
		/// <param name="panicFunction"></param>
		public void AtPanic(LuaNativeFunction panicFunction)
		{
			var newPanicPtr = panicFunction.ToFunctionPointer();
			_ = NativeMethods.AtPanic(Handle, newPanicPtr);
		}

		/// <summary>
		/// Ensures that the stack has space for at least n extra slots (that is, that you can safely push up to n values into it). It returns false if it cannot fulfill the request,
		/// </summary>
		/// <param name="nExtraSlots"></param>
		public bool CheckStack(int nExtraSlots)
			=> NativeMethods.CheckStack(Handle, nExtraSlots) != 0;

		/// <summary>
		/// Compares two Lua values. Returns 1 if the value at index index1 satisfies op when compared with the value at index index2
		/// </summary>
		/// <param name="index1"></param>
		/// <param name="index2"></param>
		/// <param name="comparison"></param>
		/// <returns></returns>
		public bool Compare(int index1, int index2, LuaCompare comparison)
			=> NativeMethods.Compare(Handle, index1, index2, (int)comparison) != 0;

		/// <summary>
		/// Generates a Lua error, using the value at the top of the stack as the error object. This function does a long jump
		/// (We want it to be inlined to avoid issues with managed stack)
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Error()
			=> NativeMethods.Error(Handle);

		/// <summary>
		/// Pushes onto the stack the value t[k], where t is the value at the given index. As in Lua, this function may trigger a metamethod for the "index" event (see §2.4).
		/// </summary>
		/// <param name="index"></param>
		/// <param name="key"></param>
		public void GetField(int index, string key)
			=> _ = NativeMethods.GetField(Handle, index, key);

		/// <summary>
		/// Pushes onto the stack the value t[k], where t is the value at the given index. As in Lua, this function may trigger a metamethod for the "index" event (see §2.4).
		/// Returns the type of the pushed value. 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public LuaType GetField(LuaRegistry index, string key)
			=> (LuaType)NativeMethods.GetField(Handle, (int)index, key);

		/// <summary>
		/// Pushes onto the stack the value of the global name. Returns the type of that value
		/// </summary>
		/// <param name="name"></param>
		public void GetGlobal(string name)
			=> _ = NativeMethods.GetGlobal(Handle, name);

		/// <summary>
		/// If the value at the given index has a metatable, the function pushes that metatable onto the stack and returns 1
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool GetMetaTable(int index)
			=> NativeMethods.GetMetaTable(Handle, index) != 0;

		/// <summary>
		/// Pushes onto the stack the value t[k], where t is the value at the given index and k is the value at the top of the stack. 
		/// </summary>
		/// <param name="index"></param>
		public void GetTable(int index)
			=> _ = NativeMethods.GetTable(Handle, index);

		/// <summary>
		/// Returns the index of the top element in the stack. 0 means an empty stack.
		/// </summary>
		/// <returns>Returns the index of the top element in the stack.</returns>
		public int GetTop()
			=> NativeMethods.GetTop(Handle);

		/// <summary>
		/// Moves the top element into the given valid index, shifting up the elements above this index to open space. This function cannot be called with a pseudo-index, because a pseudo-index is not an actual stack position. 
		/// </summary>
		/// <param name="index"></param>
		public void Insert(int index)
			=> NativeMethods.Rotate(Handle, index, 1);

		/// <summary>
		/// Returns if the value at the given index is a boolean
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsBoolean(int index)
			=> Type(index) == LuaType.Boolean;

		/// <summary>
		/// Returns if the value at the given index is an integer
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsInteger(int index)
			=> NativeMethods.IsInteger(Handle, index) != 0;

		/// <summary>
		/// Returns if the value at the given index is nil
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsNil(int index)
			=> Type(index) == LuaType.Nil;

		/// <summary>
		/// Returns if the value at the given index is a number
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsNumber(int index)
			=> NativeMethods.IsNumber(Handle, index) != 0;

		/// <summary>
		/// Returns if the value at the given index is a string or a number (which is always convertible to a string)
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsStringOrNumber(int index)
			=> NativeMethods.IsString(Handle, index) != 0;

		/// <summary>
		/// Creates a new empty table and pushes it onto the stack
		/// </summary>
		public void NewTable()
			=> NativeMethods.CreateTable(Handle, 0, 0);

		/// <summary>
		/// Creates a new thread, pushes it on the stack, and returns a pointer to a lua_State that represents this new thread. The new thread returned by this function shares with the original thread its global environment, but has an independent execution stack. 
		/// </summary>
		/// <returns></returns>
		public LuaState NewThread()
		{
			var thread = NativeMethods.NewThread(Handle);
			return new(thread, this);
		}

		/// <summary>
		/// This function creates and pushes on the stack a new full userdata,
		/// with nuvalue associated Lua values, called user values, plus an
		/// associated block of raw memory with size bytes. (The user values
		/// can be set and read with the functions lua_setiuservalue and lua_getiuservalue.)
		/// The function returns the address of the block of memory.
		/// </summary>
		public IntPtr NewUserData(int size)
			=> NativeMethods.NewUserData(Handle, (UIntPtr)size);

		/// <summary>
		/// Pops a key from the stack, and pushes a key–value pair from the table at the given index (the "next" pair after the given key).
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool Next(int index)
			=> NativeMethods.Next(Handle, index) != 0;

		/// <summary>
		/// Calls a function in protected mode. 
		/// </summary>
		/// <param name="arguments"></param>
		/// <param name="results"></param>
		/// <param name="errorFunctionIndex"></param>
		public LuaStatus PCall(int arguments, int results, int errorFunctionIndex)
			=> (LuaStatus)NativeMethods.PCallK(Handle, arguments, results, errorFunctionIndex, IntPtr.Zero, IntPtr.Zero);

		/// <summary>
		/// Pops n elements from the stack. 
		/// </summary>
		/// <param name="n"></param>
		public void Pop(int n)
			=> NativeMethods.SetTop(Handle, -n - 1);

		/// <summary>
		/// Pushes a boolean value with value b onto the stack. 
		/// </summary>
		/// <param name="b"></param>
		public void PushBoolean(bool b)
			=> NativeMethods.PushBoolean(Handle, b ? 1 : 0);

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
			=> NativeMethods.PushCClosure(Handle, function.ToFunctionPointer(), n);

		/// <summary>
		/// Pushes a C function onto the stack. This function receives a pointer to a C function and pushes onto the stack a Lua value of type function that, when called, invokes the corresponding C function. 
		/// </summary>
		/// <param name="function"></param>
		public void PushCFunction(LuaNativeFunction function)
			=> PushCClosure(function, 0);

		/// <summary>
		/// Pushes the global environment onto the stack. 
		/// </summary>
		public void PushGlobalTable()
			=> _ = NativeMethods.RawGetI(Handle, (int)LuaRegistry.Index, (int)LuaRegistryIndex.Globals);

		/// <summary>
		/// Pushes an integer with value n onto the stack. 
		/// </summary>
		/// <param name="n"></param>
		public void PushInteger(long n)
			=> NativeMethods.PushInteger(Handle, n);

		/// <summary>
		/// Pushes a light userdata onto the stack.
		/// Userdata represent C values in Lua. A light userdata represents a pointer, a void*. It is a value (like a number): you do not create it, it has no individual metatable, and it is not collected (as it was never created). A light userdata is equal to "any" light userdata with the same C address. 
		/// </summary>
		/// <param name="data"></param>
		public void PushLightUserData(IntPtr data)
			=> NativeMethods.PushLightUserData(Handle, data);

		/// <summary>
		/// Pushes binary buffer onto the stack (usually UTF encoded string) or any arbitraty binary data
		/// </summary>
		/// <param name="buffer"></param>
		public void PushBuffer(ReadOnlySpan<byte> buffer)
		{
			if (buffer == null)
			{
				PushNil();
				return;
			}

			_ = NativeMethods.PushLString(Handle, buffer, (UIntPtr)buffer.Length);
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

			// could also use GetByteCount here, but why iterate twice when we're renting the memory anyway?
			byte[] buffer = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(value.Length));
			int actualLength = Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);
			PushBuffer(buffer.AsSpan(0, actualLength));
			ArrayPool<byte>.Shared.Return(buffer);
		}

		/// <summary>
		/// Pushes a nil value onto the stack. 
		/// </summary>
		public void PushNil()
			=> NativeMethods.PushNil(Handle);

		/// <summary>
		/// Pushes a double with value n onto the stack. 
		/// </summary>
		/// <param name="number"></param>
		public void PushNumber(double number)
			=> NativeMethods.PushNumber(Handle, number);

		/// <summary>
		/// Pushes the thread represented by L onto the stack.
		/// </summary>
		public void PushThread()
			=> _ = NativeMethods.PushThread(Handle);

		/// <summary>
		/// Pushes a copy of the element at the given index onto the stack. (lua_pushvalue)
		/// The method was renamed, since pushvalue is a bit vague
		/// </summary>
		/// <param name="index"></param>
		public void PushCopy(int index)
			=> NativeMethods.PushValue(Handle, index);

		/// <summary>
		/// Returns true if the two values in indices index1 and index2 are primitively equal (that is, without calling the __eq metamethod). Otherwise returns false. Also returns false if any of the indices are not valid. 
		/// </summary>
		/// <param name="index1"></param>
		/// <param name="index2"></param>
		/// <returns></returns>
		public bool RawEqual(int index1, int index2)
			=> NativeMethods.RawEqual(Handle, index1, index2) != 0;

		/// <summary>
		/// Similar to GetTable, but does a raw access (i.e., without metamethods). 
		/// </summary>
		/// <param name="index"></param>
		public void RawGet(int index)
			=> _ = NativeMethods.RawGet(Handle, index);

		/// <summary>
		/// Similar to GetTable, but does a raw access (i.e., without metamethods). 
		/// </summary>
		/// <param name="index"></param>
		public void RawGet(LuaRegistry index)
			=> _ = NativeMethods.RawGet(Handle, (int)index);

		/// <summary>
		/// Pushes onto the stack the value t[n], where t is the table at the given index. The access is raw, that is, it does not invoke the __index metamethod. 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="n"></param>
		public void RawGetInteger(int index, long n)
			=> _ = NativeMethods.RawGetI(Handle, index, n);

		/// <summary>
		/// Pushes onto the stack the value t[n], where t is the table at the given index. The access is raw, that is, it does not invoke the __index metamethod. 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="n"></param>
		public void RawGetInteger(LuaRegistry index, long n)
			=> _ = NativeMethods.RawGetI(Handle, (int)index, n);

		/// <summary>
		/// Similar to lua_settable, but does a raw assignment (i.e., without metamethods).
		/// </summary>
		/// <param name="index"></param>
		public void RawSet(int index)
			=> NativeMethods.RawSet(Handle, index);

		/// <summary>
		/// Similar to lua_settable, but does a raw assignment (i.e., without metamethods).
		/// </summary>
		/// <param name="index"></param>
		public void RawSet(LuaRegistry index)
			=> NativeMethods.RawSet(Handle, (int)index);

		/// <summary>
		/// Does the equivalent of t[i] = v, where t is the table at the given index and v is the value at the top of the stack.
		/// This function pops the value from the stack. The assignment is raw, that is, it does not invoke the __newindex metamethod. 
		/// </summary>
		/// <param name="index">index of table</param>
		/// <param name="i">value</param>
		public void RawSetInteger(int index, long i)
			=> NativeMethods.RawSetI(Handle, index, i);

		/// <summary>
		/// Does the equivalent of t[i] = v, where t is the table at the given index and v is the value at the top of the stack.
		/// This function pops the value from the stack. The assignment is raw, that is, it does not invoke the __newindex metamethod. 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="i"></param>
		public void RawSetInteger(LuaRegistry index, long i)
			=> NativeMethods.RawSetI(Handle, (int)index, i);

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
			=> (LuaStatus)NativeMethods.Resume(Handle, from?.Handle ?? IntPtr.Zero, arguments);

		/// <summary>
		/// Rotates the stack elements between the valid index idx and the top of the stack. The elements are rotated n positions in the direction of the top, for a positive n, or -n positions in the direction of the bottom, for a negative n. The absolute value of n must not be greater than the size of the slice being rotated. This function cannot be called with a pseudo-index, because a pseudo-index is not an actual stack position. 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="n"></param>
		public void Rotate(int index, int n)
			=> NativeMethods.Rotate(Handle, index, n);

		/// <summary>
		/// Pops a value from the stack and sets it as the new value of global name. 
		/// </summary>
		/// <param name="name"></param>
		public void SetGlobal(string name)
			=> NativeMethods.SetGlobal(Handle, name);

		/// <summary>
		/// Pops a table from the stack and sets it as the new metatable for the value at the given index. 
		/// </summary>
		/// <param name="index"></param>
		public void SetMetaTable(int index)
			=> NativeMethods.SetMetaTable(Handle, index);

		/// <summary>
		/// Does the equivalent to t[k] = v, where t is the value at the given index, v is the value at the top of the stack, and k is the value just below the top
		/// </summary>
		/// <param name="index"></param>
		public void SetTable(int index)
			=> NativeMethods.SetTable(Handle, index);

		/// <summary>
		/// Accepts any index, or 0, and sets the stack top to this index. If the new top is larger than the old one, then the new elements are filled with nil. If index is 0, then all stack elements are removed. 
		/// </summary>
		/// <param name="newTop"></param>
		public void SetTop(int newTop)
			=> NativeMethods.SetTop(Handle, newTop);

		/// <summary>
		/// Converts the Lua value at the given index to a C# boolean value
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool ToBoolean(int index)
			=> NativeMethods.ToBoolean(Handle, index) != 0;

		/// <summary>
		/// Converts the Lua value at the given index to the signed integral type lua_Integer. The Lua value must be an integer, or a number or string convertible to an integer (see §3.4.3); otherwise, lua_tointegerx returns 0. 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public long ToInteger(int index)
			=> NativeMethods.ToIntegerX(Handle, index, out _);

		private IntPtr ToLString(int index, bool callMetamethod, out int stringLength)
		{
			UIntPtr len;
			IntPtr buff;

			if (callMetamethod)
			{
				buff = NativeMethods.LToLString(Handle, index, out len);
				Pop(1);
			}
			else
			{
				buff = NativeMethods.ToLString(Handle, index, out len);
			}

			stringLength = (int)len;

			return buff;
		}

		/// <summary>
		/// Converts the Lua value at the given index to a byte array.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="callMetamethod">Calls __tostring field if present</param>
		/// <returns></returns>
		public byte[] ToBuffer(int index, bool callMetamethod = true)
		{
			IntPtr buffer = ToLString(index, callMetamethod, out int length);

			if (buffer == IntPtr.Zero) return null;
			if (length == 0) return Array.Empty<byte>();

			var output = new byte[length];
			Marshal.Copy(buffer, output, 0, length);
			return output;
		}

		/// <summary>
		/// Converts the Lua value at the given index to a C# string
		/// </summary>
		/// <param name="index"></param>
		/// <param name="callMetamethod">Calls __tostring field if present</param>
		/// <returns></returns>
		public string ToString(int index, bool callMetamethod = true)
		{
			var buffer = ToLString(index, callMetamethod, out int length);

			if (buffer == IntPtr.Zero) return null;
			if (length == 0) return "";

			unsafe
			{
				return Encoding.UTF8.GetString((byte*)buffer.ToPointer(), length);
			}
		}

		/// <summary>
		/// Converts the Lua value at the given index to a C# double
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public double ToNumber(int index)
			=> NativeMethods.ToNumberX(Handle, index, out _);

		/// <summary>
		/// Converts the value at the given index to a Lua thread
		/// or return the self if is the main thread
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public LuaState ToThread(int index)
		{
			var state = NativeMethods.ToThread(Handle, index);
			return state == Handle ? this : FromIntPtr(state);
		}

		/// <summary>
		/// If the value at the given index is a full userdata, returns its block address. If the value is a light userdata, returns its pointer. Otherwise, returns NULL
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public IntPtr ToUserData(int index)
			=> NativeMethods.ToUserData(Handle, index);

		/// <summary>
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public LuaType Type(int index)
			=> (LuaType)NativeMethods.Type(Handle, index);

		/// <summary>
		/// Return the version of Lua (e.g 504)
		/// </summary>
		/// <returns></returns>
		public double Version()
			=> NativeMethods.IsLua53 ? 503 : 504;

		/// <summary>
		/// Exchange values between different threads of the same state.
		/// This function pops n values from the current stack, and pushes them onto the stack to. 
		/// </summary>
		/// <param name="to"></param>
		/// <param name="n"></param>
		public void XMove(LuaState to, int n)
		{
			if (to == null)
			{
				throw new ArgumentNullException(nameof(to), "to shouldn't be null");
			}

			NativeMethods.XMove(Handle, to.Handle, n);
		}

		/// <summary>
		/// This function is equivalent to lua_yieldk, but it has no continuation (see §4.7). Therefore, when the thread resumes, it continues the function that called the function calling lua_yield. 
		/// </summary>
		/// <param name="results"></param>
		public void Yield(int results)
			=> _ = NativeMethods.YieldK(Handle, results, IntPtr.Zero, IntPtr.Zero);

		// Auxialiary Library Functions

		/// <summary>
		/// Loads and runs the given string
		/// </summary>
		/// <param name="file"></param>
		public void DoString(string file)
			=> _ = LoadString(file) != LuaStatus.OK || PCall(0, -1, 0) != LuaStatus.OK;

		/// <summary>
		/// Pushes onto the stack the field e from the metatable of the object at index obj and returns the type of the pushed value
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="field"></param>
		/// <returns></returns>
		public LuaType GetMetaField(int obj, string field)
			=> (LuaType)NativeMethods.LGetMetaField(Handle, obj, field);

		/// <summary>
		/// Pushes onto the stack the metatable associated with name tname in the registry (see luaL_newmetatable) (nil if there is no metatable associated with that name)
		/// </summary>
		/// <param name="tableName"></param>
		public void GetMetaTable(string tableName)
			=> _ = GetField(LuaRegistry.Index, tableName);

		/// <summary>
		/// Loads a buffer as a Lua chunk
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="name"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public LuaStatus LoadBuffer(byte[] buffer, string name = null, string mode = null)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer), "buffer shouldn't be null");
			}

			return (LuaStatus)NativeMethods.LLoadBufferX(Handle, buffer, (UIntPtr)buffer.Length, name, mode);
		}

		/// <summary>
		/// Loads a string as a Lua chunk
		/// </summary>
		/// <param name="chunk"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public LuaStatus LoadString(string chunk, string name = null)
		{
			var buffer = Encoding.UTF8.GetBytes(chunk);
			return LoadBuffer(buffer, name);
		}

		/// <summary>
		/// Loads a file as a Lua chunk. This function uses lua_load to load the chunk in the file named filename
		/// </summary>
		/// <param name="file"></param>
		/// <param name="mode"></param>
		/// <returns>The status of operation</returns>
		public LuaStatus LoadFile(string file, string mode = null)
			=> (LuaStatus)NativeMethods.LLoadFileX(Handle, file, mode);

		/// <summary>
		/// Creates a new table to be used as a metatable for userdata
		/// </summary>
		/// <param name="name"></param>
		public void NewMetaTable(string name)
			=> _ = NativeMethods.LNewMetaTable(Handle, name);

		/// <summary>
		/// Opens all standard Lua libraries into the given state. 
		/// </summary>
		public void OpenLibs()
			=> NativeMethods.LOpenLibs(Handle);

		/// <summary>
		/// Creates and returns a reference, in the table at index t, for the object at the top of the stack (and pops the object). 
		/// </summary>
		/// <param name="tableIndex"></param>
		/// <returns></returns>
		public int Ref(LuaRegistry tableIndex)
			=> NativeMethods.LRef(Handle, (int)tableIndex);

		/// <summary>
		/// Releases reference ref from the table at index t (see luaL_ref). The entry is removed from the table, so that the referred object can be collected. The reference ref is also freed to be used again
		/// </summary>
		/// <param name="tableIndex"></param>
		/// <param name="reference"></param>
		public void Unref(LuaRegistry tableIndex, int reference)
			=> NativeMethods.LUnref(Handle, (int)tableIndex, reference);

		/// <summary>
		/// Pushes onto the stack a string identifying the current position of the control at level lvl in the call stack
		/// </summary>
		/// <param name="level"></param>
		public void Where(int level)
			=> NativeMethods.LWhere(Handle, level);
	}
}
