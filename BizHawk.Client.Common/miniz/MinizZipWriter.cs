using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Client.Common.Miniz
{
	public class MinizZipWriter : IZipWriter
	{
		private IntPtr _zip;
		private uint _flags;
		private static readonly byte[] _shitcock = new byte[32 * 1024 * 1024];

		public MinizZipWriter(string path, int compressionlevel)
		{
			_zip = Marshal.AllocHGlobal(128);
			unsafe
			{
				var p = (int*)_zip;
				for (int i = 0; i < 32; i++)
					p[i] = 0;
			}
			if (!LibMiniz.mz_zip_writer_init_file(_zip, path, 0))
			{
				Marshal.FreeHGlobal(_zip);
				_zip = IntPtr.Zero;
				throw new InvalidOperationException("mz_zip_writer_init_file returned FALSE");
			}
			_flags = (uint)compressionlevel;
		}

		void IDisposable.Dispose()
		{
			Dispose();
			GC.SuppressFinalize(this);
		}

		~MinizZipWriter()
		{
			Dispose();
		}

		private void Dispose()
		{
			if (_zip != IntPtr.Zero)
			{
				if (LibMiniz.mz_zip_writer_finalize_archive(_zip))
					LibMiniz.mz_zip_writer_end(_zip);
				Marshal.FreeHGlobal(_zip);
				_zip = IntPtr.Zero;
			}
		}

		public void WriteItem(string name, Action<Stream> callback)
		{
			lock (_shitcock)
			{
				var ms = new MemoryStream(_shitcock);
				callback(ms);
				if (!LibMiniz.mz_zip_writer_add_mem(_zip, name, _shitcock /*ms.GetBuffer()*/, (ulong)ms.Position, _flags))
					throw new InvalidOperationException("mz_zip_writer_add_mem returned FALSE");
			}
		}
	}
}
