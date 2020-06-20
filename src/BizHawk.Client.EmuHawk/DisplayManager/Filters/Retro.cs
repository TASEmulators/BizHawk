// https://github.com/Themaister/RetroArch/wiki/GLSL-shaders
// https://github.com/Themaister/Emulator-Shader-Pack/blob/master/Cg/README
// https://github.com/libretro/common-shaders/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using BizHawk.Client.EmuHawk.FilterManager;

using BizHawk.Bizware.BizwareGL;
using OpenTK;

namespace BizHawk.Client.EmuHawk.Filters
{
	public class RetroShaderChain : IDisposable
	{
		private static readonly Regex RxInclude = new Regex(@"^(\s)?\#include(\s)+(""|<)(.*)?(""|>)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
		static string ResolveIncludes(string content, string baseDirectory)
		{
			for (; ; )
			{
				var match = RxInclude.Match(content);
				if(match.Value == "") break;
				string fname = match.Groups[4].Value;
				fname = Path.Combine(baseDirectory,fname);
				string includedContent = ResolveIncludes(File.ReadAllText(fname),Path.GetDirectoryName(fname));
				content = content.Substring(0, match.Index) + includedContent + content.Substring(match.Index + match.Length);
			}
			return content;
		}

		public RetroShaderChain(IGL owner, RetroShaderPreset preset, string baseDirectory, bool debug = false)
		{
			Owner = owner;
			Preset = preset;
			Passes = preset.Passes.ToArray();
			Errors = "";

			//load up the shaders
			Shaders = new RetroShader[preset.Passes.Count];
			for (var i = 0; i < preset.Passes.Count; i++)
			{
				//acquire content. we look for it in any reasonable filename so that one preset can bind to multiple shaders
				string content;
				var path = Path.Combine(baseDirectory, preset.Passes[i].ShaderPath);
				if (!File.Exists(path))
				{
					if (!Path.HasExtension(path))
						path += ".cg";
					if (!File.Exists(path))
					{
						if (owner.API == "OPENGL")
							path = Path.ChangeExtension(path, ".glsl");
						else
							path = Path.ChangeExtension(path, ".hlsl");
					}
				}
				try
				{
					content = ResolveIncludes(File.ReadAllText(path), Path.GetDirectoryName(path));
				}
				catch (DirectoryNotFoundException e)
				{
					Errors += $"caught {nameof(DirectoryNotFoundException)}: {e.Message}\n";
					return;
				}
				catch (FileNotFoundException e)
				{
					Errors += $"could not read file {e.FileName}\n";
					return;
				}

				var shader = Shaders[i] = new RetroShader(Owner, content, debug);
				if (!shader.Available)
				{
					Errors += $"===================\r\nPass {i}:\r\n{shader.Errors}\n";
					return;
				}
			}

			Available = true;
		}

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			foreach (var s in Shaders)
			{
				s.Dispose();
			}

			_isDisposed = true;
		}

		/// <summary>Whether this shader chain is available (it wont be available if some resources failed to load or compile)</summary>
		public readonly bool Available;
		public readonly string Errors;
		public readonly IGL Owner;
		public readonly RetroShaderPreset Preset;
		public readonly RetroShader[] Shaders;
		public readonly RetroShaderPreset.ShaderPass[] Passes;

		private bool _isDisposed;
	}

	public class RetroShaderPreset
	{
		/// <summary>
		/// Parses an instance from a stream to a CGP file
		/// </summary>
		public RetroShaderPreset(Stream stream)
		{
			var content = new StreamReader(stream).ReadToEnd();
			var dict = new Dictionary<string, string>();

			//parse the key-value-pair format of the file
			content = content.Replace("\r", "");
			foreach (var splitLine in content.Split('\n'))
			{
				var line = splitLine.Trim();
				if (line.StartsWith("#")) continue; //lines that are solely comments
				if (line == "") continue; //empty line
				int eq = line.IndexOf('=');
				var key = line.Substring(0, eq).Trim();
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

			// process the keys
			int nShaders = FetchInt(dict, "shaders", 0);
			for (int i = 0; i < nShaders; i++)
			{
				var sp = new ShaderPass { Index = i };
				Passes.Add(sp);

				sp.InputFilterLinear = FetchBool(dict, $"filter_linear{i}", false); //Should this value not be defined, the filtering option is implementation defined.
				sp.OuputFloat = FetchBool(dict, $"float_framebuffer{i}", false);
				sp.FrameCountMod = FetchInt(dict, $"frame_count_mod{i}", 1);
				sp.ShaderPath = FetchString(dict, $"shader{i}", "?"); //todo - change extension to .cg for better compatibility? just change .cg to .glsl transparently at last second?

				// If no scale type is assumed, it is assumed that it is set to "source" with scaleN set to 1.0.
				// It is possible to set scale_type_xN and scale_type_yN to specialize the scaling type in either direction. scale_typeN however overrides both of these.
				sp.ScaleTypeX = (ScaleType)Enum.Parse(typeof(ScaleType), FetchString(dict, $"scale_type_x{i}", "Source"), true);
				sp.ScaleTypeY = (ScaleType)Enum.Parse(typeof(ScaleType), FetchString(dict, $"scale_type_y{i}", "Source"), true);
				ScaleType st = (ScaleType)Enum.Parse(typeof(ScaleType), FetchString(dict, $"scale_type{i}", "NotSet"), true);
				if (st != ScaleType.NotSet)
					sp.ScaleTypeX = sp.ScaleTypeY = st;

				// scaleN controls both scaling type in horizontal and vertical directions. If scaleN is defined, scale_xN and scale_yN have no effect.
				sp.Scale.X = FetchFloat(dict, $"scale_x{i}", 1);
				sp.Scale.Y = FetchFloat(dict, $"scale_y{i}", 1);
				float scale = FetchFloat(dict, $"scale{i}", -999);
				if (scale != -999)
				{
					sp.Scale.X = sp.Scale.Y = FetchFloat(dict, $"scale{i}", 1);
				}

				//TODO - LUTs
			}
		}

		public List<ShaderPass> Passes { get; set; } = new List<ShaderPass>();


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

		private string FetchString(IDictionary<string, string> dict, string key, string @default)
		{
			return dict.TryGetValue(key, out var str) ? str : @default;
		}

		private int FetchInt(IDictionary<string, string> dict, string key, int @default)
		{
			return dict.TryGetValue(key, out var str) ? int.Parse(str) : @default;
		}

		private float FetchFloat(IDictionary<string, string> dict, string key, float @default)
		{
			return dict.TryGetValue(key, out var str) ? float.Parse(str) : @default;
		}

		private bool FetchBool(IDictionary<string, string> dict, string key, bool @default)
		{
			return dict.TryGetValue(key, out var str) ? ParseBool(str) : @default;
		}

		private bool ParseBool(string value)
		{
			if (value == "1") return true;
			if (value == "0") return false;
			value = value.ToLower();
			if (value == "true") return true;
			if (value == "false") return false;
			throw new InvalidOperationException("Unparsable bool in CGP file content");
		}
	}

	public class RetroShaderPass : BaseFilter
	{
		private readonly RetroShaderChain _rsc;
		private readonly RetroShaderPreset.ShaderPass _sp;
		private readonly int _rsi;
		private Size _outputSize;

		public override string ToString() => $"{nameof(RetroShaderPass)}[#{_rsi}]";

		public RetroShaderPass(RetroShaderChain rsc, int index)
		{
			_rsc = rsc;
			_rsi = index;
			_sp = _rsc.Passes[index];
		}

		public override void Initialize()
		{
			DeclareInput(SurfaceDisposition.Texture);
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			Size inSize = state.SurfaceFormat.Size;
			if (_sp.ScaleTypeX == RetroShaderPreset.ScaleType.Absolute) _outputSize.Width = (int)_sp.Scale.X;
			if (_sp.ScaleTypeY == RetroShaderPreset.ScaleType.Absolute) _outputSize.Width = (int)_sp.Scale.Y;
			if (_sp.ScaleTypeX == RetroShaderPreset.ScaleType.Source) _outputSize.Width = (int)(inSize.Width * _sp.Scale.X);
			if (_sp.ScaleTypeY == RetroShaderPreset.ScaleType.Source) _outputSize.Height = (int)(inSize.Height * _sp.Scale.Y);

			DeclareOutput(new SurfaceState
			{
				SurfaceFormat = new SurfaceFormat(_outputSize),
				SurfaceDisposition = SurfaceDisposition.RenderTarget
			});
		}

		public override Size PresizeOutput(string channel, Size size)
		{
			_outputSize = size;
			return size;
		}

		public override Size PresizeInput(string channel, Size inSize)
		{
			Size outsize = inSize;
			if (_sp.ScaleTypeX == RetroShaderPreset.ScaleType.Absolute) outsize.Width = (int)_sp.Scale.X;
			if (_sp.ScaleTypeY == RetroShaderPreset.ScaleType.Absolute) outsize.Width = (int)_sp.Scale.Y;
			if (_sp.ScaleTypeX == RetroShaderPreset.ScaleType.Source) outsize.Width = (int)(inSize.Width * _sp.Scale.X);
			if (_sp.ScaleTypeY == RetroShaderPreset.ScaleType.Source) outsize.Height = (int)(inSize.Height * _sp.Scale.Y);
			return outsize;
		}

		public override void Run()
		{
			var shader = _rsc.Shaders[_rsi];
			shader.Bind();

			// apply all parameters to this shader.. even if it was meant for other shaders. kind of lame.
			if(Parameters != null)
			{ 
				foreach (var kvp in Parameters)
				{
					if (kvp.Value is float value)
					{
						shader.Pipeline[kvp.Key].Set(value);
					}
				}
			}

			var input = InputTexture;
			if (_sp.InputFilterLinear)
			{
				InputTexture.SetFilterLinear();
			}
			else
			{
				InputTexture.SetFilterNearest();
			}

			_rsc.Shaders[_rsi].Run(input, input.Size, _outputSize, InputTexture.IsUpsideDown);

			// maintain invariant.. i think.
			InputTexture.SetFilterNearest();
		}
	}
}
