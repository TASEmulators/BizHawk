#nullable enable

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Fluent.Net;

namespace BizHawk.Client.DiscoHawk
{
	public sealed class ArgsDict : Dictionary<string, object?>
	{
		public ArgsDict(int? tabCount = null)
		{
			Add(nameof(tabCount), tabCount);
		}
	}

	public readonly struct MultiMessageContext
	{
		private static IReadOnlyList<MessageContext> ReadEmbeddedAndConcat(string lang, MessageContext[] overlays)
		{
			MessageContext mc = new(lang, new() { UseIsolating = false });
			Stream embeddedStream;
			try
			{
				embeddedStream = ReflectionCache.EmbeddedResourceStream($"locale.{lang}.ftl");
			}
			catch (ArgumentException e)
			{
				Console.WriteLine(e);
				return overlays;
			}
			using StreamReader sr = new(embeddedStream);
			var errors = mc.AddMessages(sr);
			foreach (var error in errors) Console.WriteLine(error);
			return [ ..overlays, mc ];
		}

		private readonly IReadOnlyList<MessageContext> _contexts;

		public readonly CultureInfo Culture;

		public MultiMessageContext(IReadOnlyList<MessageContext> contexts)
		{
			_contexts = contexts;
			Culture = new(_contexts.FirstOrDefault()?.Locales?.First());
		}

		public MultiMessageContext(string lang, params MessageContext[] overlays)
			: this(ReadEmbeddedAndConcat(lang, overlays)) {}

		public string? this[string id]
			=> GetString(id);

		public string? GetString(
			string id,
			IDictionary<string, object?>? args = null,
			ICollection<FluentError>? errors = null)
		{
			foreach (var context in _contexts)
			{
				var msg = context.GetMessage(id);
				if (msg is not null) return context.Format(msg, args, errors);
			}
			return null;
		}
	}

	public static class Sample
	{
		public static void RunAll()
		{
			static void RunTest(string lang)
			{
				MultiMessageContext translator = new(lang);
				Console.WriteLine($"\n{lang}:");
				Console.WriteLine($"tabs-close-button = {translator.GetString("tabs-close-button")}");
				Console.WriteLine(
					"tabs-close-tooltip ($tabCount = 1) = "
						+ translator.GetString("tabs-close-tooltip", new ArgsDict(tabCount: 1)));
				Console.WriteLine(
					"tabs-close-tooltip ($tabCount = 2) = "
						+ translator.GetString("tabs-close-tooltip", new ArgsDict(tabCount: 2)));
				Console.WriteLine(
					"tabs-close-warning ($tabCount = 1) = "
						+ translator.GetString("tabs-close-warning", new ArgsDict(tabCount: 1)));
				Console.WriteLine(
					"tabs-close-warning ($tabCount = 2) = "
						+ translator.GetString("tabs-close-warning", new ArgsDict(tabCount: 2)));
				Console.WriteLine($"sync-dialog-title = {translator.GetString("sync-dialog-title")}");
				Console.WriteLine($"sync-headline-title = {translator.GetString("sync-headline-title")}");
				Console.WriteLine($"sync-signedout-title = {translator.GetString("sync-signedout-title")}");
			}
			RunTest("en");
			RunTest("it");
			RunTest("pl");
		}
	}
}
