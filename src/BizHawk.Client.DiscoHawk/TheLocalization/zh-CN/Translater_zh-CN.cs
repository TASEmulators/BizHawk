namespace BizHawk.Client.DiscoHawk.TheLocalization.zh_CN
{
	internal class Translater_zh_CN : HardEncodingTextTralslaterBase
	{
		public Translater_zh_CN()
		{
			cultureName = "zh-CN";

			// ====================================================

			theDictionary["Error loading disc"] = "加载磁碟出错";

			theDictionary["FFmpeg missing"] = "未找到 FFmpeg";
			theDictionary["This function requires FFmpeg, but it doesn't appear to have been downloaded.\n"
			+ "EmuHawk can automatically download it: you just need to set up A/V recording with the FFmpeg writer."]
			= "该功能需要 FFmpeg，但尚未下载。\nEmuHawk 可自动下载 FFmpeg：只需在 Bizhawk 配置 AV录制器为 FFmpeg 并开始录制即可。";

			theDictionary["Do you want to overwrite existing files? Choosing \"No\" will simply skip those. You could also \"Cancel\" the extraction entirely.\n\ncaused by file: {0}"]
			= "是否覆盖已有文件？选择“否”将跳过这些文件，选择“取消”则终止全部提取操作。\n\n涉及文件：{0}";
			theDictionary["File to extract already exists"] = "待提取文件已存在";

			theDictionary["DiscoHawk requires no semicolons within its base directory! DiscoHawk will now close."]
			= "DiscoHawk 的根目录路径不能包含分号！DiscoHawk 即将关闭。";
			theDictionary["SetDllDirectoryW failed with error code {0}, this is fatal. DiscoHawk will now close."] = "致命错误：SetDllDirectoryW 失败， 错误码 {0}， DiscoHawk 即将关闭。";
		}
	}
}
