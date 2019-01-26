using System;
using System.Runtime.InteropServices;

using BizHawk.Common.BizInvoke;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public abstract class LibQuickNES
	{
		/// <summary>
		/// setup extra mappers.  should be done before anything else
		/// </summary>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void qn_setup_mappers();
		/// <summary>
		/// create a new quicknes context
		/// </summary>
		/// <returns>NULL on failure</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr qn_new();
		/// <summary>
		/// destroy a quicknes context
		/// </summary>
		/// <param name="e">context previously returned from qn_new()</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void qn_delete(IntPtr e);
		/// <summary>
		/// load an ines file
		/// </summary>
		/// <param name="e">context</param>
		/// <param name="data">file</param>
		/// <param name="length">length of file</param>
		/// <returns></returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr qn_loadines(IntPtr e, byte[] data, int length);
		/// <summary>
		/// set audio sample rate
		/// </summary>
		/// <param name="e">context</param>
		/// <param name="rate">hz</param>
		/// <returns>string error</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr qn_set_sample_rate(IntPtr e, int rate);
		/// <summary>
		/// emulate a single frame
		/// </summary>
		/// <param name="e">context</param>
		/// <param name="pad1">pad 1 input</param>
		/// <param name="pad2">pad 2 input</param>
		/// <returns>string error</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr qn_emulate_frame(IntPtr e, int pad1, int pad2);
		/// <summary>
		/// blit to rgb32
		/// </summary>
		/// <param name="e">Context</param>
		/// <param name="dest">rgb32 256x240 packed</param>
		/// <param name="colors">rgb32 colors, 512 of them</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void qn_blit(IntPtr e, int[] dest, int[] colors, int cropleft, int croptop, int cropright, int cropbottom);
		/// <summary>
		/// get quicknes's default palette
		/// </summary>
		/// <returns>1536 bytes suitable for qn_blit</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr qn_get_default_colors();
		/// <summary>
		/// get number of times joypad was read in most recent frame
		/// </summary>
		/// <param name="e">context</param>
		/// <returns>0 means lag</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract int qn_get_joypad_read_count(IntPtr e);
		/// <summary>
		/// get audio info for most recent frame
		/// </summary>
		/// <param name="e">context</param>
		/// <param name="sample_count">number of samples actually created</param>
		/// <param name="chan_count">1 for mono, 2 for stereo</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void qn_get_audio_info(IntPtr e, ref int sample_count, ref int chan_count);
		/// <summary>
		/// get audio for most recent frame.  must not be called more than once per frame!
		/// </summary>
		/// <param name="e">context</param>
		/// <param name="dest">sample buffer</param>
		/// <param name="max_samples">length to read into sample buffer</param>
		/// <returns>length actually read</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract int qn_read_audio(IntPtr e, short[] dest, int max_samples);
		/// <summary>
		/// reset the console
		/// </summary>
		/// <param name="e">context</param>
		/// <param name="hard">true for powercycle, false for reset button</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void qn_reset(IntPtr e, bool hard);
		/// <summary>
		/// get the required byte size of a savestate
		/// </summary>
		/// <param name="e">context</param>
		/// <param name="size">size is returned</param>
		/// <returns>string error</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr qn_state_size(IntPtr e, ref int size);
		/// <summary>
		/// save state to buffer
		/// </summary>
		/// <param name="e">context</param>
		/// <param name="dest">buffer</param>
		/// <param name="size">length of buffer</param>
		/// <returns>string error</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr qn_state_save(IntPtr e, byte[] dest, int size);
		/// <summary>
		/// load state from buffer
		/// </summary>
		/// <param name="e">context</param>
		/// <param name="src">buffer</param>
		/// <param name="size">length of buffer</param>
		/// <returns>string error</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr qn_state_load(IntPtr e, byte[] src, int size);
		/// <summary>
		/// query battery ram state
		/// </summary>
		/// <param name="e">context</param>
		/// <returns>true if battery backup sram exists</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract bool qn_has_battery_ram(IntPtr e);
		/// <summary>
		/// query battery ram size
		/// </summary>
		/// <param name="e">context</param>
		/// <param name="size">size is returned</param>
		/// <returns>string error</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr qn_battery_ram_size(IntPtr e, ref int size);
		/// <summary>
		/// save battery ram to buffer
		/// </summary>
		/// <param name="e">context</param>
		/// <param name="dest">buffer</param>
		/// <param name="size">size</param>
		/// <returns>string error</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr qn_battery_ram_save(IntPtr e, byte[] dest, int size);
		/// <summary>
		/// load battery ram from buffer
		/// </summary>
		/// <param name="e">context</param>
		/// <param name="src">buffer</param>
		/// <param name="size">size</param>
		/// <returns>string error</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr qn_battery_ram_load(IntPtr e, byte[] src, int size);
		/// <summary>
		/// clear battery ram
		/// </summary>
		/// <param name="e">context</param>
		/// <returns>string error</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr qn_battery_ram_clear(IntPtr e);
		/// <summary>
		/// set sprite limit; does not affect emulation
		/// </summary>
		/// <param name="e">context</param>
		/// <param name="n">0 to hide, 8 for normal, 64 for all</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void qn_set_sprite_limit(IntPtr e, int n);
		/// <summary>
		/// get memory area for debugging
		/// </summary>
		/// <param name="e">Context</param>
		/// <param name="which"></param>
		/// <param name="data"></param>
		/// <param name="size"></param>
		/// <param name="writable"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract bool qn_get_memory_area(IntPtr e, int which, ref IntPtr data, ref int size, ref bool writable, ref IntPtr name);
		/// <summary>
		/// peek the system bus
		/// </summary>
		/// <param name="e">Context</param>
		/// <param name="addr">0000:ffff, but non-ram/rom addresses won't work</param>
		/// <returns></returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract byte qn_peek_prgbus(IntPtr e, int addr);
		/// <summary>
		/// poke the system bus
		/// </summary>
		/// <param name="e">Context</param>
		/// <param name="addr">0000:ffff, but non-ram/rom addresses won't work</param>
		/// <param name="val"></param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void qn_poke_prgbus(IntPtr e, int addr, byte val);
		/// <summary>
		/// get internal registers
		/// </summary>
		/// <param name="e">Context</param>
		/// <param name="dest">a, x, y, sp, pc, p</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void qn_get_cpuregs(IntPtr e, [Out] int[] dest);
		/// <summary>
		/// get the mapper that's loaded
		/// </summary>
		/// <param name="e">Context</param>
		/// <param name="number">recieves mapper number</param>
		/// <returns>mapper name</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr qn_get_mapper(IntPtr e, ref int number);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void TraceCallback(IntPtr data);

		/// <summary>
		/// set a trace callback to be run on each cycle
		/// </summary>
		/// <param name="e">Context</param>
		/// <param name="cb"></param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void qn_set_tracecb(IntPtr e, TraceCallback cb);



		[BizImport(CallingConvention.Cdecl)]
		public abstract byte qn_get_reg2000(IntPtr e);
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr qn_get_palmem(IntPtr e);
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr qn_get_oammem(IntPtr e);
		[BizImport(CallingConvention.Cdecl)]
		public abstract byte qn_peek_ppu(IntPtr e, int addr);
		[BizImport(CallingConvention.Cdecl)]
		public abstract void qn_peek_ppubus(IntPtr e, byte[] dest);

		/// <summary>
		/// handle "string error" as returned by some quicknes functions
		/// </summary>
		/// <param name="p"></param>
		public static void ThrowStringError(IntPtr p)
		{
			if (p == IntPtr.Zero)
				return;
			string s = Marshal.PtrToStringAnsi(p);
			if (s == "Unsupported mapper"
				|| s == "Not an iNES file" // Not worth making a new exception for the iNES error, they ultimately are the same problem
				|| s == " truncated file" // This is a garbage rom not worth anyone's time but at least NesHawk handles these better, and these occur before the core has a chance to assess an unsupported mapper
				) 
			{
				throw new Emulation.Common.UnsupportedGameException("Quicknes unsupported mapper");
			}
			else
			{
				throw new InvalidOperationException("LibQuickNES error: " + s);
			}
		}
	}
}
