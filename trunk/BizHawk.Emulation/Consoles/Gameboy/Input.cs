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

        public void SetControllersAsMnemonic(string mnemonic)
        {
            //TODO
        }

        public string GetControllersAsMnemonic()
        {
            return "|........|0|";
        }

        public ControllerDefinition ControllerDefinition { get { return GbController; } }
        public IController Controller { get; set; }
    }
}
