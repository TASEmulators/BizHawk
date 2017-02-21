namespace BizHawk.Emulation.Common
{
	public interface IController
	{
		/// <summary>
		/// Defines the controller schema, including all currently available buttons and their types
		/// </summary>
		ControllerDefinition Definition { get; }

		/// <summary>
		/// Returns the current state of a boolean control
		/// </summary>
		bool IsPressed(string button);

		/// <summary>
		/// Returns the state of a float control
		/// </summary>
		float GetFloat(string name);
	}
}
