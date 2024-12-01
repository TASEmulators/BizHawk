#nullable enable

using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public sealed class ApiContainer : IDisposable
	{
		public readonly IReadOnlyDictionary<Type, IExternalApi> Libraries;

		public ICommApi Comm
			=> Get<ICommApi>();

		public IEmuClientApi EmuClient
			=> Get<IEmuClientApi>();

		public IEmulationApi Emulation
			=> Get<IEmulationApi>(); // requires IEmulator

		public IGuiApi Gui
			=> Get<IGuiApi>(); // requires IEmulator

		public IInputApi Input
			=> Get<IInputApi>();

		public IJoypadApi Joypad
			=> Get<IJoypadApi>();

		public IMemoryApi Memory
			=> Get<IMemoryApi>(); // requires IEmulator

		public IMemoryEventsApi? MemoryEvents
			=> TryGet<IMemoryEventsApi>(); // requires IDebuggable

		public IMemorySaveStateApi? MemorySaveState
			=> TryGet<IMemorySaveStateApi>(); // requires IStatable

		public IMovieApi Movie
			=> Get<IMovieApi>();

		public ISaveStateApi SaveState
			=> Get<ISaveStateApi>();

		public ISQLiteApi SQLite
			=> Get<ISQLiteApi>();

		public IUserDataApi UserData
			=> Get<IUserDataApi>();

		public IToolApi Tool
			=> Get<IToolApi>();

		public ApiContainer(IReadOnlyDictionary<Type, IExternalApi> libs) => Libraries = libs;

		public void Dispose()
		{
			foreach (var lib in Libraries.Values) if (lib is IDisposable disposableLib) disposableLib.Dispose();
		}

		public T Get<T>()
			where T : class, IExternalApi
			=> (T) Libraries[typeof(T)];

		public T? TryGet<T>()
			where T : class, IExternalApi
			=> Libraries.TryGetValue(typeof(T), out var inst) ? (T) inst : null;
	}
}
