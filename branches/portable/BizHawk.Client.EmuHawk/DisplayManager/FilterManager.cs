//https://github.com/Themaister/RetroArch/wiki/GLSL-shaders
//https://github.com/Themaister/Emulator-Shader-Pack/blob/master/Cg/README
//https://github.com/libretro/common-shaders/

using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;

using BizHawk.Common;
using BizHawk.Client.Common;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Bizware.BizwareGL.Drivers.OpenTK;

using OpenTK;
using OpenTK.Graphics;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// 
	/// </summary>
	class RetroShaderChain : IDisposable
	{
		public RetroShaderChain(IGL owner, RetroShaderPreset preset, string baseDirectory, bool debug = false)
		{
			Owner = owner;
			this.Preset = preset;
			Passes = preset.Passes.ToArray();

			bool ok = true;

			//load up the shaders
			Shaders = new RetroShader[preset.Passes.Count];
			for(int i=0;i<preset.Passes.Count;i++)
			{
				RetroShaderPreset.ShaderPass pass = preset.Passes[i];

				//acquire content
				string path = Path.Combine(baseDirectory, pass.ShaderPath);
				string content = File.ReadAllText(path);

				var shader = new RetroShader(Owner, content, debug);
				Shaders[i] = shader;
				if (!shader.Pipeline.Available)
					ok = false;
			}

			Available = ok;
		}

		public void Dispose()
		{
			//todo
		}

		/// <summary>
		/// Whether this shader chain is available (it wont be available if some resources failed to load or compile)
		/// </summary>
		public bool Available { get; private set; }

		public readonly IGL Owner;
		public readonly RetroShaderPreset Preset;
		public readonly RetroShader[] Shaders;
		public readonly RetroShaderPreset.ShaderPass[] Passes;
	}

	class RetroShaderPreset
	{
		/// <summary>
		/// Parses an instance from a stream to a CGP file
		/// </summary>
		public RetroShaderPreset(Stream stream)
		{
			var content = new StreamReader(stream).ReadToEnd();
			Dictionary<string,string> dict = new Dictionary<string,string>();

			//parse the key-value-pair format of the file
			content = content.Replace("\r", "");
			foreach (var _line in content.Split('\n'))
			{
				var line = _line.Trim();
				if(line.StartsWith("#")) continue; //lines that are solely comments
				if (line == "") continue; //empty line
				int eq = line.IndexOf('=');
				var key = line.Substring(0,eq).Trim();
				var value = line.Substring(eq + 1).Trim();
				int quote = value.IndexOf('\"');
				if (quote != -1)
					value = value.Substring(quote + 1, value.IndexOf('\"', quote + 1) - (quote + 1));
				else
				{
					//remove comments from end of value. exclusive from above condition, since comments after quoted strings would be snipped by the quoted string extraction
					int hash = value.IndexOf('#');
					if (hash != -1)
						value = value.Substring(0, hash);
					value = value.Trim();
				}
				dict[key.ToLower()] = value;
			}

			//process the keys
			int nShaders = FetchInt(dict, "shaders", 0);
			for (int i = 0; i < nShaders; i++)
			{
				ShaderPass sp = new ShaderPass();
				sp.Index = i;
				Passes.Add(sp);

				sp.InputFilterLinear = FetchBool(dict, "filter_linear" + i, false); //Should this value not be defined, the filtering option is implementation defined.
				sp.OuputFloat = FetchBool(dict, "float_framebuffer" + i, false);
				sp.FrameCountMod = FetchInt(dict, "frame_count_mod" + i, 1);
				sp.ShaderPath = FetchString(dict, "shader" + i, "?"); //todo - change extension to .cg for better compatibility? just change .cg to .glsl transparently at last second?

				//If no scale type is assumed, it is assumed that it is set to "source" with scaleN set to 1.0.
				//It is possible to set scale_type_xN and scale_type_yN to specialize the scaling type in either direction. scale_typeN however overrides both of these.
				sp.ScaleTypeX = (ScaleType)Enum.Parse(typeof(ScaleType), FetchString(dict, "scale_type_x" + i, "Source"), true); 
				sp.ScaleTypeY = (ScaleType)Enum.Parse(typeof(ScaleType), FetchString(dict, "scale_type_y" + i, "Source"), true);
				ScaleType st = (ScaleType)Enum.Parse(typeof(ScaleType), FetchString(dict, "scale_type" + i, "NotSet"), true);
				if (st != ScaleType.NotSet)
					sp.ScaleTypeX = sp.ScaleTypeY = st;

				//scaleN controls both scaling type in horizontal and vertical directions. If scaleN is defined, scale_xN and scale_yN have no effect.
				sp.Scale.X = FetchFloat(dict, "scale_x" + i, 1);
				sp.Scale.Y = FetchFloat(dict, "scale_y" + i, 1);
				float scale = FetchFloat(dict, "scale" + i, -999);
				if(scale != -999) 
					sp.Scale.X = sp.Scale.Y = FetchFloat(dict,"scale" + i, 1);

				//TODO - LUTs
			}
		}

		public List<ShaderPass> Passes = new List<ShaderPass>();

		public enum ScaleType
		{
			NotSet, Source, Viewport, Absolute
		}

		public class ShaderPass
		{
			public int Index;
			public string ShaderPath;
			public bool InputFilterLinear;
			public bool OuputFloat;
			public int FrameCountMod;
			public ScaleType ScaleTypeX;
			public ScaleType ScaleTypeY;
			public Vector2 Scale;
		}

		string FetchString(Dictionary<string, string> dict, string key, string @default)
		{
			string str;
			if (dict.TryGetValue(key, out str))
				return str;
			else return @default;
		}

		int FetchInt(Dictionary<string, string> dict, string key, int @default)
		{
			string str;
			if (dict.TryGetValue(key, out str))
				return int.Parse(str);
			else return @default;
		}

		float FetchFloat(Dictionary<string, string> dict, string key, float @default)
		{
			string str;
			if (dict.TryGetValue(key, out str))
				return float.Parse(str);
			else return @default;
		}

		bool FetchBool(Dictionary<string, string> dict, string key, bool @default)
		{
			string str;
			if (dict.TryGetValue(key, out str))
				return ParseBool(str);
			else return @default;
		}


		bool ParseBool(string value)
		{
			if (value == "1") return true;
			if (value == "0") return false;
			value = value.ToLower();
			if (value == "true") return true;
			if (value == "false") return false;
			throw new InvalidOperationException("Unparseable bool in CGP file content");
		}
	}
}

//Here, I started making code to support GUI editing of filter chains.
//I decided to go for broke and implement retroarch's system first, and then the GUI editing should be able to internally produce a metashader

//namespace BizHawk.Client.EmuHawk
//{
//  class FilterManager
//  {
//    class PipelineState
//    {
//      public PipelineState(PipelineState other)
//      {
//        Size = other.Size;
//        Format = other.Format;
//      }
//      public Size Size;
//      public string Format;
//    }

//    abstract class BaseFilter
//    {
//      bool Connect(FilterChain chain, BaseFilter parent)
//      {
//        Chain = chain;
//        Parent = parent;
//        return OnConnect();
//      }

//      public PipelineState OutputState;
//      public FilterChain Chain;
//      public BaseFilter Parent;

//      public abstract bool OnConnect();
//    }

//    class FilterChain
//    {
//      public void AddFilter(BaseFilter filter)
//      {
//      }
//    }

//    class Filter_Grayscale : BaseFilter
//    {
//      public override bool OnConnect()
//      {
//        if(Parent.OutputState.Format != "rgb") return false;
//        OutputState = new PipelineState { Parent.OutputState; }
//      }
//    }

//    class Filter_EmuOutput_RGBA : BaseFilter
//    {
//      public Filter_EmuOutput_RGBA(int width, int height)
//      {
//        OutputState = new PipelineState() { Size = new Size(width, height), Format = "rgb" };
//      }

//      public override bool OnConnect()
//      {
//        return true;
//      }
//    }

//  }
//}