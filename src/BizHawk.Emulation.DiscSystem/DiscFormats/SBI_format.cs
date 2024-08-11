using System.IO;
using System.Collections.Generic;

using BizHawk.Common.IOExtensions;

namespace BizHawk.Emulation.DiscSystem.SBI
{
	public class SBIParseException : Exception
	{
		public SBIParseException(string message) : base(message) { }
	}

	/// <summary>
	/// The interpreted contents of an SBI file
	/// </summary>
	public class SubQPatchData
	{
		/// <summary>
		/// a list of patched ABAs
		/// </summary>
		public readonly IList<int> ABAs = new List<int>();

		/// <summary>
		/// 12 values (Q subchannel data) for every patched ABA; -1 means unpatched
		/// </summary>
		public short[] subq;
	}

	public static class SBIFormat
	{
		/// <summary>
		/// Does a cursory check to see if the file looks like an SBI
		/// </summary>
		public static bool QuickCheckISSBI(string path)
		{
			using var fs = File.OpenRead(path);
			var br = new BinaryReader(fs);
			var sig = br.ReadStringFixedUtf8(4);
			return sig == "SBI\0";
		}
	}
}