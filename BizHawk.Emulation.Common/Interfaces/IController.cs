namespace BizHawk.Emulation.Common
{
	public interface IController
	{
		/// <summary>
		/// Defines the controller schema, including all currently available buttons and their types
		/// </summary>
		ControllerDefinition Definition { get; }

		// TODO - it is obnoxious for this to be here. must be removed.
		bool this[string button] { get; }
		
		// TODO - this can stay but it needs to be changed to go through the float
		bool IsPressed(string button);

		float GetFloat(string name);
	}
}
