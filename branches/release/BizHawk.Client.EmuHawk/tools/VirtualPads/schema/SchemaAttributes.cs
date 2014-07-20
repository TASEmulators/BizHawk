using System;

namespace BizHawk.Client.EmuHawk
{
	[AttributeUsage(AttributeTargets.Class)]
	public class SchemaAttributes : Attribute
	{
		/// <summary>
		/// The system id associated with this schema
		/// </summary>
		public string SystemId { get; private set; }

		public SchemaAttributes(string systemId)
		{
			SystemId = systemId;
		}
	}
}
