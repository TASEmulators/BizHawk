using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Fluent.Net;

namespace BizHawk.Client.DiscoHawk
{
	public static class Sample
	{
		private static MessageContext GetMessages(string lang)
		{
			//TODO use ReflectionCache
			var asm = typeof(TranslationService).Assembly;
			var stream = asm.GetManifestResourceStream(asm.GetManifestResourceNames().First(s => s.EndsWith($"{lang}.ftl")));

			MessageContext mc = new(lang, new() { UseIsolating = false });
			using StreamReader sr = new(stream);
			var errors = mc.AddMessages(sr);
			foreach (var error in errors) Console.WriteLine(error);
			return mc;
		}

		public static void RunAll()
		{
			RunTest("en");
			RunTest("it");
			RunTest("pl");
		}

		private static void RunTest(string lang)
		{
			TranslationService translator = new([ GetMessages(lang) ]);
			Console.WriteLine($"\n{lang}:");
			Console.WriteLine($"tabs-close-button = {translator.GetString("tabs-close-button")}");
			Console.WriteLine(
				"tabs-close-tooltip ($tabCount = 1) = "
					+ translator.GetString("tabs-close-tooltip", TranslationService.Args("tabCount", 1)));
			Console.WriteLine(
				"tabs-close-tooltip ($tabCount = 2) = "
					+ translator.GetString("tabs-close-tooltip", TranslationService.Args("tabCount", 2)));
			Console.WriteLine(
				"tabs-close-warning ($tabCount = 1) = "
					+ translator.GetString("tabs-close-warning", TranslationService.Args("tabCount", 1)));
			Console.WriteLine(
				"tabs-close-warning ($tabCount = 2) = "
					+ translator.GetString("tabs-close-warning", TranslationService.Args("tabCount", 2)));
			Console.WriteLine($"sync-dialog-title = {translator.GetString("sync-dialog-title")}");
			Console.WriteLine($"sync-headline-title = {translator.GetString("sync-headline-title")}");
			Console.WriteLine($"sync-signedout-title = {translator.GetString("sync-signedout-title")}");
		}
	}

	public sealed class TranslationService(IReadOnlyList<MessageContext> Contexts)
	{
		public static Dictionary<string, object> Args(string name, object value, params object[] args)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(paramName: nameof(name));
			if (value is null) throw new ArgumentNullException(paramName: nameof(value));
			if (args.Length % 2 is not 0)
			{
				throw new ArgumentException(
					paramName: nameof(args),
					message: "Expected a comma separated list " +
					"of name, value arguments but the number of arguments is " +
					"not a multiple of two");
			}

			Dictionary<string, object> argsDic = new() { [name] = value };
			for (int i = 0; i < args.Length; i += 2)
			{
				if (args[i] is not string name1 || string.IsNullOrEmpty(name1))
				{
					throw new ArgumentException(
						paramName: nameof(args),
						message: $"Expected the argument at index {i} to be a non-empty string");
				}
				var value1 = args[i + 1];
				if (value1 is null)
				{
					throw new ArgumentNullException(
						paramName: nameof(args),
						message: $"Expected the argument at index {i + 1} to be a non-null value");
				}
				argsDic.Add(name1, value1);
			}
			return argsDic;
		}

		public string GetString(
			string id,
			IDictionary<string, object> args = null,
			ICollection<FluentError> errors = null)
		{
			foreach (var context in Contexts)
			{
				var msg = context.GetMessage(id);
				if (msg is not null) return context.Format(msg, args, errors);
			}
			return string.Empty;
		}

		public string PreferredLocale
			=> Contexts[0].Locales.First();

		private CultureInfo _culture;

		public CultureInfo Culture
			=> _culture ??= new(PreferredLocale);
	}
}
