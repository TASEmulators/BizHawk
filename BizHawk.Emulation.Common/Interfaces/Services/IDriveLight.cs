namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Specifies an interface to returning the state of a LED drive light such as on Disk and CD Drives
	/// </summary>
	public interface IDriveLight : IEmulatorService
	{
		/// <summary>
		/// Specifies whether there is currently a Drive light available
		/// </summary>
		bool DriveLightEnabled { get; }

		/// <summary>
		/// Specifies whether the light is currently lit
		/// </summary>
		bool DriveLightOn { get; }
	}
}
