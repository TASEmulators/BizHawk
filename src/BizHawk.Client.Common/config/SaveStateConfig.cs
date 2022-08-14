namespace BizHawk.Client.Common
{
	public class SaveStateConfig
	{
		public const int DefaultCompressionLevelNormal = 1;
		public SaveStateType Type { get; set; } = SaveStateType.Binary;
		public int CompressionLevelNormal { get; set; } = DefaultCompressionLevelNormal;
		public const int DefaultCompressionLevelRewind = 0; // this isn't actually used yet
		public int CompressionLevelRewind { get; set; } = DefaultCompressionLevelRewind; // this isn't actually used yet
		public bool MakeBackups { get; set; } = true;
		public bool SaveScreenshot { get; set; } = true;
		public int BigScreenshotSize { get; set; } = 128 * 1024;
		public bool NoLowResLargeScreenshots { get; set; }
	}
}
