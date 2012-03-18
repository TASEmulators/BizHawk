using System;
using System.Security;
using System.Runtime.InteropServices;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Reflection.Emit;
using System.Threading;
using BizHawk.DiscSystem;

namespace BizHawk
{
	/// <summary>
	/// represents an external core's interface to a disc
	/// </summary>
	public class DiscInterface : ExternalCore
	{
		public DiscInterface(IExternalCoreAccessor accessor)
			: base(accessor)
		{
			UnmanagedOpaque = QueryCoreCall<Func<IntPtr, IntPtr>>("DiscInterface.Construct")(ManagedOpaque);

			SetFp("Dispose", new disposeDelegate(Dispose));
			SetFp("GetNumSessions", new GetNumSessionsDelegate(GetNumSessions));
			SetFp("GetNumTracks", new GetNumTracksDelegate(GetNumTracks));
			SetFp("GetTrack", new GetTrackDelegate(GetTrack));
		}

		bool disposed = false;
		public override void Dispose()
		{
			if (disposed) return;
			disposed = true;

			QueryCoreCall<Action>("DiscInterface.Delete")();

			base.Dispose();
		}

		public DiscHopper DiscHopper;

		void SetFp(string name, Delegate del)
		{
			QueryCoreCall<Action<string, IntPtr>>("DiscInterface.Set_fp")(name, ExportDelegate(del));
		}

		struct TrackInfo
		{
			public ETrackType TrackType;
			public int length_lba;
			public int start_lba;
		}

		int GetNumSessions()
		{
			return DiscHopper.CurrentDisc.ReadTOC().Sessions.Count;
		}

		int GetNumTracks(int session)
		{
			return DiscHopper.CurrentDisc.ReadTOC().Sessions[session].Tracks.Count;
		}

		TrackInfo GetTrack(int session, int track)
		{
			var ti = new TrackInfo();
			var toc_track = DiscHopper.CurrentDisc.ReadTOC().Sessions[session].Tracks[track];
			ti.TrackType = toc_track.TrackType;
			ti.length_lba = toc_track.length_aba;
			return ti;
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void disposeDelegate();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate int GetNumSessionsDelegate();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate int GetNumTracksDelegate(int session);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate TrackInfo GetTrackDelegate(int session, int track);
	}


	public class EmuFile : ExternalCore
	{
		public EmuFile(IExternalCoreAccessor accessor)
			: base(accessor)
		{
			UnmanagedOpaque = QueryCoreCall<Func<IntPtr, IntPtr>>("EmuFile.Construct")(ManagedOpaque);
			
			SetFp("fgetc", new fgetcDelegate(fgetc));
			SetFp("fread", new freadDelegate(fread));
			SetFp("fwrite", new fwriteDelegate(fwrite));
			SetFp("fseek", new fseekDelegate(fseek));
			SetFp("ftell", new ftellDelegate(ftell));
			SetFp("size", new sizeDelegate(size));
			SetFp("dispose", new disposeDelegate(Dispose));
		}

		void SetFp(string name, Delegate del)
		{
			QueryCoreCall<Action<string, IntPtr>>("EmuFile.Set_fp")(name, ExportDelegate(del));
		}

		public Stream BaseStream { get; set; }

		//do we want to have a finalizer? not sure.
		bool disposed = false;
		public override void Dispose()
		{
			if (disposed) return;
			disposed = true;

			//we will call Delete in the c++ side, which will delete the object, and cause Dispose() to get called.
			//but, Dispose() can never be called again due to setting the flag above
			QueryCoreCall<Action>("EmuFile.Delete")();

			//do we always want to do this? not sure. but usually.
			BaseStream.Dispose();

			base.Dispose();
		}

		int fgetc()
		{
			return BaseStream.ReadByte();
		}

		IntPtr fread(
			[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
			byte[] ptr,
			IntPtr bytes)
		{
			long len = bytes.ToInt64();
			if (len >= int.MaxValue || len < 0) throw new ArgumentException();

			int ret = BaseStream.Read(ptr, 0, (int)len);
			return new IntPtr(ret);
		}

		IntPtr fseek(IntPtr offset, IntPtr origin)
		{
			SeekOrigin so = (SeekOrigin)origin.ToInt32();
			long loffset = offset.ToInt64();
			return new IntPtr(BaseStream.Seek(loffset, so));
		}

		IntPtr ftell() { return new IntPtr(BaseStream.Position); }
		IntPtr size() { return new IntPtr(BaseStream.Length); }

		void fwrite(
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
			byte[] ptr,
			IntPtr bytes)
		{
			long len = bytes.ToInt64();
			if (len >= int.MaxValue || len < 0) throw new ArgumentException();

			BaseStream.Write(ptr, 0, (int)len);
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate int fgetcDelegate();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void disposeDelegate();
		//TODO - for more speed fread and fwrite might appreciate taking pointers
		//(although, we'll have to convert it to an array to deal with an underlying stream anyway -- or will we?
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate IntPtr freadDelegate(
			[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
			byte[] ptr,
			IntPtr bytes);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void fwriteDelegate(
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
			byte[] ptr,
			IntPtr bytes);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate IntPtr fseekDelegate(IntPtr offset, IntPtr origin);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate IntPtr ftellDelegate();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate IntPtr sizeDelegate();

	}
}