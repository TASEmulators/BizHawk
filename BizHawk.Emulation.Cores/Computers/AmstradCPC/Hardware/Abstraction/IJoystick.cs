
namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// Represents a spectrum joystick
    /// </summary>
    public interface IJoystick
    {
        /// <summary>
        /// The type of joystick
        /// </summary>
        JoystickType JoyType { get; }

        /// <summary>
        /// Array of all the possibly button press names
        /// </summary>
        string[] ButtonCollection { get; set; }

        /// <summary>
        /// The player number that this controller is currently assigned to
        /// </summary>
        int PlayerNumber { get; set; }

        /// <summary>
        /// Sets the joystick line based on key pressed
        /// </summary>
        /// <param name="key"></param>
        /// <param name="isPressed"></param>
        void SetJoyInput(string key, bool isPressed);

        /// <summary>
        /// Gets the state of a particular joystick binding
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool GetJoyInput(string key);
    }
}
