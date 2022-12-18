using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NLua
{
	/// <summary>
	/// Structure for lua debug information
	/// </summary>
	/// <remarks>
	/// Do not change this struct because it must match the lua structure lua_Debug
	/// </remarks>
	/// <author>Reinhard Ostermeier</author>
	[StructLayout(LayoutKind.Sequential)]
	public struct LuaDebug
	{
		/// <summary>
		/// Get a LuaDebug from IntPtr
		/// </summary>
		/// <param name="ar"></param>
		/// <returns></returns>
		public static LuaDebug FromIntPtr(IntPtr ar)
		{
			return Marshal.PtrToStructure<LuaDebug>(ar);
		}
		/// <summary>
		/// Debug event code
		/// </summary>
		[MarshalAs(UnmanagedType.I4)]
		public LuaHookEvent Event;
		/// <summary>
		///  a reasonable name for the given function. Because functions in Lua are first-class values, they do not have a fixed name: some functions can be the value of multiple global variables, while others can be stored only in a table field
		/// </summary>
		public string Name => Marshal.PtrToStringAnsi(name);
		internal IntPtr name;
		/// <summary>
		/// explains the name field. The value of namewhat can be "global", "local", "method", "field", "upvalue", or "" (the empty string)
		/// </summary>
		public string NameWhat => Marshal.PtrToStringAnsi(what);
		internal IntPtr nameWhat;
		/// <summary>
		///  the string "Lua" if the function is a Lua function, "C" if it is a C function, "main" if it is the main part of a chunk
		/// </summary>
		public string What => Marshal.PtrToStringAnsi(what);
		internal IntPtr what;
		/// <summary>
		///  the name of the chunk that created the function. If source starts with a '@', it means that the function was defined in a file where the file name follows the '@'.
		/// </summary>
		/// 
		public string Source => Marshal.PtrToStringAnsi(source, SourceLength);
		internal IntPtr source;

		/// <summary>
		/// The length of the string source
		/// </summary>
		public int SourceLength => sourceLen.ToInt32();
		internal IntPtr sourceLen;

		/// <summary>
		///  the current line where the given function is executing. When no line information is available, currentline is set to -1
		/// </summary>
		public int CurrentLine;
		/// <summary>
		/// 
		/// </summary>
		public int LineDefined;
		/// <summary>
		///  the line number where the definition of the function ends. 
		/// </summary>
		public int LastLineDefined;
		/// <summary>
		/// number of upvalues
		/// </summary>
		public byte NumberUpValues;
		/// <summary>
		/// number of parameters
		/// </summary>
		public byte NumberParameters;
		/// <summary>
		///  true if the function is a vararg function (always true for C functions).
		/// </summary>
		[MarshalAs(UnmanagedType.I1)]
		public bool IsVarArg;        /* (u) */
		/// <summary>
		///  true if this function invocation was called by a tail call. In this case, the caller of this level is not in the stack.
		/// </summary>
		[MarshalAs(UnmanagedType.I1)]
		public bool IsTailCall; /* (t) */

		/// <summary>
		/// The index on the stack of the first value being "transferred", that is, parameters in a call or return values in a return. (The other values are in consecutive indices.) Using this index, you can access and modify these values through lua_getlocal and lua_setlocal. This field is only meaningful during a call hook, denoting the first parameter, or a return hook, denoting the first value being returned. (For call hooks, this value is always 1.) 
		/// </summary>
		public ushort IndexFirstValue;   /* (r) index of first value transferred */

		/// <summary>
		/// The number of values being transferred (see previous item). (For calls of Lua functions, this value is always equal to nparams.) 
		/// </summary>
		public ushort NumberTransferredValues;   /* (r) number of transferred values */

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
		internal byte[] shortSource;

		/// <summary>
		/// a "printable" version of source, to be used in error messages
		/// </summary>
		public string ShortSource
		{
			get
			{
				if (shortSource[0] == 0)
					return string.Empty;

				int count = 0;

				while (count < shortSource.Length && shortSource[count] != 0)
				{
					count++;
				}

				return Encoding.ASCII.GetString(shortSource, 0, count);
			}
		}
		internal IntPtr i_ci;
	}
}
