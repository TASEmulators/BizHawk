#nullable enable

// #define USE_EVDEV

using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Bizware.Input
{
	/// <summary>
	/// Abstract class which only handles keyboard input
	/// Uses OS specific functionality, as there is no good cross platform way to do this
	/// (mostly as all the available cross-platform options require a focused window, arg!)
	/// TODO: Doesn't work for Wayland yet (maybe Linux should just use evdev here)
	/// TODO: Stop using a ton of static classes here, checking CurrentOS constantly is annoying...
	/// </summary>
	public abstract class OSTailoredKeyInputAdapter : IHostInputAdapter
	{
		protected Config? _config;

		public abstract string Desc { get; }

		public virtual void DeInitAll()
		{
			switch (OSTailoredCode.CurrentOS)
			{
				case OSTailoredCode.DistinctOS.Linux:
#if USE_EVDEV
					EvDevKeyInput.Deinitialize();
#else
					X11KeyInput.Deinitialize();
#endif
					break;
				case OSTailoredCode.DistinctOS.macOS:
					QuartzKeyInput.Deinitialize();
					break;
				case OSTailoredCode.DistinctOS.Windows:
					RAWKeyInput.Deinitialize();
					break;
				default:
					throw new InvalidOperationException();
			}
		}

		public virtual void FirstInitAll(IntPtr mainFormHandle)
		{
			switch (OSTailoredCode.CurrentOS)
			{
				case OSTailoredCode.DistinctOS.Linux:
					// TODO: probably need a libinput option for Wayland
					// (unless we just want to ditch this and always use evdev here?)
#if USE_EVDEV
					EvDevKeyInput.Deinitialize();
#else
					X11KeyInput.Initialize();
#endif
					break;
				case OSTailoredCode.DistinctOS.macOS:
					QuartzKeyInput.Initialize();
					break;
				case OSTailoredCode.DistinctOS.Windows:
					RAWKeyInput.Initialize();
					break;
				default:
					throw new InvalidOperationException();
			}

			IPCKeyInput.Initialize(); // why not? this isn't necessarily OS specific
		}

		public abstract IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetHapticsChannels();

		public abstract void ReInitGamepads(IntPtr mainFormHandle);

		public abstract void PreprocessHostGamepads();

		public abstract void ProcessHostGamepads(Action<string?, bool, ClientInputFocus> handleButton, Action<string?, int> handleAxis);

		public virtual IEnumerable<KeyEvent> ProcessHostKeyboards()
		{
			var ret = OSTailoredCode.CurrentOS switch
			{
#if USE_EVDEV
				OSTailoredCode.DistinctOS.Linux => EvDevKeyInput.Update(),
#else
				OSTailoredCode.DistinctOS.Linux => X11KeyInput.Update(),
#endif
				OSTailoredCode.DistinctOS.macOS => QuartzKeyInput.Update(),
				OSTailoredCode.DistinctOS.Windows => RAWKeyInput.Update(_config ?? throw new(nameof(ProcessHostKeyboards) + " called before the global config was passed")),
				_ => throw new InvalidOperationException()
			};

			return ret.Concat(IPCKeyInput.Update());
		}

		public abstract void SetHaptics(IReadOnlyCollection<(string Name, int Strength)> hapticsSnapshot);

		public virtual void UpdateConfig(Config config)
			=> _config = config;
	}
}
