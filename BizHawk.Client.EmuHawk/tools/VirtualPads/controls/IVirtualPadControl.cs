using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IVirtualPadControl
	{
		/// <summary>
		/// Clears the pad and resets it to a logical default
		/// </summary>
		void Clear();

		void UpdateValues();

		/// <summary>
		/// Sets the state of the control based on the given controller state
		/// </summary>
		/// <param name="controller">The controller state that the control will be set to</param>
		void Set(IController controller);

		/// <summary>
		/// Gets or sets whether or not the user can change the state of the control
		/// </summary>
		bool ReadOnly { get; set; }
	}
}
