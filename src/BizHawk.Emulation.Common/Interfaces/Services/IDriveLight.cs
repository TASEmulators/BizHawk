namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Specifies an interface for returning the state of a LED drive light such as on Disk and CD Drives,
	/// If available the client will display a light that turns on and off based on the drive light status
	/// </summary>
	public interface IDriveLight : ISpecializedEmulatorService
	{
		/// <summary>
		/// Gets a value indicating whether there is currently a Drive light available
		/// </summary>
		bool DriveLightEnabled { get; }

		/// <summary>
		/// Gets a value indicating whether the light is currently lit
		/// </summary>
		bool DriveLightOn { get; }

		/// <value>description of the drive light icon (used in MainForm for the tooltip of the status bar icon)</value>
		string DriveLightIconDescription { get; }
	}
}
