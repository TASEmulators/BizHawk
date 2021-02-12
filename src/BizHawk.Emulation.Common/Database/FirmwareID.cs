#nullable enable

namespace BizHawk.Emulation.Common
{
	public readonly struct FirmwareID
	{
		public static bool operator ==(FirmwareID a, FirmwareID b) => a.Firmware == b.Firmware && a.System == b.System;

		public static bool operator !=(FirmwareID a, FirmwareID b) => a.Firmware != b.Firmware || a.System != b.System;

		public string ConfigKey => $"{System}+{Firmware}";

		public readonly string Firmware;

		public readonly string System;

		public FirmwareID(string system, string firmware)
		{
			System = system;
			Firmware = firmware;
		}

		public override bool Equals(object obj) => obj is FirmwareID other
			&& other.Firmware == Firmware && other.System == System;

		public override int GetHashCode() => (System, Firmware).GetHashCode();

		public override string ToString() => ConfigKey;
	}
}
