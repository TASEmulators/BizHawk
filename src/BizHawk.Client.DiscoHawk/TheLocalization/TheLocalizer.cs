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

			// 扫描当前程序集所有继承自 speakerBase 的类型
			var assembly = typeof(HardEncodingTextTralslaterBase).Assembly;
			var types = assembly.GetTypes()
				.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(HardEncodingTextTralslaterBase)));
			foreach (var t in types)
			{
				var inst = (HardEncodingTextTralslaterBase) Activator.CreateInstance(t);
				_translaters[inst.cultureName] = inst;
				foreach (var kv in inst.cultureNameMapper)
				{
					globleCultureNameMapper[kv.Key] = kv.Value;
				}
			}
		}
		
		// 本地化翻译器实例表， key 为 cultureName, value 为 翻译器实例。
		// Localization instance dictionary, key is cultureName, and value is translator instance.
		static ConcurrentDictionary<string, HardEncodingTextTralslaterBase> _translaters = new ConcurrentDictionary<string, HardEncodingTextTralslaterBase>();

		// Winform 历史遗留问题：在一些情况下，cultureName 会隐式地转换为意料之外的值，所以需要映射处理。
		// Winform legacy: In some cases, cultureName will be implicitly converted to unexpected values, so the mapper is needed.
		static ConcurrentDictionary<string, string> globleCultureNameMapper = new ConcurrentDictionary<string, string>();
		public static string NormalizeCultureName(string cultureName)
		{
			if (globleCultureNameMapper.TryGetValue(cultureName, out var mappedCultureName))
			{
				return mappedCultureName;
			}
			return cultureName;
		}
		static string getCultureName()
		{
			return NormalizeCultureName(Thread.CurrentThread.CurrentUICulture.Name);
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
