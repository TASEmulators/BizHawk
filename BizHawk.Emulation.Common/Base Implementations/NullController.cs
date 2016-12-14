namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A empty implementation of IController that represents the lack of
	/// a controller interface
	/// </summary>
	/// <seealso cref="IController" />
	public class NullController : IController
	{
		public ControllerDefinition Definition { get { return null; } }
		public bool this[string button] { get { return false; } }
		public bool IsPressed(string button) { return false; }
		public float GetFloat(string name) { return 0f; }
		public void UnpressButton(string button) { }
		public void ForceButton(string button) { }

		public void SetSticky(string button, bool sticky) { }
		public bool IsSticky(string button) { return false; }
		private static readonly NullController nullController = new NullController();
		public static NullController GetNullController() { return nullController; }
	}
}