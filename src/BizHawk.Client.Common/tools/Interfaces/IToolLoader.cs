using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IToolLoader
	{
		IEnumerable<Type> AvailableTools { get; }

		/// <summary>
		/// Loads the tool dialog T (T must implement <see cref="IToolForm"/>) , if it does not exist it will be created, if it is already open, it will be focused
		/// </summary>
		/// <param name="focus">Define if the tool form has to get the focus or not (Default is true)</param>
		/// <param name="toolPath">Path to the .dll of the external tool</param>
		/// <typeparam name="T">Type of tool you want to load</typeparam>
		/// <returns>An instantiated <see cref="IToolForm"/></returns>
		T Load<T>(bool focus = true, string toolPath = "")
			where T : class, IToolForm;

		/// <summary>
		/// Loads the tool dialog T (T must implements <see cref="IToolForm"/>) , if it does not exist it will be created, if it is already open, it will be focused
		/// This method should be used only if you can't use the generic one
		/// </summary>
		/// <param name="toolType">Type of tool you want to load</param>
		/// <param name="focus">Define if the tool form has to get the focus or not (Default is true)</param>
		/// <returns>An instantiated <see cref="IToolForm"/></returns>
		/// <exception cref="ArgumentException">Raised if <paramref name="toolType"/> can't cast into IToolForm </exception>
		IToolForm Load(Type toolType, bool focus = true);

		void LoadRamWatch(bool loadDialog);
	}
}
