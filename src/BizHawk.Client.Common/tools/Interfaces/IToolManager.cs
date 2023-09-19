using System;
using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IToolManager
	{
		/// <summary>
		/// Loads the tool dialog T (T must implements <see cref="IToolForm"/>) , if it does not exist it will be created, if it is already open, it will be focused
		/// This method should be used only if you can't use the generic one
		/// </summary>
		/// <param name="toolType">Type of tool you want to load</param>
		/// <param name="focus">Define if the tool form has to get the focus or not (Default is true)</param>
		/// <returns>An instantiated <see cref="IToolForm"/></returns>
		/// <exception cref="ArgumentException">Raised if <paramref name="toolType"/> can't cast into IToolForm </exception>
		IToolForm Load(Type toolType, bool focus = true);

		/// <summary>
		/// Loads the tool dialog T (T must implement <see cref="IToolForm"/>) , if it does not exist it will be created, if it is already open, it will be focused
		/// </summary>
		/// <param name="focus">Define if the tool form has to get the focus or not (Default is true)</param>
		/// <param name="toolPath">Path to the .dll of the external tool</param>
		/// <typeparam name="T">Type of tool you want to load</typeparam>
		/// <returns>An instantiated <see cref="IToolForm"/></returns>
		T Load<T>(bool focus = true, string toolPath = "")
			where T : class, IToolForm;

		/// <summary>Loads the external tool's entry form.</summary>
		IExternalToolForm LoadExternalToolForm(string toolPath, string customFormTypeName, bool focus = true, bool skipExtToolWarning = false);

		void AutoLoad();

		/// <summary>
		/// Determines whether a given IToolForm is already loaded
		/// </summary>
		/// <typeparam name="T">Type of tool to check</typeparam>
		/// <remarks>yo why do we have 4 versions of this, each with slightly different behaviour in edge cases --yoshi</remarks>
		bool IsLoaded<T>() where T : IToolForm;

		bool IsLoaded(Type toolType);

		bool IsOnScreen(Point topLeft);

		/// <summary>
		/// Returns true if an instance of T exists
		/// </summary>
		/// <typeparam name="T">Type of tool to check</typeparam>
		bool Has<T>() where T : IToolForm;

		/// <returns><see langword="true"/> iff a tool of the given <paramref name="toolType"/> is <see cref="IToolForm.IsActive">active</see></returns>
		bool Has(Type toolType);

		/// <summary>
		/// Gets the instance of T, or creates and returns a new instance
		/// </summary>
		/// <typeparam name="T">Type of tool to get</typeparam>
		IToolForm Get<T>() where T : class, IToolForm;

		/// <summary>
		/// returns the instance of <paramref name="toolType"/>, regardless of whether it's loaded,<br/>
		/// but doesn't create and load a new instance if it's not found
		/// </summary>
		/// <remarks>
		/// does not check <paramref name="toolType"/> is a class implementing <see cref="IToolForm"/>;<br/>
		/// you may pass any class or interface
		/// </remarks>
		IToolForm/*?*/ LazyGet(Type toolType);

		(Image/*?*/ Icon, string Name) GetIconAndNameFor(Type toolType);

		IEnumerable<Type> AvailableTools { get; }

		/// <summary>
		/// Calls UpdateValues() on an instance of T, if it exists
		/// </summary>
		/// <typeparam name="T">Type of tool to update</typeparam>
		void UpdateValues<T>() where T : IToolForm;

		void Restart(Config config, IEmulator emulator, IGameInfo game);

		/// <summary>
		/// Calls Restart() on an instance of T, if it exists
		/// </summary>
		/// <typeparam name="T">Type of tool to restart</typeparam>
		void Restart<T>() where T : IToolForm;

		/// <summary>
		/// Runs AskSave on every tool dialog, false is returned if any tool returns false
		/// </summary>
		bool AskSave();

		/// <summary>
		/// If T exists, this call will close the tool, and remove it from memory
		/// </summary>
		/// <typeparam name="T">Type of tool to close</typeparam>
		void Close<T>() where T : IToolForm;

		void Close(Type toolType);

		void Close();

		void UpdateToolsBefore();

		void UpdateToolsAfter();

		void FastUpdateBefore();

		void FastUpdateAfter();

		bool IsAvailable(Type tool);

		bool IsAvailable<T>();

		void LoadRamWatch(bool loadDialog);

		string GenerateDefaultCheatFilename();

		void UpdateCheatRelatedTools(object sender, CheatCollection.CheatListEventArgs e);

	}
}
