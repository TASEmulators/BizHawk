using System;
using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ToolAttributes : Attribute
	{
		public ToolAttributes(bool released)
		{
			Released = released;
		}

		public bool Released { get; private set; }
	}
}
