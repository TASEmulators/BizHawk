using System;
using System.IO;

//I have an ff9 disc which is truncated

namespace BizHawk.Emulation.DiscSystem
{
	public partial class Disc : IDisposable
	{
		/// <summary>
		/// For use with blobs which are prematurely ended: buffers what there is, and zero-pads the rest
		/// </summary>
		public sealed class Blob_ZeroPadBuffer : IBlob
		{
			public static Blob_ZeroPadBuffer MakeBufferFrom(IBlob baseBlob, long start, int bufferLength)
			{
				var ret = new Blob_ZeroPadBuffer();

				//allocate the entire buffer we'll need, and it will already be zero-padded
				ret.buffer = new byte[bufferLength];

				//read as much as is left
				baseBlob.Read(start, ret.buffer, 0, bufferLength);

				//if any less got read, well, there were already zeroes there
				return ret;
			}

			public int Read(long byte_pos, byte[] buffer, int offset, int count)
			{
				int have = buffer.Length;
				int todo = count;
				if (have < todo)
					todo = have;
				Buffer.BlockCopy(this.buffer, 0, buffer, offset, todo);
				return todo;
			}

			byte[] buffer;

			public void Dispose()
			{
				buffer = null;
			}
		}

		
	}
}