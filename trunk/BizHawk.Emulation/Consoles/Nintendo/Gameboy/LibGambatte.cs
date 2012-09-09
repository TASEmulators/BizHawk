using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Consoles.Gambatte
{
	/// <summary>
	/// static bindings into libgambatte.dll
	/// </summary>
	public static class LibGambatte
	{
		/// <summary>
		/// 
		/// </summary>
		/// <returns>opaque state pointer</returns>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr gambatte_create();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_destroy(IntPtr core);

		[Flags]
		public enum LoadFlags : uint
		{
			/// <summary>Treat the ROM as not having CGB support regardless of what its header advertises</summary>
			FORCE_DMG = 1,
			/// <summary>Use GBA intial CPU register values when in CGB mode.</summary>
			GBA_CGB = 2,
			/// <summary>Use heuristics to detect and support some multicart MBCs disguised as MBC1.</summary>
			MULTICART_COMPAT = 4 
		}

		/// <summary>
		/// Load ROM image.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="filename">Path to rom image file. Typically a .gbc, .gb, or .zip-file (if zip-support is compiled in).</param>
		/// <param name="flags">ORed combination of LoadFlags.</param>
		/// <returns>0 on success, negative value on failure.</returns>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int gambatte_load(IntPtr core, string filename, LoadFlags flags);

		/// <summary>
		/// Emulates until at least 'samples' stereo sound samples are produced in the supplied buffer,
		/// or until a video frame has been drawn.
		/// 
		/// There are 35112 stereo sound samples in a video frame.
		/// May run for up to 2064 stereo samples too long.
		/// A stereo sample consists of two native endian 2s complement 16-bit PCM samples,
		/// with the left sample preceding the right one.
		/// 
		/// Returns early when a new video frame has finished drawing in the video buffer,
		/// such that the caller may update the video output before the frame is overwritten.
		/// The return value indicates whether a new video frame has been drawn, and the
		/// exact time (in number of samples) at which it was drawn.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="videobuf">160x144 RGB32 (native endian) video frame buffer or null</param>
		/// <param name="pitch">distance in number of pixels (not bytes) from the start of one line to the next in videoBuf.</param>
		/// <param name="soundbuf">buffer with space >= samples + 2064</param>
		/// <param name="samples">in: number of stereo samples to produce, out: actual number of samples produced</param>
		/// <returns>sample number at which the video frame was produced. -1 means no frame was produced.</returns>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int gambatte_runfor(IntPtr core, uint[] videobuf, int pitch, short[] soundbuf, ref uint samples);

		/// <summary>
		/// Reset to initial state.
		/// Equivalent to reloading a ROM image, or turning a Game Boy Color off and on again.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_reset(IntPtr core);


		/// <summary>
		/// palette type for gambatte_setdmgpalettecolor
		/// </summary>
		public enum PalType : uint
		{
			BG_PALETTE = 0,
			SP1_PALETTE = 1,
			SP2_PALETTE = 2
		};

		/// <summary>
		/// 
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="palnum">0 <= palNum < 3. One of BG_PALETTE, SP1_PALETTE and SP2_PALETTE.</param>
		/// <param name="colornum">0 <= colorNum < 4</param>
		/// <param name="rgb32"></param>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_setdmgpalettecolor(IntPtr core, PalType palnum, uint colornum, uint rgb32);

		/// <summary>
		/// combination of button flags used by the input callback
		/// </summary>
		[Flags]
		public enum Buttons
		{ 
			A = 0x01,
			B = 0x02,
			SELECT = 0x04,
			START = 0x08,
			RIGHT = 0x10,
			LEFT = 0x20,
			UP = 0x40,
			DOWN = 0x80
		}

		/// <summary>
		/// Sets the callback used for getting input state.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="getinput"></param>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_setinputgetter(IntPtr core, Func<Buttons> getinput);

		/// <summary>
		/// Sets the directory used for storing save data. The default is the same directory as the ROM Image file.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="sdir"></param>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_setsavedir(IntPtr core, string sdir);

		/// <summary>
		/// Returns true if the currently loaded ROM image is treated as having CGB support.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <returns></returns>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool gambatte_iscgb(IntPtr core);

		/// <summary>
		/// Returns true if a ROM image is loaded.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <returns></returns>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool gambatte_isloaded(IntPtr core);

		/// <summary>
		/// Writes persistent cartridge data to disk. Done implicitly on ROM close.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_savesavedata(IntPtr core);

		/// <summary>
		/// Saves emulator state to the state slot selected with gambatte_selectstate().
		/// The data will be stored in the directory given by gambatte_setsavedir().
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="videobuf">160x144 RGB32 (native endian) video frame buffer or 0. Used for saving a thumbnail.</param>
		/// <param name="pitch">distance in number of pixels (not bytes) from the start of one line to the next in videoBuf.</param>
		/// <returns></returns>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool gambatte_savestate(IntPtr core, uint[] videobuf, int pitch);

		/// <summary>
		/// Loads emulator state from the state slot selected with selectState().
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <returns>success</returns>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool gambatte_loadstate(IntPtr core);

		/// <summary>
		/// Saves emulator state to the file given by 'filepath'.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="videobuf">160x144 RGB32 (native endian) video frame buffer or 0. Used for saving a thumbnail.</param>
		/// <param name="pitch">distance in number of pixels (not bytes) from the start of one line to the next in videoBuf.</param>
		/// <param name="filepath"></param>
		/// <returns>success</returns>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool gambatte_savestate_file(IntPtr core, uint[] videobuf, int pitch, string filepath);

		/// <summary>
		/// Loads emulator state from the file given by 'filepath'.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="filepath"></param>
		/// <returns>success</returns>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool gambatte_loadstate_file(IntPtr core, string filepath);

		/// <summary>
		/// Selects which state slot to save state to or load state from.
		/// There are 10 such slots, numbered from 0 to 9 (periodically extended for all n).
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="n"></param>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_selectstate(IntPtr core, int n);

		/// <summary>
		/// Current state slot selected with selectState(). Returns a value between 0 and 9 inclusive.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <returns></returns>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int gambatte_currentstate(IntPtr core);

		/// <summary>
		/// ROM header title of currently loaded ROM image.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <returns></returns>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern string gambatte_romtitle(IntPtr core);

		/// <summary>
		/// Set Game Genie codes to apply to currently loaded ROM image. Cleared on ROM load.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="codes">Game Genie codes in format HHH-HHH-HHH;HHH-HHH-HHH;... where H is [0-9]|[A-F]</param>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_setgamegenie(IntPtr core, string codes);

		/// <summary>
		/// Game Shark codes to apply to currently loaded ROM image. Cleared on ROM load.
		/// </summary>
		/// <param name="core">opaque state pointer</param>
		/// <param name="codes">Game Shark codes in format 01HHHHHH;01HHHHHH;... where H is [0-9]|[A-F]</param>
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_setgameshark(IntPtr core, string codes);

	}
}
