namespace BizHawk
{
    public class NullController : IController
    {
        public ControllerDefinition Type { get { return null; } }
        public bool this[string name] { get { return false; } }
        public bool IsPressed(string name) { return false; }
        public float GetFloat(string name) { return 0f; }
        public void UnpressButton(string name) { }
        public int FrameNumber { get; set; }

        private static NullController nullController = new NullController();
        public static NullController GetNullController() { return nullController;  }
    }
}