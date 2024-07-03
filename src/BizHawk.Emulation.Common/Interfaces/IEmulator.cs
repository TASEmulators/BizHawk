#nullable disable

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service defines a core as a core. It is the primary service
	/// and the absolute minimum requirement to have a functional core in BizHawk
	/// a client can not operate without this minimum requirement
	/// </summary>
	public interface IEmulator : IEmulatorService, IDisposable
	{
		/// <summary>
		/// Gets the service provider.
		/// This is the intended mechanism to get services from a core
		/// Retrieves an IEmulatorService from the core,
		/// if the core does not have the type specified, it will return null
		/// </summary>
		IEmulatorServiceProvider ServiceProvider { get; }

		/// <summary>
		/// Gets the definition that defines all the possible inputs and types that the core can receive
		/// By design this should not change during the lifetime of the instance of the core
		/// To change the definition, a new instance should be created
		/// </summary>
		ControllerDefinition ControllerDefinition { get; }

		/// <summary>
		/// Runs the emulator core for 1 frame
		/// note that (some?) cores expect you to call SoundProvider.GetSamples() after each FrameAdvance()
		/// please do this, even when <paramref name="renderSound"/> = false
		/// </summary>
		/// <param name="controller">
		/// The <see cref="IController"/> instance that the core will use for input.
		/// The <see cref="IController"/> provided by the client must provide the buttons specified by the core through the <see cref="ControllerDefinition"/> property
		/// </param>
		/// <param name="render">Whether or not to render video, cores will pass false here in cases such as frame skipping</param>
		/// <param name="renderSound">Whether or not to render audio, cores will pass here false here in cases such as fast forwarding where bypassing sound may improve speed</param>
		bool FrameAdvance(IController controller, bool render, bool renderSound = true);

		/// <summary>
		/// Gets the current frame count
		/// Frame must be incremented after emulation in the core
		/// </summary>
		int Frame { get; }

		/// <summary>
		/// Gets the unique Id of the platform currently being emulated, for instance "NES"
		/// </summary>
		string SystemId { get; }

		/// <summary>
		/// Gets a value indicating whether the core is in deterministic mode.
		/// This flag is a contract with the client.
		/// If true, the core agrees to behave in a completely deterministic manner,
		/// Features like movie recording depend on this.
		/// It is the client's responsibility to manage this flag.
		/// If a core wants to implement non-deterministic features (like speed hacks, frame-skipping), it must be done only when this flag is false
		/// if you want to set this, look in the emulator's constructor or Load() method
		/// </summary>
		bool DeterministicEmulation { get; }

		/// <summary>
		/// Resets the Frame and Lag counters, and any other similar counters a core might implement
		/// </summary>
		void ResetCounters();
	}
}
