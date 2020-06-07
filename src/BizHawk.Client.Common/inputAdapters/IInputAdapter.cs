using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Represents an input adapter, that can take in a source, manipulate it as needed
	/// and then represent the resulting state as an <see cref="IController"/>
	/// </summary>
	public interface IInputAdapter : IController
	{
		IController Source { get; set; }
	}
}
