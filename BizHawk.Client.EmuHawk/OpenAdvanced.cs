using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

using BizHawk.Emulation.Cores;

using Newtonsoft.Json;

//this file contains some cumbersome self-"serialization" in order to gain a modicum of control over what the serialized output looks like
//I don't want them to look like crufty json

namespace BizHawk.Client.EmuHawk
{
	public interface IOpenAdvanced
	{
		string TypeName { get; }
		string DisplayName { get; }

		/// <summary>
		/// returns a sole path to use for opening a rom (not sure if this is a good idea)
		/// </summary>
		string SimplePath { get; }
		
		void Deserialize(string str);
		void Serialize(TextWriter tw);
	}

	public interface IOpenAdvancedLibretro
	{
		string CorePath { get; set;  }
	}

	public static class OpenAdvancedTypes
	{
		public const string OpenRom = "OpenRom";
		public const string Libretro = "Libretro";
		public const string LibretroNoGame = "LibretroNoGame";
	}


	public class OpenAdvancedSerializer
	{

		public static IOpenAdvanced ParseWithLegacy(string text)
		{
			if (text.StartsWith("*"))
				return Deserialize(text.Substring(1));
			else return new OpenAdvanced_OpenRom { Path = text };
		}

		private static IOpenAdvanced Deserialize(string text)
		{
			int idx = text.IndexOf('*');
			string type = text.Substring(0, idx);
			string token = text.Substring(idx + 1);
			IOpenAdvanced ioa;
			if (type == OpenAdvancedTypes.OpenRom) ioa = new OpenAdvanced_OpenRom();
			else if (type == OpenAdvancedTypes.Libretro) ioa = new OpenAdvanced_Libretro();
			else if (type == OpenAdvancedTypes.LibretroNoGame) ioa = new OpenAdvanced_LibretroNoGame();
			else ioa = null;
			if (ioa == null)
				throw new InvalidOperationException("IOpenAdvanced deserialization error");
			ioa.Deserialize(token);
			return ioa;
		}

		public static string Serialize(IOpenAdvanced ioa)
		{
			StringWriter sw = new StringWriter();
			sw.Write("{0}*", ioa.TypeName);
			ioa.Serialize(sw);
			return sw.ToString();
		}
	}

	class OpenAdvanced_Libretro : IOpenAdvanced, IOpenAdvancedLibretro
	{
		public OpenAdvanced_Libretro()
		{
		}

		public struct Token
		{
			public string Path, CorePath;
		}
		public Token token = new Token();

		public string TypeName { get { return "Libretro"; } }
		public string DisplayName { get { return string.Format("{0}:{1}", Path.GetFileNameWithoutExtension(token.CorePath), token.Path); } }
		public string SimplePath { get { return token.Path; } }

		public void Deserialize(string str)
		{
			token = JsonConvert.DeserializeObject<Token>(str);
		}
		
		public void Serialize(TextWriter tw)
		{
			tw.Write(JsonConvert.SerializeObject(token));
		}

		public string CorePath { get { return token.CorePath; } set { token.CorePath = value; } }
	}

	class OpenAdvanced_LibretroNoGame : IOpenAdvanced, IOpenAdvancedLibretro
	{
		//you might think ideally we'd fetch the libretro core name from the core info inside it
		//but that would involve spinning up excess libretro core instances, which probably isnt good for stability, no matter how much we wish otherwise, not to mention slow.
		//moreover it's kind of complicated here,
		//and finally, I think the Displayname should really be file-based in all cases, since the user is going to be loading cores by filename and 
		//this is related to the recent roms filename management. 
		//so, leave it.

		public OpenAdvanced_LibretroNoGame()
		{
		}

		public OpenAdvanced_LibretroNoGame(string corepath)
		{
			_corePath = corepath;
		}

		string _corePath;

		public string TypeName { get { return "LibretroNoGame"; } }
		public string DisplayName { get { return Path.GetFileName(_corePath); } } //assume we like the filename of the core
		public string SimplePath { get { return ""; } } //effectively a signal to not use a game

		public void Deserialize(string str)
		{
			_corePath = str;
		}

		public void Serialize(TextWriter tw)
		{
			tw.Write(_corePath);
		}

		public string CorePath { get { return _corePath; } set { _corePath = value; } }
	}

	class OpenAdvanced_OpenRom : IOpenAdvanced
	{
		public OpenAdvanced_OpenRom()
		{}

		public string Path;

		public string TypeName { get { return "OpenRom"; } }
		public string DisplayName { get { return Path; } }
		public string SimplePath { get { return Path; } }

		public void Deserialize(string str)
		{
			Path = str;
		}

		public void Serialize(TextWriter tw)
		{
			tw.Write(Path);
		}
	}
}