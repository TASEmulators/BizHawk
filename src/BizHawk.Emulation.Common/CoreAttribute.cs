using System;

namespace BizHawk.Emulation.Common
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class CoreAttribute : Attribute
	{
		public CoreAttribute(string name, string author, bool isPorted, bool isReleased, string portedVersion = null, string portedUrl = null, bool singleInstance = false)
		{
			CoreName = name;
			Author = author;
			Ported = isPorted;
			Released = isReleased;
			PortedVersion = portedVersion ?? string.Empty;
			PortedUrl = portedUrl ?? string.Empty;
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
