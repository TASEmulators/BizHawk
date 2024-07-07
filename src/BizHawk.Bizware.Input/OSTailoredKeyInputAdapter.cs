#nullable enable

using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;

namespace BizHawk.Bizware.Input
{
	/// <summary>
	/// Abstract class which only handles keyboard input
	/// Uses OS specific functionality, as there is no good cross platform way to do this
	/// (Mostly as all the available cross-platform options require a focused window, arg!)
	/// TODO: Doesn't work for Wayland yet (must use XWayland, which Wayland users need to use anyways for BizHawk)
	/// </summary>
	public abstract class OSTailoredKeyInputAdapter : IHostInputAdapter
	{
		private IKeyInput? _keyInput;
		protected Config? _config;

		public abstract string Desc { get; }

		public virtual void DeInitAll()
			=> _keyInput!.Dispose();

		public virtual void FirstInitAll(IntPtr mainFormHandle)
		{
			_keyInput = KeyInputFactory.CreateKeyInput();
			IPCKeyInput.Initialize(); // why not? this isn't necessarily OS specific
		}

		public abstract IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetHapticsChannels();

		public abstract void ReInitGamepads(IntPtr mainFormHandle);

		public abstract void PreprocessHostGamepads();

		public abstract void ProcessHostGamepads(Action<string?, bool, ClientInputFocus> handleButton, Action<string?, int> handleAxis);

		public virtual IEnumerable<KeyEvent> ProcessHostKeyboards()
		{
			if (_config is null)
			{
				throw new InvalidOperationException(nameof(ProcessHostKeyboards) + " called before the global config was passed");
			}

			var ret = _keyInput!.Update(_config.HandleAlternateKeyboardLayouts);
			return ret.Concat(IPCKeyInput.Update());
		}

		public abstract void SetHaptics(IReadOnlyCollection<(string Name, int Strength)> hapticsSnapshot);

		public virtual void UpdateConfig(Config config)
			=> _config = config;
	}
}
