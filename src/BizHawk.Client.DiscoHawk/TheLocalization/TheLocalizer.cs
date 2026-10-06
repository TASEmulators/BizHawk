using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace BizHawk.Client.DiscoHawk.TheLocalization
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
	}
}
