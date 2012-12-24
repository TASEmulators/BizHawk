namespace EMU7800.Core
{
    public enum ControllerAction
    {
        Up,
        Down,
        Left,
        Right,
        Trigger,   // Interpretation: 7800 RFire; 2600 Fire, BoosterGrip top
        Trigger2,  // Interpretation: 7800 LFire, BoosterGrip trigger
        Keypad1, Keypad2, Keypad3,
        Keypad4, Keypad5, Keypad6,
        Keypad7, Keypad8, Keypad9,
        KeypadA, Keypad0, KeypadP,
        Driving0, Driving1, Driving2, Driving3,
    }
}