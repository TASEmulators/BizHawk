#nullable enable

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;

using Fluent.Net;
using Fluent.Net.RuntimeAst;

namespace BizHawk.Client.DiscoHawk
{
	public sealed class ArgsDict : Dictionary<string, object?>
	{
		public ArgsDict(string? filePath = null)
		{
			Add(nameof(filePath), filePath);
		}
	}

	public readonly struct MultiMessageContext
	{
		public static MultiMessageContext ForCurrentCulture()
		{
			MultiMessageContext mmc = new(CultureInfo.CurrentUICulture.IetfLanguageTag);
			return mmc.Culture is null ? new("en") : mmc;
		}

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

		public readonly CultureInfo? Culture;

		public MultiMessageContext(IReadOnlyList<MessageContext> contexts)
		{
			_contexts = contexts;
			var langcode = _contexts.FirstOrDefault()?.Locales?.First();
			if (langcode is null) return;
			try
			{
				Culture = new(langcode);
			}
			catch (Exception)
			{
				try
				{
					Culture = new(langcode.SubstringBefore('-'));
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			}
		}

		public MultiMessageContext(string lang, params MessageContext[] overlays)
			: this(ReadEmbeddedAndConcat(lang, overlays)) {}

		public string? this[string id]
			=> GetString(id);

		public (string? Caption, string Text) GetDialogText(
			string id,
			IDictionary<string, object?>? args = null,
			ICollection<FluentError>? errors = null)
				=> GetNode(id) is (var msg, var fromCtx)
					? (msg.Attributes.TryGetValue("windowtitle", out var caption)
							? fromCtx.Format(caption, args, errors)
							: null,
						fromCtx.Format(msg, args, errors))
					: (null, string.Empty);

		private (Message Message, MessageContext FromContext)? GetNode(string id)
			=> _contexts.Select(ctx => (Message: ctx.GetMessage(id), FromContext: ctx))
				.FirstOrNull(static tuple => tuple.Message is not null);

		public string? GetString(
			string id,
			IDictionary<string, object?>? args = null,
			ICollection<FluentError>? errors = null)
				=> GetNode(id) is (var msg, var fromCtx) ? fromCtx.Format(msg, args, errors) : null;

		public string? GetWithMnemonic(
			string id,
			IDictionary<string, object?>? args = null,
			ICollection<FluentError>? errors = null)
		{
			const string ATTR_NAME_CHAR = "mnemonic";
			const string ATTR_NAME_INDEX = "mnemonicindex";
			const string ERR_PFX = $"[i18n {nameof(GetWithMnemonic)}]";
			const string ERR_FMT_STR_MNEMONIC_CHAR_ABSENT = $"{ERR_PFX} the char {{0}}.{ATTR_NAME_CHAR} isn't present in the string (wrong case?)";
			const string ERR_FMT_STR_MNEMONIC_CHAR_ISNT = $"{ERR_PFX} {{0}}.{ATTR_NAME_CHAR} is not a single char";
			const string ERR_FMT_STR_MNEMONIC_INDEX_ISNT = $"{ERR_PFX} failed to parse {{0}}.{ATTR_NAME_INDEX} as int";
			const string ERR_FMT_STR_MNEMONIC_INDEX_MISMATCH = $"{ERR_PFX} {{0}}.{ATTR_NAME_INDEX} points to '{{1}}', which doesn't match .{ATTR_NAME_CHAR} (case-sensitive)";
			const string ERR_FMT_STR_MNEMONIC_INDEX_OOR = $"{ERR_PFX} {{0}}.{ATTR_NAME_INDEX} out of range 0..<{{1}}";
			if (GetNode(id) is not (var msg, var fromCtx)) return null;
			string? GetAttr(string attrID)
				=> msg.Attributes?.TryGetValue(attrID, out var mn) is true ? fromCtx.Format(mn, args, errors) : null;
			var strWithoutMn = fromCtx.Format(msg, args, errors);
			string? Fail(string msgFmtStr, object? extraArg = null)
			{
				Console.WriteLine(string.Format(msgFmtStr, id, extraArg));
				return strWithoutMn;
			}
			//TODO make the char searches case-insensitive?
			if (GetAttr(ATTR_NAME_INDEX) is string iAmpStr)
			{
				// "index" is in `char`s, not graphemes... but I think that's fine, since the mnemonic feature probably only works with Latin/ext anyway --yoshi
				if (!int.TryParse(iAmpStr, out var iAmp)) return Fail(ERR_FMT_STR_MNEMONIC_INDEX_ISNT);
				if (iAmp < 0 || strWithoutMn.Length <= iAmp) return Fail(ERR_FMT_STR_MNEMONIC_INDEX_OOR, strWithoutMn.Length);
				if (GetAttr(ATTR_NAME_CHAR) is string mnChar1)
				{
					if (mnChar1.Length is not 1) return Fail(ERR_FMT_STR_MNEMONIC_CHAR_ISNT);
					var mnChar2 = mnChar1[0];
					if (strWithoutMn[iAmp] != mnChar2) return Fail(ERR_FMT_STR_MNEMONIC_INDEX_MISMATCH, mnChar2);
				}
//				if (strWithoutMn.Contains('&')) Fail(); //TODO do this?
				return strWithoutMn.Insert(iAmp, "&");
			}
			if (GetAttr(ATTR_NAME_CHAR) is string mnChar)
			{
				if (mnChar.Length is not 1) return Fail(ERR_FMT_STR_MNEMONIC_CHAR_ISNT);
//				if (strWithoutMn.Contains('&')) Fail(); //TODO do this?
				var strWithMn = strWithoutMn.InsertBefore(mnChar[0], "&", out var found);
				return found ? strWithMn : Fail(ERR_FMT_STR_MNEMONIC_CHAR_ABSENT);
			}
			return strWithoutMn;
		}
	}
}
