namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Represents a binary patch, to be applied to a byte array. Patches must be contiguous; multiple instances can be used to for non-contiguous patches.
	/// Patches usually contain data which needs to be XOR'd with a base file, but with <see cref="Overwrite"/> set to <see langword="true"/>, this struct can represent data which should replace part of a base file.
	/// </summary>
	/// <remarks>
	/// TODO no mechanism to change length, would that be useful? --yoshi<br/>
	/// upon further reflection, I'm heading towards what is effectively a worse .bps, so maybe just use that --a later yoshi
	/// </remarks>
	public readonly struct FirmwarePatchData
	{
		public readonly byte[] Contents;

		/// <summary>position in base file where patch should start</summary>
		/// <remarks>in bytes (octets)</remarks>
		public readonly int Offset;

		/// <summary>base file should be overwritten with patch iff <see langword="true"/>, XOR'd otherwise</summary>
		public readonly bool Overwrite;

		public FirmwarePatchData(int offset, byte[] contents, bool overwrite = false)
		{
			Contents = contents;
			Offset = offset;
			Overwrite = overwrite;
		}

		/// <summary>applies this patch to <paramref name="base"/> in-place, and returns the same reference</summary>
		public readonly byte[] ApplyToMutating(byte[] @base)
		{
			if (Overwrite)
			{
				Array.Copy(Contents, 0, @base, Offset, Contents.Length);
			}
			else
			{
				var iBase = Offset;
				var iPatch = 0;
				var l = Contents.Length;
				while (iPatch < l) @base[iBase++] ^= Contents[iPatch++];
			}
			return @base;
		}
	}
}
