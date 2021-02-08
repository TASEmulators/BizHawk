#nullable enable

namespace BizHawk.Emulation.Common
{
	public readonly struct FirmwareRecord
	{
		public readonly string Description;

		public readonly FirmwareID ID;

		public FirmwareRecord(FirmwareID id, string desc)
		{
			Description = desc;
			ID = id;
		}
	}
}
