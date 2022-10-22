using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IRetroAchievements : IDisposable
	{
		/// <summary>
		/// General update, not tied to frame advance
		/// Use this for overlay / checking hardcore mode
		/// </summary>
		public void Update();

		/// <summary>
		/// Called on frame advance, process achievements here
		/// </summary>
		public void OnFrameAdvance();

		/// <summary>
		/// Call this whenever the ROM changes (and after ctor)
		/// Make sure CurrentlyOpenRomArgs is updated before calling this!
		/// </summary>
		public void Restart();

		/// <summary>
		/// Call this before the emulator is disposed
		/// </summary>
		public void Stop();

		/// <summary>
		/// Call this when a state is saved
		/// In memory states (like with rewind) do not need to call this
		/// </summary>
		/// <param name="path">path to .State file</param>
		public void OnSaveState(string path);

		/// <summary>
		/// Call this when a state is loaded
		/// In memory states (like with rewind) do not need to call this
		/// </summary>
		/// <param name="path">path to .State file</param>
		public void OnLoadState(string path);
	}
}
