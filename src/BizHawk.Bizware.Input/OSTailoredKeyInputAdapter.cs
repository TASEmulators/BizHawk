#nullable enable

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
	/// TODO: Doesn't work for Wayland or macOS yet (maybe Linux should just use evdev here)
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
					X11KeyInput.Deinitialize();
					break;
				case OSTailoredCode.DistinctOS.macOS:
					//QuartzKeyInput.Deinitialize();
					//break;
					throw new NotSupportedException("TODO QUARTZ");
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
					X11KeyInput.Initialize();
					break;
				case OSTailoredCode.DistinctOS.macOS:
					//QuartzKeyInput.Initialize();
					//break;
					throw new NotSupportedException("TODO QUARTZ");
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
				OSTailoredCode.DistinctOS.Linux => X11KeyInput.Update(),
				OSTailoredCode.DistinctOS.macOS => throw new NotSupportedException("TODO QUARTZ"),
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
