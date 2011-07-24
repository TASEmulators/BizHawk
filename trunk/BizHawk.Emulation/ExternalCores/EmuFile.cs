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

namespace BizHawk
{

	public class EmuFile : ExternalCore
	{

		public EmuFile(IExternalCoreAccessor accessor)
			: base(accessor)
		{
			UnmanagedOpaque = QueryCoreCall<Func<IntPtr, IntPtr>>("EmuFile.Construct")(ManagedOpaque);
			
			QueryCoreCall<Action<string, IntPtr>>("EmuFile.Set_fp")("fgetc", ExportDelegate(new fgetcDelegate(fgetc)));
			QueryCoreCall<Action<string, IntPtr>>("EmuFile.Set_fp")("fread", ExportDelegate(new freadDelegate(fread)));
			QueryCoreCall<Action<string, IntPtr>>("EmuFile.Set_fp")("fwrite", ExportDelegate(new fwriteDelegate(fwrite)));
			QueryCoreCall<Action<string, IntPtr>>("EmuFile.Set_fp")("fseek", ExportDelegate(new fseekDelegate(fseek)));
			QueryCoreCall<Action<string, IntPtr>>("EmuFile.Set_fp")("ftell", ExportDelegate(new ftellDelegate(ftell)));
			QueryCoreCall<Action<string, IntPtr>>("EmuFile.Set_fp")("size", ExportDelegate(new sizeDelegate(size)));
			QueryCoreCall<Action<string, IntPtr>>("EmuFile.Set_fp")("size", ExportDelegate(new sizeDelegate(size)));
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