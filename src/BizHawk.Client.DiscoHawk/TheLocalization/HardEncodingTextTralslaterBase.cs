using System.Collections.Concurrent;

namespace BizHawk.Client.DiscoHawk.TheLocalization
{
	internal class HardEncodingTextTralslaterBase
	{
		// 将根据系统的 cultureName 字段应用对应的翻译器。
		// The corresponding translator will be applied according to the cultureName.
		internal string cultureName = "default";

		// Winform 历史遗留问题：在一些情况下，cultureName 会隐式地转换为意料之外的值，所以需要映射处理。
		// Winform legacy: In some cases, cultureName will be implicitly converted to unexpected values, so the mapper is needed. 
		internal ConcurrentDictionary<string, string> cultureNameMapper = new ConcurrentDictionary<string, string>();

		// The dictionary used to translate. 字典，文字对照表。
		protected ConcurrentDictionary<string, string> theDictionary = new ConcurrentDictionary<string, string>();

		// Translate a plain text. 翻译一条纯文本。
		public virtual string TranslateText(string text)
		{
			if (theDictionary.TryGetValue(text, out var translatedString)) // If there is pre-translated text in the dictionary, 如果字典中存在翻译前的文本，
			{
				return translatedString; //take out the translated text from it. 则从中取出翻译后的文本。
			}
			return text; // Otherwise, non translate.
		}

		// Translate a format string. 翻译一条格式化字符串。
		public virtual string TranslateFormatString(string formatString, params object[] args)
		{
			if (theDictionary.TryGetValue(formatString, out var translatedString)) // If there is pre-translated text in the dictionary, 如果字典中存在翻译前的文本，
			{
				return string.Format(translatedString, args); //take out the translated text from it. 则从中取出翻译后的文本。
			}
			return string.Format(formatString, args); // Otherwise, non translate.
		}
	}
}
