namespace BizHawk.Client.Common
{
	interface IPlugin
	{
		void PreFrameCallback();
		void PostFrameCallback();
		void SaveStateCallback(string name);
		void LoadStateCallback(string name);
		void InputPollCallback();
		void Init(IPluginAPI api);
	}
}
