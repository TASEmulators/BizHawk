using System;
using System.IO;

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

		public sealed class Blob_ZeroPadAdapter : IBlob
		{
			public Blob_ZeroPadAdapter(IBlob baseBlob, long padFrom, long padLen)
			{
				this.baseBlob = baseBlob;
				this.padFrom = padFrom;
				this.padLen = padLen;
			}

			public int Read(long byte_pos, byte[] buffer, int offset, int count)
			{
				//I have an ff9 disc which can demo this
				throw new NotImplementedException("Blob_ZeroPadAdapter hasnt been tested yet! please report this!");

				//something about this seems unnecessarily complex, but... i dunno.
				/*
				//figure out how much remains until the zero-padding begins
				long remain = byte_pos - padFrom;
				int todo;
				if (remain < count)
					todo = (int)remain;
				else todo = count;

				//read up until the zero-padding
				int totalRead = 0;
				int readed = baseBlob.Read(byte_pos, buffer, offset, todo);
				totalRead += readed;
				offset += todo;

				//if we didnt read enough, we certainly shouldnt try to read any more
				if (readed < todo)
					return readed;

				//if that was all we needed, then we're done
				count -= todo;
				if (count == 0)
					return totalRead;

				//if we need more, it must come from zero-padding
				remain = padLen;
				if (remain < count)
					todo = (int)remain;
				else todo = count;

				Array.Clear(buffer, offset, todo);
				totalRead += todo;

				return totalRead;
				*/
			}

			public void Dispose()
			{
				baseBlob.Dispose();
			}

			private readonly IBlob baseBlob;
			private long padFrom;
			private long padLen;
		}

	}
}