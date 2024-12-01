using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	public partial class LibretroHost : IEmulator
	{
		private readonly BasicServiceProvider _serviceProvider;
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		private LibretroApi.retro_message retro_msg = default;

		private readonly Action<string, int?> _notify;

		private void FrameAdvancePrep(IController controller)
		{
			UpdateInput(controller);

			if (controller.IsPressed("Reset"))
			{
				api.retro_reset();
			}
		}

		private void FrameAdvancePost(bool render, bool renderSound)
		{
			if (bridge.LibretroBridge_GetRetroGeometryInfo(cbHandler, ref av_info.geometry))
			{
				_vidBuffer = new int[av_info.geometry.max_width * av_info.geometry.max_height];
			}

			if (bridge.LibretroBridge_GetRetroTimingInfo(cbHandler, ref av_info.timing))
			{
				VsyncNumerator = checked((int)(10000000 * av_info.timing.fps));
				_blipL.SetRates(av_info.timing.sample_rate, 44100);
				_blipR.SetRates(av_info.timing.sample_rate, 44100);
			}

			if (render)
			{
				UpdateVideoBuffer();
			}

			ProcessSound();
			if (!renderSound)
			{
				DiscardSamples();
			}

			bridge.LibretroBridge_GetRetroMessage(cbHandler, out retro_msg);
			if (retro_msg.frames > 0)
			{
				// TODO: pass frames for duration?
				_notify(Mershul.PtrToStringUtf8(retro_msg.msg), null);
			}

			Frame++;
		}

		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			FrameAdvancePrep(controller);
			api.retro_run();
			FrameAdvancePost(render, renderSound);
			return true;
		}

		private static readonly LibretroControllerDef ControllerDef = new();

		public class LibretroControllerDef : ControllerDefinition
		{
			private const string CAT_KEYBOARD = "RetroKeyboard";

			private const string PFX_RETROPAD = "RetroPad ";

			public LibretroControllerDef()
				: base(name: "LibRetro Controls"/*for compatibility*/)
			{
				for (var player = 1; player <= 2; player++) foreach (var button in new[] { "Up", "Down", "Left", "Right", "Select", "Start", "Y", "B", "X", "A", "L", "R", "L2", "R2", "L3", "R3", })
				{
					BoolButtons.Add($"P{player} {PFX_RETROPAD}{button}");
				}

				BoolButtons.Add("Pointer Pressed");
				this.AddXYPair("Pointer {0}", AxisPairOrientation.RightAndUp, (-32767).RangeTo(32767), 0);

				foreach (var s in new[] {
					"Backspace", "Tab", "Clear", "Return", "Pause", "Escape",
					"Space", "Exclaim", "QuoteDbl", "Hash", "Dollar", "Ampersand", "Quote", "LeftParen", "RightParen", "Asterisk", "Plus", "Comma", "Minus", "Period", "Slash",
					"0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
					"Colon", "Semicolon", "Less", "Equals", "Greater", "Question", "At", "LeftBracket", "Backslash", "RightBracket", "Caret", "Underscore", "Backquote",
					"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
					"Delete",
					"KP0", "KP1", "KP2", "KP3", "KP4", "KP5", "KP6", "KP7", "KP8", "KP9",
					"KP_Period", "KP_Divide", "KP_Multiply", "KP_Minus", "KP_Plus", "KP_Enter", "KP_Equals",
					"Up", "Down", "Right", "Left", "Insert", "Home", "End", "PageUp", "PageDown",
					"F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "F13", "F14", "F15",
					"NumLock", "CapsLock", "ScrollLock", "RShift", "LShift", "RCtrl", "LCtrl", "RAlt", "LAlt", "RMeta", "LMeta", "LSuper", "RSuper", "Mode", "Compose",
					"Help", "Print", "SysReq", "Break", "Menu", "Power", "Euro", "Undo"
				})
				{
					var buttonName = $"Key {s}";
					BoolButtons.Add(buttonName);
					CategoryLabels[buttonName] = CAT_KEYBOARD;
				}

				BoolButtons.Add("Reset");

				MakeImmutable();
			}

			protected override IReadOnlyList<IReadOnlyList<(string Name, AxisSpec? AxisSpec)>> GenOrderedControls()
			{
				// all this is to remove the keyboard buttons from P0 and put them in P3 so they appear at the end of the input display
				var players = base.GenOrderedControls().ToList();
				List<(string, AxisSpec?)> retroKeyboard = new();
				var p0 = (List<(string, AxisSpec?)>) players[0];
				for (var i = 0; i < p0.Count; /* incremented in body */)
				{
					(string ButtonName, AxisSpec?) button = p0[i];
					if (CategoryLabels.TryGetValue(button.ButtonName, out var v) && v is CAT_KEYBOARD)
					{
						retroKeyboard.Add(button);
						p0.RemoveAt(i);
					}
					else
					{
						i++;
					}
				}
				players.Add(retroKeyboard);
				return players;
			}
		}

		public ControllerDefinition ControllerDefinition { get; }
		public int Frame { get; set; }
		public string SystemId => VSystemID.Raw.Libretro;
		public bool DeterministicEmulation => false;

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		private bool inited = false;

		public void Dispose()
		{
			if (inited)
			{
				api.retro_unload_game();
				api.retro_deinit();
				inited = false;
			}

			bridge.LibretroBridge_DestroyCallbackHandler(cbHandler);

			_blipL?.Dispose();
			_blipL = null;
			_blipR?.Dispose();
			_blipR = null;
		}
	}
}
