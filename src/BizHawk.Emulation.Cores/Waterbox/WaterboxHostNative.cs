using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public abstract class WaterboxHostNative
	{
		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct ReturnData
		{
			public fixed byte ErrorMessage[1024];
			public IntPtr Data;

			public IntPtr GetDataOrThrow()
			{
				if (ErrorMessage[0] != 0)
				{
					fixed (byte* p = ErrorMessage)
						throw new InvalidOperationException(Mershul.PtrToStringUtf8((IntPtr)p));
				}
				return Data;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public class MemoryLayoutTemplate
		{
			/// <summary>
			/// Memory space to serve brk(2)
			/// </summary>
			public UIntPtr sbrk_size;

			/// <summary>
			/// Memory space to serve alloc_sealed(3)
			/// </summary>
			public UIntPtr sealed_size;

			/// <summary>
			/// Memory space to serve alloc_invisible(3)
			/// </summary>
			public UIntPtr invis_size;

			/// <summary>
			/// Memory space to serve alloc_plain(3)
			/// </summary>
			public UIntPtr plain_size;

			/// <summary>
			/// Memory space to serve mmap(2) and friends.
			/// Calls without MAP_FIXED or MREMAP_FIXED will be placed in this area.
			/// TODO: Are we allowing fixed calls to happen anywhere in the block?
			/// </summary>
			public UIntPtr mmap_size;
		}

		/// <summary>
		/// Read bytes into the buffer.  Return number of bytes read on success, or &lt; 0 on failure.
		/// permitted to read less than the provided buffer size, but must always read at least 1
		/// byte if EOF is not reached.  If EOF is reached, should return 0.
		/// </summary>
		public delegate IntPtr ReadCallback(IntPtr userdata, IntPtr /*byte**/ data, UIntPtr size);

		/// <summary>
		/// write bytes.  Return 0 on success, or &lt; 0 on failure.
		/// Must write all provided bytes in one call or fail, not permitted to write less (unlike reader).
		/// </summary>
		public delegate int WriteCallback(IntPtr userdata, IntPtr /*byte**/ data, UIntPtr size);

		// public delegate UIntPtr /*MissingFileResult*/ FileCallback(IntPtr userdata, UIntPtr /*string*/ name);

		// [StructLayout(LayoutKind.Sequential)]
		// public class MissingFileCallback
		// {
		// 	public UIntPtr userdata;
		// 	public FileCallback callback;
		// }
		// [StructLayout(LayoutKind.Sequential)]
		// public class MissingFileResult : CReader
		// {
		// 	public bool writable;
		// }

		/// <summary>
		/// Given a guest executable and a memory layout, create a new host environment.  All data will be immediately consumed from the reader,
		/// which will not be used after this call.
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_create_host(MemoryLayoutTemplate layout, string moduleName, ReadCallback wbx, IntPtr userdata, out ReturnData /*WaterboxHost*/ ret);

		/// <summary>
		/// Tear down a host environment.  If called while the environment is active, will deactivate it first.
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_destroy_host(IntPtr /*WaterboxHost*/ obj, out ReturnData /*void*/ ret);

		/// <summary>
		/// Activate a host environment.  This swaps it into memory and makes it available for use.
		/// Pointers to inside the environment are only valid while active.  Uses a mutex internally
		/// so as to not stomp over other host environments in the same 4GiB slice.
		/// Ignored if host is already active.
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_activate_host(IntPtr /*WaterboxHost*/ obj, out ReturnData /*void*/ ret);

		/// <summary>
		/// Deactivates a host environment, and releases the mutex.
		/// Ignored if host is not active
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_deactivate_host(IntPtr /*WaterboxHost*/ obj, out ReturnData /*void*/ ret);
		/// <summary>
		/// Returns a thunk suitable for calling an exported function from the guest executable.  This pointer is only valid
		/// while the host is active.  A missing proc is not an error and simply returns 0.  The guest function must be,
		/// and the returned callback will be, sysv abi, and will only pass up to 6 int/ptr args and no other arg types.
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_get_proc_addr(IntPtr /*WaterboxHost*/ obj, string name, out ReturnData /*UIntPtr*/ ret);
		/// <summary>
		/// Returns a thunk suitable for calling an arbitrary entry point into the guest executable.  This pointer is only valid
		/// while the host is active.  wbx_get_proc_addr already calls this internally on pointers it returns, so this call is
		/// only needed if the guest exposes callin pointers that aren't named exports (for instance, if a function returns
		/// a pointer to another function).
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_get_callin_addr(IntPtr /*WaterboxHost*/ obj, IntPtr ptr, out ReturnData /*UIntPtr*/ ret);
		/// <summary>
		/// Returns the raw address of a function exported from the guest.  `wbx_get_proc_addr()` is equivalent to
		/// `wbx_get_callin_addr(wbx_get_proc_addr_raw()).  Most things should not use this directly, as the returned
		/// pointer will not have proper stack hygiene and will crash on syscalls from the guest.
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_get_proc_addr_raw(IntPtr /*WaterboxHost*/ obj, string name, out ReturnData /*UIntPtr*/ ret);
		/// <summary>
		/// Returns a function pointer suitable for passing to the guest to allow it to call back while active.
		/// Slot number is an integer that is used to keep pointers consistent across runs:  If the host is loaded
		/// at a different address, and some external function `foo` moves from run to run, things will still work out
		/// in the guest because `foo` was bound to the same slot and a particular slot gives a consistent pointer.
		/// The returned thunk will be, and the callback must be, sysv abi and will only pass up to 6 int/ptr args and no other arg types.
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_get_callback_addr(IntPtr /*WaterboxHost*/ obj, IntPtr callback, int slot, out ReturnData /*UIntPtr*/ ret);
		/// <summary>
		/// Calls the seal operation, which is a one time action that prepares the host to save states.
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_seal(IntPtr /*WaterboxHost*/ obj, out ReturnData /*void*/ ret);

		/// <summary>
		/// Mounts a file in the environment.  All data will be immediately consumed from the reader, which will not be used after this call.
		/// To prevent nondeterminism, adding and removing files is very limited WRT savestates.  Every file added must either exist
		/// in every savestate, or never appear in any savestates.  All savestateable files must be added in the same order for every run.
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_mount_file(IntPtr /*WaterboxHost*/ obj, string name, ReadCallback reader, IntPtr userdata, bool writable, out ReturnData /*void*/ ret);

		/// <summary>
		/// Remove a file previously added.  Writer is optional; if provided, the contents of the file at time of removal will be dumped to it.
		/// It is an error to remove a file which is currently open in the guest.
		/// If the file has been used in savestates, it does not make sense to remove it here, but nothing will stop you.
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_unmount_file(IntPtr /*WaterboxHost*/ obj, string name, WriteCallback writer, IntPtr userdata, out ReturnData /*void*/ ret);

#if false
		/// <summary>
		/// Set (or clear, with None) a callback to be called whenever the guest tries to load a nonexistant file.
		/// The callback will be provided with the name of the requested load, and can either return null to signal the waterbox
		/// to return ENOENT to the guest, or a struct to immediately load that file.  You may not call any wbx methods
		/// in the callback.  If the MissingFileResult is provided, it will be consumed immediately and will have the same effect
		/// as wbx_mount_file().  You may free resources associated with the MissingFileResult whenever control next returns to your code.
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_set_missing_file_callback(IntPtr /*WaterboxHost*/ obj, MissingFileCallback mfc_o);
#endif

		/// <summary>
		/// Save state.  Must not be called before seal.  Must not be called with any writable files mounted.
		/// Must always be called with the same sequence and contents of readonly files.
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_save_state(IntPtr /*WaterboxHost*/ obj, WriteCallback writer, IntPtr userdata, out ReturnData /*void*/ ret);

		/// <summary>
		/// Load state.  Must not be called before seal.  Must not be called with any writable files mounted.
		/// Must always be called with the same sequence and contents of readonly files that were in the save state.
		/// Must be called with the same wbx executable and memory layout as in the savestate.
		/// Errors generally poison the environment; sorry!
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_load_state(IntPtr /*WaterboxHost*/ obj, ReadCallback reader, IntPtr userdata, out ReturnData /*void*/ ret);

		/// <summary>
		/// Control whether the host automatically evicts blocks from memory when they are not active.  For the best performance,
		/// this should be set to false.  Set to true to help catch dangling pointer issues.  Will be ignored (and forced to true)
		/// if waterboxhost was built in debug mode.  This is a single global setting.
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_set_always_evict_blocks(bool val);

		/// <summary>
		/// Retrieve the number of pages of guest memory that this host is tracking
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_get_page_len(IntPtr /*WaterboxHost*/ obj, out ReturnData /*UIntPtr*/ ret);

		/// <summary>
		/// Retrieve basic information for a tracked guest page.  Index should be in 0..wbx_get_page_len().
		/// 1 - readable, implies allocated
		/// 2 - writable
		/// 4 - executable
		/// 0x10 - stack
		/// 0x20 - allocated but not readable (guest-generated "guard")
		/// 0x40 - invisible
		/// 0x80 - dirty
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void wbx_get_page_data(IntPtr /*WaterboxHost*/ obj, UIntPtr index, out ReturnData /*byte*/ ret);
	}
}
