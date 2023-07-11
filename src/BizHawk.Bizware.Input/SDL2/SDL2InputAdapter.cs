#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Common.CollectionExtensions;

using static SDL2.SDL;

namespace BizHawk.Bizware.Input
{
	public sealed class SDL2InputAdapter : OSTailoredKeyInputAdapter
	{
		private static readonly IReadOnlyCollection<string> SDL2_HAPTIC_CHANNEL_NAMES = new[] { "Left", "Right" };

		private IReadOnlyDictionary<string, int> _lastHapticsSnapshot = new Dictionary<string, int>();

		private readonly object _syncObject = new();
		private bool _isInit;

		public override string Desc => "SDL2";

		static SDL2InputAdapter()
		{
			if (SDL_Init(SDL_INIT_JOYSTICK | SDL_INIT_HAPTIC | SDL_INIT_GAMECONTROLLER) != 0)
			{
				throw new InvalidOperationException("Could not init SDL2");
			}
		}

		public override void DeInitAll()
		{
			lock (_syncObject)
			{
				base.DeInitAll();

				SDL2GameController.Deinitialize();
				SDL2Joystick.Deinitialize();

				_isInit = false;
			}
		}

		public override void FirstInitAll(IntPtr mainFormHandle)
		{
			if (_isInit) throw new InvalidOperationException($"Cannot reinit with {nameof(FirstInitAll)}");

			// SDL2's keyboard support is not usable by us, as it requires a focused window
			// even worse, the main form doesn't even work in this context
			// as for some reason SDL2 just never receives input events
			base.FirstInitAll(mainFormHandle);

			SDL2GameController.Initialize();
			SDL2Joystick.Initialize();

			_isInit = true;
		}

		public override IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetHapticsChannels()
		{
			lock (_syncObject)
			{
				return _isInit
					? SDL2GameController.EnumerateDevices()
						.Where(pad => pad.HasRumble)
						.Select(pad => pad.InputNamePrefix)
						.Concat(SDL2Joystick.EnumerateDevices()
							.Where(stick => stick.HasRumble)
							.Select(stick => stick.InputNamePrefix))
						.ToDictionary(s => s, _ => SDL2_HAPTIC_CHANNEL_NAMES)
					: new();
			}
		}

		public override void ReInitGamepads(IntPtr mainFormHandle)
		{
		}

		public override void PreprocessHostGamepads()
		{
			SDL_JoystickUpdate(); // also updates game controllers
			SDL2GameController.Refresh();
			SDL2Joystick.Refresh();
		}

		public override void ProcessHostGamepads(Action<string?, bool, ClientInputFocus> handleButton, Action<string?, int> handleAxis)
		{
			lock (_syncObject)
			{
				if (!_isInit) return;

				foreach (var pad in SDL2GameController.EnumerateDevices())
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

				foreach (var stick in SDL2Joystick.EnumerateDevices())
				{
					foreach (var but in stick.ButtonGetters) handleButton(stick.InputNamePrefix + but.ButtonName, but.GetIsPressed(), ClientInputFocus.Pad);
					foreach (var (axisID, f) in stick.GetAxes()) handleAxis($"{stick.InputNamePrefix}{axisID} Axis", f);

					if (stick.HasRumble)
					{
						var leftStrength = _lastHapticsSnapshot.GetValueOrDefault(stick.InputNamePrefix + "Left");
						var rightStrength = _lastHapticsSnapshot.GetValueOrDefault(stick.InputNamePrefix + "Right");
						stick.SetVibration(leftStrength, rightStrength);	
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
