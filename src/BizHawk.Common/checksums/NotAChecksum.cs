namespace BizHawk.Common
{
	public class NotAChecksum : Checksum
	{
		protected override string Prefix => "NUL";

		internal NotAChecksum(byte[] digest)
			: base(digest) {}
	}
}
