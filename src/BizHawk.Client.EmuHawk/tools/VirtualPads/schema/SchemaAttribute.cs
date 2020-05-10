using System;

namespace BizHawk.Client.EmuHawk
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class SchemaAttribute : Attribute
	{
		public string SystemId { get; }

		public SchemaAttribute(string systemId)
		{
			SystemId = systemId;
		}
	}
}
