using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.ApiHawk
{
	public abstract class PluginBase : IPlugin
	{
		/// <summary>
		/// The base class from which all
		/// plugins will be derived
		/// 
		/// Actual plugins should implement
		/// one of the below callback methods
		/// or register memory callbacks in
		/// their Init function.
		/// </summary>
		protected IApiContainer _api;

		public PluginBase()	{ }

		public abstract string Name { get; }
		public abstract string Description { get; }
		
		public bool Enabled => Running;
		public bool Paused => !Running;

		public bool Running { get; set; }

		public void Stop()
		{
			Running = false;
		}

		public void Toggle()
		{
			Running = !Running;
		}

		public virtual void PreFrameCallback() { }
		public virtual void PostFrameCallback() { }
		public virtual void SaveStateCallback(string name) { }
		public virtual void LoadStateCallback(string name) { }
		public virtual void InputPollCallback() { }
		public virtual void ExitCallback() { }
		public virtual void Init (IApiContainer api)
		{
			_api = api;
			Running = true;
		}
	}
}
