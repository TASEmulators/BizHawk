using System;

namespace BizHawk.Emulation.Common
{
	[AttributeUsage(AttributeTargets.Class)]
	public class CoreAttributes : Attribute
	{
		public CoreAttributes(
			string name,
			string author,
			bool isPorted = false,
			bool isReleased = false,
			string portedVersion = "",
			string portedUrl = "",
			bool singleInstance = false)
		{
			CoreName = name;
			Author = author;
			Ported = isPorted;
			Released = isReleased;
			PortedVersion = portedVersion;
			PortedUrl = portedUrl;
			SingleInstance = singleInstance;
		}

		public string CoreName { get; private set; }
		public string Author { get; private set; }
		public bool Ported { get; private set; }
		public bool Released { get; private set; }
		public string PortedVersion { get; private set; }
		public string PortedUrl { get; private set; }
		public bool SingleInstance { get; private set; }
	}
}
