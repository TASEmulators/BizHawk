using System;

namespace BizHawk.Emulation.Common
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class CoreAttribute : Attribute
	{
		public CoreAttribute(string name, string author, bool isPorted, bool isReleased, string portedVersion, string portedUrl, bool singleInstance)
		{
			CoreName = name;
			Author = author;
			Ported = isPorted;
			Released = isReleased;
			PortedVersion = portedVersion ?? string.Empty;
			PortedUrl = portedUrl ?? string.Empty;
			SingleInstance = singleInstance;
		}

		public CoreAttribute(string name, string author, bool isPorted, bool isReleased)
			: this(name, author, isPorted, isReleased, null, null, false) {}

		public string CoreName { get; }
		public string Author { get; }
		public bool Ported { get; }
		public bool Released { get; }
		public string PortedVersion { get; }
		public string PortedUrl { get; }
		public bool SingleInstance { get; }
	}
}
