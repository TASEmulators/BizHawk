#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Bizware.Input
{
	/// <summary>
	/// Abstract class which only handles keyboard and mouse input
	/// Uses OS specific functionality, as there is no good cross platform way to do this
	/// (Mostly as all the available cross-platform options require a focused window, arg!)
	/// TODO: Doesn't work for Wayland yet (must use XWayland, which Wayland users need to use anyways for BizHawk)
	/// </summary>
	public abstract class OSTailoredKeyMouseInputAdapter : IHostInputAdapter
	{
		private IKeyMouseInput? _keyMouseInput;
		protected Func<bool>? _getHandleAlternateKeyboardLayouts;

		public abstract string Desc { get; }

		public virtual void DeInitAll()
			=> _keyMouseInput!.Dispose();

		public virtual void FirstInitAll(IntPtr mainFormHandle)
		{
			_keyMouseInput = KeyMouseInputFactory.CreateKeyMouseInput();
			IPCKeyInput.Initialize(); // why not? this isn't necessarily OS specific
		}

		public abstract IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetHapticsChannels();

		public abstract void PreprocessHostGamepads();

		public abstract void ProcessHostGamepads(Action<string?, bool, HostInputType> handleButton, Action<string?, int> handleAxis);

		public virtual IEnumerable<KeyEvent> ProcessHostKeyboards()
		{
			if (_getHandleAlternateKeyboardLayouts is null)
			{
				throw new InvalidOperationException(nameof(ProcessHostKeyboards) + " called before alternate keyboard layout enable callback was set");
			}

			var ret = _keyMouseInput!.UpdateKeyInputs(_getHandleAlternateKeyboardLayouts());
			return ret.Concat(IPCKeyInput.Update());
		}

		public virtual (int DeltaX, int DeltaY) ProcessHostMice()
			=> _keyMouseInput!.UpdateMouseInput();

		public abstract void SetHaptics(IReadOnlyCollection<(string Name, int Strength)> hapticsSnapshot);

		public virtual void SetAlternateKeyboardLayoutEnableCallback(Func<bool> getHandleAlternateKeyboardLayouts)
			=> _getHandleAlternateKeyboardLayouts = getHandleAlternateKeyboardLayouts;
	}
}
