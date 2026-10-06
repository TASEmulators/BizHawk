using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace BizHawk.Client.EmuHawk.TheLocalization
{
	internal static class TheLocalizer
	{
		static TheLocalizer()
		{
			_translaters[""] = new HardEncodingTextTralslaterBase(); //default implementation.

			// 自动扫描并注册翻译器 Automatically scan and register translators
			var assembly = typeof(HardEncodingTextTralslaterBase).Assembly;
			var types = assembly.GetTypes()
				.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(HardEncodingTextTralslaterBase)));
			foreach (var t in types)
			{
				var inst = (HardEncodingTextTralslaterBase) Activator.CreateInstance(t);
				_translaters[inst.cultureName] = inst;
			}
		}
		
		// 本地化翻译器实例表， key 为 cultureName, value 为 翻译器实例。
		// Localization instance dictionary, key is cultureName, and value is translator instance.
		static ConcurrentDictionary<string, HardEncodingTextTralslaterBase> _translaters = new ConcurrentDictionary<string, HardEncodingTextTralslaterBase>();

		static string getCultureName()
		{
			return Thread.CurrentThread.CurrentUICulture.Name;
		}

		// Translate a plain text. 翻译一条纯文本。
		public static string TranslateText(string text)
		{
			string cultureName = getCultureName();
			if (_translaters.TryGetValue(cultureName, out var theTranslater))
			{
				return theTranslater.TranslateText(text);
			}
			return text;
		}

		// Translate a format string. 翻译一条格式化字符串。
		public static string TranslateFormatString(string formatString, params object[] args)
		{
			string cultureName = getCultureName();
			if (_translaters.TryGetValue(cultureName, out var theTranslater))
			{
				return theTranslater.TranslateFormatString(formatString, args);
			}
			return string.Format(formatString, args);
		}

		// 本地化使用的像素字体，fontSize 需要有 16, 20, 25, 32 四个取值。
		// The customized font of the language. fontSize has four values of 16, 20, 25 and 32.
		// 字体名称必须为 XXX{fontSize}px.fnt ，如 test16.fnt 。
		// The font name must be XXX{fontSize}px.fnt, such as "test16.fnt" .
		// 你需要把字体文件组织在 BiaHawk.Client.Common.Resources 文件夹下。
		// You need to put font files in the folder "biahawk.client.common.resources"' .
		// 建议建立 culturalName 文件夹统一组织文件。
		// It is suggested to establish a culturalName folder to organize files in a unified way.
		// 你需要返回字体对于 BiaHawk.Client.Common.Resources 的相对路径和名称，如 zh_CN.XXX 。
		// You need to return the relative path and name of the font to "biahawk.client.com mon.resources" , such as "en_US.XXX" .
		public static string GetCustomizedFontPathAndFirstName()
		{
			string cultureName = getCultureName();
			if (_translaters.TryGetValue(cultureName, out var theTranslater))
			{
				return theTranslater.CustomizedPixelFontPathAndFirstName;
			}
			return null;
		}
	}
}
