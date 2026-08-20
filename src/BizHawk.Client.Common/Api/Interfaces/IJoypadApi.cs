using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	/// <summary>for querying or modifying virtual input</summary>
	/// <seealso cref="IInputApi"/>
	public interface IJoypadApi : IExternalApi
	{
		IReadOnlyDictionary<string, object> Get(int? controller = null);
		IReadOnlyDictionary<string, object> GetWithMovie(int? controller = null);
		IReadOnlyDictionary<string, object> GetImmediate(int? controller = null);

		/// <summary>
		/// Sets the input for the current frame, as if the inputs came from the user. String will be interpreted the same way an entry from a movie input log would be.
		/// </summary>
		void SetFromMnemonicStr(string inputLogEntry);

		/// <summary>
		/// Sets the given buttons to their provided values for the current frame, as if the inputs came from the user. Any buttons previously set but missing from the given dictionary will be unset.
		/// </summary>
		void Set(IReadOnlyDictionary<string, bool> buttons, int? controller = null);

		/// <summary>
		/// Sets the given button to the provided state for the current frame, as if the input came from the user.
		/// </summary>
		void Set(string button, bool? state = null, int? controller = null);

		/// <summary>
		/// Sets the given analog controls to their provided values for the current frame, as if the inputs came from the user. Any analog inputs previously set but missing from the given dictionary will be unset.
		/// </summary>
		void SetAnalog(IReadOnlyDictionary<string, int> controls, int? controller = null);

		/// <summary>
		/// Sets the given analog control to the provided value for the current frame, as if the input came from the user.
		/// </summary>
		void SetAnalog(string control, int? value = null, int? controller = null);
	}
}
