namespace Jellyfish.Virtu.Services
{
    public abstract class KeyboardService : MachineService
    {
        protected KeyboardService(Machine machine) : 
            base(machine)
        {
        }

        public abstract bool IsKeyDown(int key);

        public virtual void Update() // main thread
        {
            var keyboard = Machine.Keyboard;
            var buttons = Machine.Buttons;

            keyboard.Button0Key = (ulong)buttons;

            if (IsResetKeyDown && !keyboard.DisableResetKey)
            {
                if (!_resetKeyDown)
                {
                    _resetKeyDown = true; // entering reset; pause until key released
									//TODO ADELIKAT : HANDLE RESET DIFFERENTLY
										//Machine.Pause();
										//Machine.Reset();
                }
            }
            else if (_resetKeyDown)
            {
                _resetKeyDown = false; // leaving reset
							//TODO ADELIKAT : HANDLE RESET DIFFERENTLY
                //Machine.Unpause();
            }
        }

        public bool IsAnyKeyDown { get { return Machine.Buttons > 0; }}
        public bool IsControlKeyDown { get { return Machine.Buttons.HasFlag(Buttons.Ctrl); }}
        public bool IsShiftKeyDown { get { return Machine.Buttons.HasFlag(Buttons.Shift); }}

        public bool IsOpenAppleKeyDown { get; protected set; }
        public bool IsCloseAppleKeyDown { get; protected set; }

        protected bool IsResetKeyDown { get; set; }

        private bool _resetKeyDown;
    }
}
