using System;
using System.Text;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// A resource representing a disc opened and mounted through the MednaDisc component.
	/// Does not attempt to virtually present the disc as a BizHawk disc - that will be the
	/// responsibility of the user of this code
	/// </summary>
	public unsafe class MednaDisc : IDisposable
	{
		public MednaDisc(string pathToDisc)
		{
			if (!IsLibraryAvailable)
				throw new InvalidOperationException("MednaDisc library is not available!");

			handle = mednadisc_LoadCD(pathToDisc);
			if (handle == IntPtr.Zero)
				throw new InvalidOperationException("Failed to load MednaDisc: " + pathToDisc);

			//read the mednafen toc
			TOCTracks = new MednadiscTOCTrack[101];
			fixed (MednadiscTOCTrack* _tracks = &TOCTracks[0])
				fixed(MednadiscTOC* _toc = &TOC)
					mednadisc_ReadTOC(handle, _toc, _tracks);

			//leave the disc open until this is disposed so we can read sectors from it
		}

		IntPtr handle;

		public MednadiscTOC TOC;
		public MednadiscTOCTrack[] TOCTracks;

		[ThreadStatic] static byte[] buf2442 = new byte[2448];
		[ThreadStatic] static byte[] buf96 = new byte[96];


		public void Read_2442(int LBA, byte[] buffer, int offset)
		{
			//read directly into the target buffer
			fixed(byte* pBuffer = &buffer[0])
				mednadisc_ReadSector(handle, LBA, pBuffer + offset);
		}

		//public void ReadSubcodeDeinterleaved(int LBA, byte[] buffer, int offset)
		//{
		//  fixed (byte* pBuffer = buf2442)
		//    mednadisc_ReadSector(handle, LBA, pBuffer);
		//  SubcodeUtils.Deinterleave(buf2442, 2352, buffer, offset);
		//}

		//public void ReadSubcodeChannel(int LBA, int number, byte[] buffer, int offset)
		//{
		//  fixed (byte* pBuffer = buf2442)
		//    mednadisc_ReadSector(handle, LBA, pBuffer);
		//  SubcodeUtils.Deinterleave(buf2442, 2352, buf96, 0);
		//  for (int i = 0; i < 12; i++)
		//    buffer[offset + i] = buf96[number * 12 + i];
		//}

		//public void Read_2352(int LBA, byte[] buffer, int offset)
		//{
		//  fixed (byte* pBuffer = buf2442)
		//    mednadisc_ReadSector(handle, LBA, pBuffer);
		//  Buffer.BlockCopy(buf2442, 0, buffer, offset, 2352);
		//}

		//public void Read_2048(int LBA, byte[] buffer, int offset)
		//{
		//  //this depends on CD-XA mode and such. so we need to read the mode bytes
		//  //HEY!!!!!! SHOULD THIS BE DONE BASED ON THE CLAIMED TRACK TYPE, OR ON WHATS IN THE SECTOR?
		//  //this is kind of a function of the CD reader.. it's not clear how this function should work.
		//  //YIKES!!!!!!!!!!!!!!
		//  //well, we need to scrutinize it for CCD files anyway, so...
		//  //this sucks.

		//  fixed (byte* pBuffer = buf2442)
		//    mednadisc_ReadSector(handle, LBA, pBuffer);

		//  byte mode = buf2442[15];
		//  if (mode == 1)
		//    Buffer.BlockCopy(buf2442, 16, buffer, offset, 2048);
		//  else
		//    Buffer.BlockCopy(buf2442, 24, buffer, offset, 2048); //PSX assumptions about CD-XA.. BAD BAD BAD
		//}

		static void CheckLibrary()
		{
			IntPtr lib = LoadLibrary("mednadisc.dll");
			if (lib == IntPtr.Zero)
			{
				_IsLibraryAvailable = false;
				return;
			}
			IntPtr addr = GetProcAddress(lib, "mednadisc_LoadCD");
			FreeLibrary(lib);
			if (addr == IntPtr.Zero)
			{
				_IsLibraryAvailable = false;
			}
			_IsLibraryAvailable = true;
		}

		static MednaDisc()
		{
			CheckLibrary();
		}

		static bool _IsLibraryAvailable;
		public static bool IsLibraryAvailable { get { return _IsLibraryAvailable; } }

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
		};

		[StructLayout(LayoutKind.Explicit)]
		public struct MednadiscTOCTrack
		{
			[FieldOffset(0)] public byte adr;
			[FieldOffset(1)] public byte control;
			[FieldOffset(4)] public uint lba;

			//can't be a bool due to marshalling...
			[FieldOffset(8)] public byte _validByte;

			public bool Valid { get { return _validByte != 0; } }
		};

		[DllImport("kernel32.dll")]
		static extern IntPtr LoadLibrary(string dllToLoad);
		[DllImport("kernel32.dll")]
		static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
		[DllImport("kernel32.dll")]
		static extern bool FreeLibrary(IntPtr hModule);

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