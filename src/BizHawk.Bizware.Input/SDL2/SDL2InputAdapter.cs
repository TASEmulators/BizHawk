#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

using static SDL2.SDL;

namespace BizHawk.Bizware.Input
{
	public sealed class SDL2InputAdapter : OSTailoredKeyInputAdapter
	{
		private static readonly IReadOnlyCollection<string> SDL2_HAPTIC_CHANNEL_NAMES = new[] { "Left", "Right" };

		private IReadOnlyDictionary<string, int> _lastHapticsSnapshot = new Dictionary<string, int>();

		private Thread? _sdlThread;
		private readonly ManualResetEventSlim _initialEventQueueEmptied = new(false);
		private readonly object _syncObject = new();
		private volatile bool _isInit;

		public override string Desc => "SDL2";

		// we only want joystick adding and remove events
		private static readonly SDL_EventFilter _sdlEventFilter = SDLEventFilter;
		private static unsafe int SDLEventFilter(IntPtr userdata, IntPtr e)
			=> ((SDL_Event*)e)->type is SDL_EventType.SDL_JOYDEVICEADDED or SDL_EventType.SDL_JOYDEVICEREMOVED ? 1 : 0;

		static SDL2InputAdapter()
		{
			SDL_SetEventFilter(_sdlEventFilter, IntPtr.Zero);
			SDL_SetHint(SDL_HINT_JOYSTICK_THREAD, "1");
		}

		private void SDLThread()
		{
			// we can't use SDL_Pump here
			// as that is only valid on the video (main) thread
			// SDL_JoystickUpdate() works for our purposes here

			// we'll want to init here, as this thread is what will be updating stuff
			if (SDL_Init(SDL_INIT_JOYSTICK | SDL_INIT_HAPTIC | SDL_INIT_GAMECONTROLLER) != 0)
			{
				SDL_QuitSubSystem(SDL_INIT_JOYSTICK | SDL_INIT_HAPTIC | SDL_INIT_GAMECONTROLLER);
				_isInit = false;
				_initialEventQueueEmptied.Set();
				return;
			}

			// Windows SDL bug
			// SDL uses message pumping for hidapi device detection
			// but besides the initial init, it will not pump this window
			// even using SDL_PumpEvent will fail at pumping this window
			// (although we can't use that for other reasons)
			var hidapiWindow = IntPtr.Zero;
			if (!OSTailoredCode.IsUnixHost)
			{
				hidapiWindow = Win32Imports.FindWindowEx(Win32Imports.HWND_MESSAGE, IntPtr.Zero,
					"SDL_HIDAPI_DEVICE_DETECTION", null);
			}

			var e = new SDL_Event[1];
			while (true)
			{
				lock (_syncObject)
				{
					if (!_isInit)
					{
						break;
					}

					if (!OSTailoredCode.IsUnixHost && hidapiWindow != IntPtr.Zero)
					{
						while (Win32Imports.PeekMessage(out var msg, hidapiWindow, 0, 0, Win32Imports.PM_REMOVE))
						{
							Win32Imports.TranslateMessage(ref msg);
							Win32Imports.DispatchMessage(ref msg);
						}
					}

					SDL_JoystickUpdate();
					while (SDL_PeepEvents(e, 1, SDL_eventaction.SDL_GETEVENT, SDL_EventType.SDL_JOYDEVICEADDED, SDL_EventType.SDL_JOYDEVICEREMOVED) == 1)
					{
						// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
						switch (e[0].type)
						{
							case SDL_EventType.SDL_JOYDEVICEADDED:
								SDL2Gamepad.AddDevice(e[0].jdevice.which);
								break;
							case SDL_EventType.SDL_JOYDEVICEREMOVED:
								SDL2Gamepad.RemoveDevice(e[0].jdevice.which);
								break;
						}
					}
				}

				_initialEventQueueEmptied.Set();
				Thread.Sleep(1);
			}

			SDL_QuitSubSystem(SDL_INIT_JOYSTICK | SDL_INIT_HAPTIC | SDL_INIT_GAMECONTROLLER);
		}

		public override void DeInitAll()
		{
			if (!_isInit)
			{
				return;
			}

			lock (_syncObject)
			{
				base.DeInitAll();
				SDL2Gamepad.Deinitialize();
				_isInit = false;
			}

			_sdlThread!.Join();
			_initialEventQueueEmptied.Dispose();
		}

		public override void FirstInitAll(IntPtr mainFormHandle)
		{
			if (_isInit) throw new InvalidOperationException($"Cannot reinit with {nameof(FirstInitAll)}");

			// SDL2's keyboard support is not usable by us, as it requires a focused window
			// even worse, the main form doesn't even work in this context
			// as for some reason SDL2 just never receives input events
			base.FirstInitAll(mainFormHandle);

			_isInit = true;
			_sdlThread = new(SDLThread) { IsBackground = true };
			_sdlThread.Start();
			_initialEventQueueEmptied.Wait();

			if (!_isInit)
			{
				base.DeInitAll();
				throw new InvalidOperationException($"SDL failed to init, SDL error: {SDL_GetError()}");
			}
		}

		public override IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetHapticsChannels()
		{
			lock (_syncObject)
			{
				return _isInit
					? SDL2Gamepad.EnumerateDevices()
						.Where(pad => pad.HasRumble)
						.Select(pad => pad.InputNamePrefix)
						.ToDictionary(s => s, _ => SDL2_HAPTIC_CHANNEL_NAMES)
					: new();
			}
		}

		public override void ReInitGamepads(IntPtr mainFormHandle)
		{
		}

		public override void PreprocessHostGamepads()
		{
		}

		public override void ProcessHostGamepads(Action<string?, bool, ClientInputFocus> handleButton, Action<string?, int> handleAxis)
		{
			lock (_syncObject)
			{
				if (!_isInit) return;

				foreach (var pad in SDL2Gamepad.EnumerateDevices())
				{
					foreach (var but in pad.ButtonGetters) handleButton(pad.InputNamePrefix + but.ButtonName, but.GetIsPressed(), ClientInputFocus.Pad);
					foreach (var (axisID, f) in pad.GetAxes()) handleAxis($"{pad.InputNamePrefix}{axisID} Axis", f);

					if (pad.HasRumble)
					{
						var leftStrength = _lastHapticsSnapshot.GetValueOrDefault(pad.InputNamePrefix + "Left");
						var rightStrength = _lastHapticsSnapshot.GetValueOrDefault(pad.InputNamePrefix + "Right");
						pad.SetVibration(leftStrength, rightStrength);	
					}
				}
			}
		}

		public override IEnumerable<KeyEvent> ProcessHostKeyboards()
		{
			lock (_syncObject)
			{
				return _isInit
					? base.ProcessHostKeyboards()
					: Enumerable.Empty<KeyEvent>();
			}
		}

		public override void SetHaptics(IReadOnlyCollection<(string Name, int Strength)> hapticsSnapshot)
			=> _lastHapticsSnapshot = hapticsSnapshot.ToDictionary(tuple => tuple.Name, tuple => tuple.Strength);
	}
}
