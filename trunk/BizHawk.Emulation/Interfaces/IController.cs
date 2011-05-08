using System.Collections.Generic;

namespace BizHawk
{
    public class ControllerDefinition
    {
        public string Name;
        public List<string> BoolButtons = new List<string>();
        public List<string> FloatControls = new List<string>();
    }

    public interface IController
    {
        ControllerDefinition Type { get; }

        bool this[string button] { get; }
        bool IsPressed(string button);
        float GetFloat(string name);

        void UpdateControls(int frame);
    }
}
