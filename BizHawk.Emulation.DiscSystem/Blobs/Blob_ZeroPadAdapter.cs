using System;
using System.IO;

//I have an ff9 disc which is truncated

namespace BizHawk.Emulation.DiscSystem
{
	public partial class Disc : IDisposable
	{
		/// <summary>
		/// A blob that always reads 0
		/// </summary>
		internal sealed class Blob_Zeros : IBlob
		{
			public int Read(long byte_pos, byte[] buffer, int offset, int count)
			{
				Array.Clear(buffer, offset, count);
				return count;
			}

			public void Dispose()
			{
			}
		}

		internal sealed class Blob_ZeroPadAdapter : IBlob
		{
			IBlob srcBlob;
			long srcBlobLength;
			public Blob_ZeroPadAdapter(IBlob srcBlob, long srcBlobLength)
			{
				this.srcBlob = srcBlob;
				this.srcBlobLength = srcBlobLength;
			}

			public int Read(long byte_pos, byte[] buffer, int offset, int count)
			{
				int todo = count;
				long end = byte_pos + todo;
				if (end > srcBlobLength)
				{
					long temp = (int)(srcBlobLength - byte_pos);
					if (temp > int.MaxValue)
						throw new InvalidOperationException();
					todo = (int)temp;
					
					//zero-fill the unused part (just for safety's sake)
					Array.Clear(buffer, offset + todo, count - todo);
				}

				srcBlob.Read(byte_pos, buffer, offset, todo);

				//since it's zero padded, this never fails and always reads the requested amount
				return count;
			}

			public void Dispose()
			{
			}
		}

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