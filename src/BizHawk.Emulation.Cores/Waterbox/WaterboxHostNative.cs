using System;
using System.IO;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public abstract class WaterboxHostNative
	{
		[StructLayout(LayoutKind.Sequential)]
		public class ReturnData
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
			public byte[] ErrorMessage = new byte[1024];
			public IntPtr Data;

			public unsafe IntPtr GetDataOrThrow()
			{
				if (ErrorMessage[0] != 0)
				{
					fixed(byte* p = ErrorMessage)
						throw new InvalidOperationException(Mershul.PtrToStringUtf8((IntPtr)p));
				}
				return Data;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public class MemoryLayoutTemplate
		{
			/// Absolute pointer to the start of the mapped space
			public UIntPtr start;
			/// Memory space for the elf executable.  The elf must be non-relocatable and
			/// all loaded segments must fit within [start..start + elf_size]
			public UIntPtr elf_size;
			/// Memory space to serve brk(2)
			public UIntPtr sbrk_size;
			/// Memory space to serve alloc_sealed(3)
			public UIntPtr sealed_size;
			/// Memory space to serve alloc_invisible(3)
			public UIntPtr invis_size;
			/// Memory space to serve alloc_plain(3)
			public UIntPtr plain_size;
			/// Memory space to serve mmap(2) and friends.
			/// Calls without MAP_FIXED or MREMAP_FIXED will be placed in this area.
			/// TODO: Are we allowing fixed calls to happen anywhere in the block?
			public UIntPtr mmap_size;
		}
		public delegate IntPtr StreamCallback(IntPtr userdata, IntPtr /*byte**/ data, UIntPtr size);
		public delegate UIntPtr /*MissingFileResult*/ FileCallback(IntPtr userdata, UIntPtr /*string*/ name);
		[StructLayout(LayoutKind.Sequential)]
		public unsafe class CWriter : IDisposable
		{
			/// will be passed to callback
			public IntPtr userdata;
			/// write bytes.  Return number of bytes written on success, or < 0 on failure.
			/// Permitted to write less than the provided number of bytes.
			public StreamCallback callback;
			public static CWriter FromStream(Stream stream)
			{
				var ss = SpanStream.GetOrBuild(stream);
				return new CWriter
				{
					// TODO: spans
					callback = (_unused, data, size) =>
					{
						try
						{
							var count = (int)size;
							ss.Write(new ReadOnlySpan<byte>((void*)data, count));
							return Z.SS(count);
						}
						catch
						{
							return Z.SS(-1);
						}
					}
				};
			}
			public void Dispose()
			{
				// Dummy disposable impl for release mode GC interop shens, or something
				if (userdata != IntPtr.Zero)
					throw new Exception();
			}
		}
		[StructLayout(LayoutKind.Sequential)]
		public unsafe class CReader : IDisposable
		{
			/// will be passed to callback
			public IntPtr userdata;
			/// Read bytes into the buffer.  Return number of bytes read on success, or < 0 on failure.
			/// permitted to read less than the provided buffer size, but must always read at least 1
			/// byte if EOF is not reached.  If EOF is reached, should return 0.
			public StreamCallback callback;
			public static CReader FromStream(Stream stream)
			{
				var ss = SpanStream.GetOrBuild(stream);
				return new CReader
				{
					// TODO: spans
					callback = (_unused, data, size) =>
					{
						try
						{
							var count = (int)size;
							var n = ss.Read(new Span<byte>((void*)data, count));
							return Z.SS(n);
						}
						catch
						{
							return Z.SS(-1);
						}
					}
				};
			}
			public void Dispose()
			{
				// Dummy disposable impl for release mode GC interop shens, or something
				if (userdata != IntPtr.Zero)
					throw new Exception();
			}
		}
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

		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void wbx_create_host([In]MemoryLayoutTemplate layout, string moduleName, [In]CReader wbx, [Out]ReturnData /*WaterboxHost*/ ret);
		/// Tear down a host environment.  May not be called while the environment is active.
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void wbx_destroy_host(IntPtr /*WaterboxHost*/ obj, [Out]ReturnData /*void*/ ret);
		/// Activate a host environment.  This swaps it into memory and makes it available for use.
		/// Pointers to inside the environment are only valid while active.  Uses a mutex internally
		/// so as to not stomp over other host environments in the same 4GiB slice.
		/// Returns a pointer to the activated object, used to do most other functions.
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void wbx_activate_host(IntPtr /*WaterboxHost*/ obj, [Out]ReturnData /*ActivatedWaterboxHost*/ ret);
		/// Deactivates a host environment, and releases the mutex.
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void wbx_deactivate_host(IntPtr /*ActivatedWaterboxHost*/ obj, [Out]ReturnData /*void*/ ret);
		/// Returns the address of an exported function from the guest executable.  This pointer is only valid
		/// while the host is active.  A missing proc is not an error and simply returns 0.
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void wbx_get_proc_addr(IntPtr /*ActivatedWaterboxHost*/ obj, string name, [Out]ReturnData /*UIntPtr*/ ret);
		/// Calls the seal operation, which is a one time action that prepares the host to save states.
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void wbx_seal(IntPtr /*ActivatedWaterboxHost*/ obj, [Out]ReturnData /*void*/ ret);
		/// Mounts a file in the environment.  All data will be immediately consumed from the reader, which will not be used after this call.
		/// To prevent nondeterminism, adding and removing files is very limited WRT savestates.  If a file is writable, it must never exist
		/// when save_state is called, and can only be used for transient operations.  If a file is readable, it can appear in savestates,
		/// but it must exist in every savestate and the exact sequence of add_file calls must be consistent from savestate to savestate.
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void wbx_mount_file(IntPtr /*ActivatedWaterboxHost*/ obj, string name, [In]CReader reader, bool writable, [Out]ReturnData /*void*/ ret);
		/// Remove a file previously added.  Writer is optional; if provided, the contents of the file at time of removal will be dumped to it.
		/// It is an error to remove a file which is currently open in the guest.
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void wbx_unmount_file(IntPtr /*ActivatedWaterboxHost*/ obj, string name, [In]CWriter writer, [Out]ReturnData /*void*/ ret);
		/// Set (or clear, with None) a callback to be called whenever the guest tries to load a nonexistant file.
		/// The callback will be provided with the name of the requested load, and can either return null to signal the waterbox
		/// to return ENOENT to the guest, or a struct to immediately load that file.  You may not call any wbx methods
		/// in the callback.  If the MissingFileResult is provided, it will be consumed immediately and will have the same effect
		/// as wbx_mount_file().  You may free resources associated with the MissingFileResult whenever control next returns to your code.
		// [BizImport(CallingConvention.Cdecl, Compatibility = true)]
		// public abstract void wbx_set_missing_file_callback(IntPtr /*ActivatedWaterboxHost*/ obj, MissingFileCallback mfc_o);
		/// Save state.  Must not be called before seal.  Must not be called with any writable files mounted.
		/// Must always be called with the same sequence and contents of readonly files.
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void wbx_save_state(IntPtr /*ActivatedWaterboxHost*/ obj, [In]CWriter writer, [Out]ReturnData /*void*/ ret);
		/// Load state.  Must not be called before seal.  Must not be called with any writable files mounted.
		/// Must always be called with the same sequence and contents of readonly files that were in the save state.
		/// Must be called with the same wbx executable and memory layout as in the savestate.
		/// Errors generally poison the environment; sorry!
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void wbx_load_state(IntPtr /*ActivatedWaterboxHost*/ obj, [In]CReader reader, [Out]ReturnData /*void*/ ret);
	}
}
