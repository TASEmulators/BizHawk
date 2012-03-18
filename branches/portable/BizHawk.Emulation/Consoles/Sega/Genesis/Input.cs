namespace BizHawk.Emulation.Consoles.Sega
{
	partial class Genesis
	{
		public static readonly ControllerDefinition GenesisController = new ControllerDefinition
		{
			Name = "Genesis 3-Button Controller",
			BoolButtons =
			{
				"Reset",
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 A", "P1 B", "P1 C", "P1 Start"
			}
		};

		public ControllerDefinition ControllerDefinition { get { return GenesisController; } }
		public IController Controller { get; set; }
	}
}