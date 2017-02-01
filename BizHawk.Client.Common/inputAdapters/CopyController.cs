using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Just copies source to sink, or returns whatever a NullController would if it is disconnected. useful for immovable hardpoints.
	/// </summary>
	public class CopyControllerAdapter : IController
	{
		public ControllerDefinition Definition
		{
			get { return Curr.Definition; }
		}

		public bool IsPressed(string button)
		{
			return Curr.IsPressed(button);
		}

		public float GetFloat(string name)
		{
			return Curr.GetFloat(name);
		}

		public IController Source { get; set; }

		private IController Curr
		{
			get
			{
				return Source == null
					? NullController.Instance
					: Source;
			}
		}
	}
}
