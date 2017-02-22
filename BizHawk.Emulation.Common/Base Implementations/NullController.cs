namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A empty implementation of IController that represents the lack of
	/// a controller interface
	/// </summary>
	/// <seealso cref="IController" />
	public class NullController : IController
	{
		private static readonly ControllerDefinition _definition = new ControllerDefinition
		{
			Name = "Null Controller"
		};

		public ControllerDefinition Definition
		{
			get { return _definition; }
		}

		public bool this[string button]
		{
			get { return false; }
		}

		public bool IsPressed(string button)
		{
			return false;
		}

		public float GetFloat(string name)
		{
			return 0f;
		}

		public static NullController Instance = new NullController();
	}
}