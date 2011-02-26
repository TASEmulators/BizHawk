namespace BizHawk.Emulation.Consoles.Gameboy
{
    public partial class Gameboy
    {
        public static readonly ControllerDefinition GbController = new ControllerDefinition
        {
            Name = "Gameboy Controller",
            BoolButtons =
                {
                    "Up", "Down", "Left", "Right", "A", "B", "Select", "Start"
                }
        };

        public string GetControllersAsMneumonic()
        {
            return "|........|0|";
        }

        public ControllerDefinition ControllerDefinition { get { return GbController; } }
        public IController Controller { get; set; }
    }
}
