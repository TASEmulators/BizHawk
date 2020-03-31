using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Just copies source to sink, or returns whatever a NullController would if it is disconnected. useful for immovable hard-points.
	/// </summary>
	public class CopyControllerAdapter : IController
	{
		public ControllerDefinition Definition => Curr.Definition;

		public bool IsPressed(string button)
		{
			return Curr.IsPressed(button);
		}

		public float AxisValue(string name)
		{
			return Curr.AxisValue(name);
		}

		public IController Source { get; set; }

		private IController Curr => Source ?? NullController.Instance;
	}
}
