using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;

namespace BizHawk.Tests.Testroms
{
	public readonly struct DummyRomAsset(byte[] fileData) : IRomAsset
	{
#pragma warning disable CA1065 // yes, really throw
		public string? Extension
			=> throw new NotImplementedException();

		public byte[]? FileData
			=> fileData;

		public GameInfo? Game
			=> throw new NotImplementedException();

		public byte[]? RomData
			=> throw new NotImplementedException();

		public string? RomPath
			=> throw new NotImplementedException();
#pragma warning restore CA1065
	}
}
