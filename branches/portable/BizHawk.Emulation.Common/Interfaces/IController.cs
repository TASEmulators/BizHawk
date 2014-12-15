namespace BizHawk.Emulation.Common
{
	
	public interface IController
	{
		ControllerDefinition Type { get; }

		// TODO - it is obnoxious for this to be here. must be removed.
		bool this[string button] { get; }
		
		// TODO - this can stay but it needs to be changed to go through the float
		bool IsPressed(string button);

		float GetFloat(string name);
	}
}
