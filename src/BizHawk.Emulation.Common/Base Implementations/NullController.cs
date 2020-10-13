namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A empty implementation of IController that represents the lack of
	/// a controller interface
	/// </summary>
	/// <seealso cref="IController" />
	public class NullController : IController
	{
		public IVGamepadDef Definition => new ControllerDefinition("Null Controller");

		public bool IsPressed(string button) => false;

		public int AxisValue(string name) => 0;

		public static readonly NullController Instance = new NullController();
	}
}