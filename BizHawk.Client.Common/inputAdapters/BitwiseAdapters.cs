using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class AndAdapter : IController
	{
		public ControllerDefinition Definition
		{
			get { return Source.Definition; }
		}

		public bool IsPressed(string button)
		{
			if (Source != null && SourceAnd != null)
			{
				return Source.IsPressed(button) & SourceAnd.IsPressed(button);
			}

			return false;
		}

		// pass floats solely from the original source
		// this works in the code because SourceOr is the autofire controller
		public float GetFloat(string name)
		{
			return Source.GetFloat(name);
		}

		internal IController Source { get; set; }
		internal IController SourceAnd { get; set; }
	}

	public class ORAdapter : IController
	{
		public ControllerDefinition Definition
		{
			get { return Source.Definition; }
		}

		public bool IsPressed(string button)
		{
			return (Source != null ? Source.IsPressed(button) : false)
					| (SourceOr != null ? SourceOr.IsPressed(button) : false);
		}

		// pass floats solely from the original source
		// this works in the code because SourceOr is the autofire controller
		public float GetFloat(string name)
		{
			return Source.GetFloat(name);
		}

		internal IController Source { get; set; }
		internal IController SourceOr { get; set; }
	}
}
