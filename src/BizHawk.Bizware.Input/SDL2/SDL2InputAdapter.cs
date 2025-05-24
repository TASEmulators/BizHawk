#nullable enable

using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
#if BIZHAWKBUILD_DEBUG_RUMBLE
using BizHawk.Common.NumberExtensions;
#endif

using Windows.Win32.UI.WindowsAndMessaging;

using static SDL2.SDL;

#pragma warning disable BHI1007 // target-typed Exception TODO don't

namespace BizHawk.Bizware.Input
{
	public sealed class SDL2InputAdapter : OSTailoredKeyMouseInputAdapter
	{
		private static readonly IReadOnlyCollection<string> SDL2_HAPTIC_CHANNEL_NAMES = [ "Left", "Right" ];

		private IReadOnlyDictionary<string, int> _lastHapticsSnapshot = new Dictionary<string, int>();

		private bool _sdlInitCalled; // must be deferred on the input thread (FirstInitAll is not on the input thread)
		private bool _isInit;

		public override string Desc => "SDL2";

		// we only want joystick adding and remove events
		private static readonly SDL_EventFilter _sdlEventFilter = SDLEventFilter;

		private static unsafe int SDLEventFilter(IntPtr userdata, IntPtr e)
			=> ((SDL_Event*)e)->type is SDL_EventType.SDL_JOYDEVICEADDED or SDL_EventType.SDL_JOYDEVICEREMOVED ? 1 : 0;

		static SDL2InputAdapter()
		{
			SDL_SetEventFilter(_sdlEventFilter, IntPtr.Zero);
			// this is required as we create hidden (unfocused!) SDL windows in IGL backends
			SDL_SetHintWithPriority(SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1", SDL_HintPriority.SDL_HINT_OVERRIDE);
		}

		private static void DoSDLEventLoop()
		{
			var e = new SDL_Event[1];
			// this loop somewhat models SDL_PollEvent
			while (true)
			{
				// need to pump events for this thread's queue (normally expected from SDL_PumpEvents, but not allowed since this is not the video thread)
				// similar code shouldn't be needed on other platforms (which have global message queues and not thread local message queues)
				if (!OSTailoredCode.IsUnixHost)
				{
					while (WmImports.PeekMessageW(
						out var msg,
						hWnd: default,
						wMsgFilterMin: 0,
						wMsgFilterMax: 0,
						PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
					{
						WmImports.TranslateMessage(in msg);
						WmImports.DispatchMessageW(in msg);
					}
				}

				SDL_JoystickUpdate();

				if (SDL_PeepEvents(e, 1, SDL_eventaction.SDL_GETEVENT, SDL_EventType.SDL_JOYDEVICEADDED, SDL_EventType.SDL_JOYDEVICEREMOVED) != 1)
				{
					break;
				}

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

		public override void DeInitAll()
		{
			if (!_isInit)
			{
				return;
			}

			base.DeInitAll();
			SDL2Gamepad.Deinitialize();
			if (_sdlInitCalled)
			{
				SDL_QuitSubSystem(SDL_INIT_JOYSTICK | SDL_INIT_GAMECONTROLLER);
				_sdlInitCalled = false;
			}

			_isInit = false;
		}

		public override void FirstInitAll(IntPtr mainFormHandle)
		{
			if (_isInit) throw new InvalidOperationException($"Cannot reinit with {nameof(FirstInitAll)}");

			// SDL2's keyboard support is not usable by us, as it requires a focused window
			// even worse, the main form doesn't even work in this context
			// as for some reason SDL2 just never receives input events
			base.FirstInitAll(mainFormHandle);

			// first event loop adds controllers
			// but it must be deferred to the input thread (first PreprocessHostGamepads call)
			// however, let's just test if SDL init works (if it does, 99.9% chance it will on the input thread)
			try
			{
				if (SDL_Init(SDL_INIT_JOYSTICK | SDL_INIT_GAMECONTROLLER) != 0)
				{
					throw new($"SDL failed to init, SDL error: {SDL_GetError()}");
				}
			}
			finally
			{
				SDL_QuitSubSystem(SDL_INIT_JOYSTICK | SDL_INIT_GAMECONTROLLER);
				SDL_FlushEvents(SDL_EventType.SDL_FIRSTEVENT, SDL_EventType.SDL_LASTEVENT);
			}

			_isInit = true;
		}

		public override IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetHapticsChannels()
		{
			return _isInit
				? SDL2Gamepad.EnumerateDevices()
					.Where(pad => pad.HasRumble)
					.Select(pad => pad.InputNamePrefix)
					.ToDictionary(s => s, _ => SDL2_HAPTIC_CHANNEL_NAMES)
				: [ ];
		}

		public override void PreprocessHostGamepads()
		{
			if (!_sdlInitCalled)
			{
				if (SDL_Init(SDL_INIT_JOYSTICK | SDL_INIT_GAMECONTROLLER) != 0)
				{
					SDL_QuitSubSystem(SDL_INIT_JOYSTICK | SDL_INIT_GAMECONTROLLER);
					throw new($"SDL failed to init, SDL error: {SDL_GetError()}");
				}

				_sdlInitCalled = true;
			}

			DoSDLEventLoop();
		}

		public override void ProcessHostGamepads(Action<string?, bool, HostInputType> handleButton, Action<string?, int> handleAxis)
		{
			if (!_isInit) return;

			foreach (var pad in SDL2Gamepad.EnumerateDevices())
			{
				foreach (var (ButtonName, GetIsPressed) in pad.ButtonGetters)
				{
					handleButton(pad.InputNamePrefix + ButtonName, GetIsPressed(), HostInputType.Pad);
				}

				foreach (var (axisID, f) in pad.GetAxes())
				{
					handleAxis($"{pad.InputNamePrefix}{axisID} Axis", f);
				}

				if (pad.HasRumble)
				{
					var leftStrength = _lastHapticsSnapshot.GetValueOrDefault(pad.InputNamePrefix + "Left");
					var rightStrength = _lastHapticsSnapshot.GetValueOrDefault(pad.InputNamePrefix + "Right");
#if BIZHAWKBUILD_DEBUG_RUMBLE
					const int WIDTH = 50; // don't forget to change PACKED_BAR_GRAPHICS too
					const string PACKED_BAR_GRAPHICS = "                                                  ##################################################                                                  ";
					const float SCALE_FACTOR = (float) (WIDTH / (double) int.MaxValue);
					Console.WriteLine($"rumble: [{PACKED_BAR_GRAPHICS.Substring(startIndex: (leftStrength * SCALE_FACTOR).RoundToInt(), length: WIDTH)}] L / R [{PACKED_BAR_GRAPHICS.Substring(startIndex: 2 * WIDTH - (rightStrength * SCALE_FACTOR).RoundToInt(), length: WIDTH)}]");
#endif
					pad.SetVibration(leftStrength, rightStrength);
				}
			}
		}

		public override IEnumerable<KeyEvent> ProcessHostKeyboards()
		{
			return _isInit
				? base.ProcessHostKeyboards()
				: [ ];
		}

		public override void SetHaptics(IReadOnlyCollection<(string Name, int Strength)> hapticsSnapshot)
			=> _lastHapticsSnapshot = hapticsSnapshot.ToDictionary(tuple => tuple.Name, tuple => tuple.Strength);
	}
}
