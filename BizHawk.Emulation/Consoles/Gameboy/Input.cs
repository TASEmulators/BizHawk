namespace BizHawk.Emulation.Consoles.Gameboy
{
    public partial class Gameboy
    {
        public string type
        {
            get { return "GB"; }
        }
        
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
