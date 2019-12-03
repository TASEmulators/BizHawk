using System;

namespace BizHawk.Emulation.Common
{
	[AttributeUsage(AttributeTargets.Class)]
	public class CoreAttribute : Attribute
	{
		public CoreAttribute(
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

		public string CoreName { get; }
		public string Author { get; }
		public bool Ported { get; }
		public bool Released { get; }
		public string PortedVersion { get; }
		public string PortedUrl { get; }
		public bool SingleInstance { get; }
	}
}
