using System.Runtime.InteropServices;

using BizHawk.Common;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// A resource representing a disc opened and mounted through the MednaDisc component.
	/// Does not attempt to virtually present the disc as a BizHawk disc - that will be the
	/// responsibility of the user of this code
	/// </summary>
	public unsafe class MednaDisc : IDisposable
	{
		/// <exception cref="InvalidOperationException"><see cref="IsLibraryAvailable"/> is <see langword="false"/> (could not load <c>mednadisc.dll</c>), or unmanaged call failed</exception>
		public MednaDisc(string pathToDisc)
		{
			if (!IsLibraryAvailable)
				throw new InvalidOperationException($"{nameof(MednaDisc)} library is not available!");

			handle = mednadisc_LoadCD(pathToDisc);
			if (handle == IntPtr.Zero)
				throw new InvalidOperationException($"Failed to load {nameof(MednaDisc)}: {pathToDisc}");

			//read the mednafen toc
			TOCTracks = new MednadiscTOCTrack[101];
			fixed (MednadiscTOCTrack* _tracks = &TOCTracks[0])
				fixed(MednadiscTOC* _toc = &TOC)
					mednadisc_ReadTOC(handle, _toc, _tracks);

			//leave the disc open until this is disposed so we can read sectors from it
		}

		private readonly IntPtr handle;

		public MednadiscTOC TOC;
		public MednadiscTOCTrack[] TOCTracks;

		[ThreadStatic] private static byte[] buf2442 = new byte[2448];
		[ThreadStatic] private static byte[] buf96 = new byte[96];


		public void Read_2442(int LBA, byte[] buffer, int offset)
		{
			//read directly into the target buffer
			fixed(byte* pBuffer = &buffer[0])
				_ = mednadisc_ReadSector(handle, LBA, pBuffer + offset);
		}

#if false
		public void ReadSubcodeDeinterleaved(int LBA, byte[] buffer, int offset)
		{
			fixed (byte* pBuffer = buf2442)
				mednadisc_ReadSector(handle, LBA, pBuffer);
			SynthUtils.DeinterleaveSubcode(buf2442, 2352, buffer, offset);
		}

		public void ReadSubcodeChannel(int LBA, int number, byte[] buffer, int offset)
		{
			fixed (byte* pBuffer = buf2442)
				mednadisc_ReadSector(handle, LBA, pBuffer);
			SynthUtils.DeinterleaveSubcode(buf2442, 2352, buf96, 0);
			for (int i = 0; i < 12; i++)
				buffer[offset + i] = buf96[number * 12 + i];
		}

		public void Read_2352(int LBA, byte[] buffer, int offset)
		{
			fixed (byte* pBuffer = buf2442)
				mednadisc_ReadSector(handle, LBA, pBuffer);
			Buffer.BlockCopy(buf2442, 0, buffer, offset, 2352);
		}

		public void Read_2048(int LBA, byte[] buffer, int offset)
		{
			//this depends on CD-XA mode and such. so we need to read the mode bytes
			//HEY!!!!!! SHOULD THIS BE DONE BASED ON THE CLAIMED TRACK TYPE, OR ON WHATS IN THE SECTOR?
			//this is kind of a function of the CD reader.. it's not clear how this function should work.
			//YIKES!!!!!!!!!!!!!!
			//well, we need to scrutinize it for CCD files anyway, so...
			//this sucks.

			fixed (byte* pBuffer = buf2442)
				mednadisc_ReadSector(handle, LBA, pBuffer);

			byte mode = buf2442[15];
			if (mode == 1)
				Buffer.BlockCopy(buf2442, 16, buffer, offset, 2048);
			else
				Buffer.BlockCopy(buf2442, 24, buffer, offset, 2048); //PSX assumptions about CD-XA.. BAD BAD BAD
		}
#endif

		private static void CheckLibrary()
		{
			var lib = OSTailoredCode.LinkedLibManager.LoadOrZero("mednadisc.dll");
			_IsLibraryAvailable = lib != IntPtr.Zero
				&& OSTailoredCode.LinkedLibManager.GetProcAddrOrZero(lib, "mednadisc_LoadCD") != IntPtr.Zero;
			if (lib != IntPtr.Zero) OSTailoredCode.LinkedLibManager.FreeByPtr(lib);
		}

		static MednaDisc()
		{
			CheckLibrary();
		}

		private static bool _IsLibraryAvailable;
		public static bool IsLibraryAvailable => _IsLibraryAvailable;

		public void Dispose()
		{
			if(handle == IntPtr.Zero) return;
			mednadisc_CloseCD(handle);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MednadiscTOC
		{
			public byte first_track;
			public byte last_track;
			public byte disc_type;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct MednadiscTOCTrack
		{
			[FieldOffset(0)] public byte adr;
			[FieldOffset(1)] public byte control;
			[FieldOffset(4)] public uint lba;

			//can't be a bool due to marshalling...
			[FieldOffset(8)] public byte _validByte;

			public bool Valid => _validByte != 0;
		}

		[DllImport("mednadisc.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr mednadisc_LoadCD(string path);

		[DllImport("mednadisc.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int mednadisc_ReadSector(IntPtr disc, int lba, byte* buf2448);

		[DllImport("mednadisc.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void mednadisc_CloseCD(IntPtr disc);

		[DllImport("mednadisc.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void mednadisc_ReadTOC(IntPtr disc, MednadiscTOC* toc, MednadiscTOCTrack* tracks101);
	}
}