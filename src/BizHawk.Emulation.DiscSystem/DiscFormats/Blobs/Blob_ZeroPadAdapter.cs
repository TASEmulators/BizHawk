//I have an ff9 disc which is truncated

namespace BizHawk.Emulation.DiscSystem
{
	internal sealed class Blob_ZeroPadAdapter : IBlob
	{
		private readonly IBlob srcBlob;
		private readonly long srcBlobLength;

		public Blob_ZeroPadAdapter(IBlob srcBlob, long srcBlobLength)
		{
			this.srcBlob = srcBlob;
			this.srcBlobLength = srcBlobLength;
		}

		public int Read(long byte_pos, byte[] buffer, int offset, int count)
		{
			var todo = count;
			var end = byte_pos + todo;
			if (end > srcBlobLength)
			{
				todo = checked((int)(srcBlobLength - byte_pos));
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
