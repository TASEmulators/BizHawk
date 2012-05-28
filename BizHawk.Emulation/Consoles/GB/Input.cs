using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.GB
{
	public partial class GB
	{
		public static readonly ControllerDefinition GbController = new ControllerDefinition
		{
			Name = "Gameboy Controller",
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "A", "B", "Select", "Start"
			}
		};

		public ControllerDefinition ControllerDefinition { get { return GbController; } }
		public IController Controller { get; set; }
	}
}
