using System;
using System.IO;

//I have an ff9 disc which is truncated

namespace BizHawk.Emulation.DiscSystem
{
	public partial class Disc : IDisposable
	{
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
		
	}
}