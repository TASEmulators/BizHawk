using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : IEmulator
	{
		public string SystemId => VSystemID.Raw.MAME;
		public bool DeterministicEmulation => true;
		public int Frame { get; private set; }
		public IEmulatorServiceProvider ServiceProvider { get; }
		public ControllerDefinition ControllerDefinition => MAMEController;

		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			_controller = controller;

			using (_exe.EnterExit())
			{
				SendInput();
				_core.mame_lua_execute(MAMELuaCommand.Step);
				_core.mame_coswitch();
				UpdateVideo();
			}

			Frame++;

			if (IsLagFrame)
			{
				LagCount++;
			}

			return true;
		}

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void Dispose()
		{
			_exe.Dispose();
		}
	}
}