#nullable enable

using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	/// <remarks>this was easier than trying to make static classes instantiable...</remarks>
	/// TODO: Reconsider if we want to hand over the main form handle
	/// This is only used in DirectInput, and it would work just as fine if a hidden window was created internally in its place
	public interface IHostInputAdapter
	{
		string Desc { get; }

		void DeInitAll();

		void FirstInitAll(IntPtr mainFormHandle);

		/// <remarks>keys are pad prefixes "X# "/"J# " (with the trailing space)</remarks>
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetHapticsChannels();

		void ReInitGamepads(IntPtr mainFormHandle);

		void PreprocessHostGamepads();

		void ProcessHostGamepads(Action<string?, bool, ClientInputFocus> handleButton, Action<string?, int> handleAxis);

		IEnumerable<KeyEvent> ProcessHostKeyboards();

		/// <remarks>implementors may store this for use during the next <see cref="ProcessHostGamepads"/> call</remarks>
		void SetHaptics(IReadOnlyCollection<(string Name, int Strength)> hapticsSnapshot);

		void UpdateConfig(Config config);
	}
}
