using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class AndAdapter : IController
	{
		public ControllerDefinition Definition => Source.Definition;

		public bool IsPressed(string button)
		{
			if (Source != null && SourceAnd != null)
			{
				return Source.IsPressed(button) & SourceAnd.IsPressed(button);
			}

			return false;
		}

		// pass axes solely from the original source
		// this works in the code because SourceOr is the autofire controller
		public int AxisValue(string name) => Source.AxisValue(name);

		internal IController Source { get; set; }
		internal IController SourceAnd { get; set; }
	}

	public class XorAdapter : IController
	{
		public ControllerDefinition Definition => Source.Definition;

		public bool IsPressed(string button)
		{
			if (Source != null && SourceXor != null)
			{
				return Source.IsPressed(button) ^ SourceXor.IsPressed(button);
			}

			return false;
		}

		// pass axes solely from the original source
		// this works in the code because SourceOr is the autofire controller
		public int AxisValue(string name) => Source.AxisValue(name);

		internal IController Source { get; set; }
		internal IController SourceXor { get; set; }
	}

	public class ORAdapter : IController
	{
		public ControllerDefinition Definition => Source.Definition;

		public bool IsPressed(string button)
		{
			return (Source?.IsPressed(button) ?? false)
					| (SourceOr?.IsPressed(button) ?? false);
		}

		// pass axes solely from the original source
		// this works in the code because SourceOr is the autofire controller
		public int AxisValue(string name) => Source.AxisValue(name);

		internal IController Source { get; set; }
		internal IController SourceOr { get; set; }
	}
}
