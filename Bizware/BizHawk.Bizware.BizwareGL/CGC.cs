using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

//todo - be able to run out of PATH too

namespace BizHawk.Bizware.BizwareGL
{
	public class CGC
	{
		public CGC()
		{
		}

		public static string CGCBinPath;

		private static string[] Escape(IEnumerable<string> args)
		{
			return args.Select(s => s.Contains(" ") ? string.Format("\"{0}\"", s) : s).ToArray();
		}

		public class Results
		{
			public bool Succeeded;
			public string Code, Errors;
			public Dictionary<string, string> MapCodeToNative = new Dictionary<string, string>();
			public Dictionary<string, string> MapNativeToCode = new Dictionary<string, string>();
		}

		Regex rxHlslSamplerCrashWorkaround = new Regex(@"\((.*?)(in sampler2D)(.*?)\)", RegexOptions.Multiline | RegexOptions.IgnoreCase);

		public Results Run(string code, string entry, string profile, bool hlslHacks)
		{
			//version=110; GLSL generates old fashioned semantic attributes and not generic attributes
			string[] args = new[]{"-profile", profile, "-entry", entry, "-po", "version=110"};

			args = Escape(args);
			StringBuilder sbCmdline = new StringBuilder();
			for (int i = 0; i < args.Length; i++)
			{
				sbCmdline.Append(args[i]);
				if (i != args.Length - 1) sbCmdline.Append(' ');
			}

			//http://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
			using (Process proc = new Process())
			{
				proc.StartInfo.UseShellExecute = false;
				proc.StartInfo.CreateNoWindow = true;
				proc.StartInfo.RedirectStandardInput = true;
				proc.StartInfo.RedirectStandardOutput = true;
				proc.StartInfo.RedirectStandardError = true;
				proc.StartInfo.Arguments = sbCmdline.ToString();
				proc.StartInfo.FileName = CGCBinPath;

				StringBuilder output = new StringBuilder(), error = new StringBuilder();

				using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
				using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
				{
					proc.OutputDataReceived += (sender, e) =>
					{
						if (e.Data == null) outputWaitHandle.Set();
						else output.AppendLine(e.Data);
					};
					proc.ErrorDataReceived += (sender, e) =>
					{
						if (e.Data == null) errorWaitHandle.Set();
						else error.AppendLine(e.Data);
					};


					proc.Start();
					new Thread(() =>
					{
						proc.StandardInput.AutoFlush = true;
						proc.StandardInput.Write(code);
						proc.StandardInput.Flush();
						proc.StandardInput.Close();
					}).Start();

					proc.BeginOutputReadLine();
					proc.BeginErrorReadLine();
					proc.WaitForExit();
					outputWaitHandle.WaitOne();
					errorWaitHandle.WaitOne();
				}

				bool ok = (proc.ExitCode == 0);

				var ret = new Results()
				{
					Succeeded = ok,
					Code = output.ToString(),
					Errors = error.ToString()
				};

				if (!ok)
					Console.WriteLine(ret.Errors);

				if (hlslHacks)
				{
					ret.Code = rxHlslSamplerCrashWorkaround.Replace(ret.Code, m => string.Format("({0}uniform sampler2D{1})", m.Groups[1].Value, m.Groups[3].Value));
				}

				//make variable name map
				//loop until the first line that doesnt start with a comment
				var reader = new StringReader(ret.Code);
				for(;;)
				{
					var line = reader.ReadLine();
					if (line == null) break;
					if (!line.StartsWith("//")) break;
					if (!line.StartsWith("//var")) continue;
					var parts = line.Split(':');
					var native_name = parts[0].Split(' ')[2];
					var code_name = parts[1].Trim();
					if (code_name.StartsWith("TEXUNIT")) code_name = ""; //need parsing differently
					if (code_name == "")
						code_name = parts[2].Trim();
					//remove some array indicators. example: `modelViewProj1[0], 4`
					code_name = code_name.Split(',')[0];
					code_name = code_name.Split(' ')[0];
					if (code_name != "")
					{
						ret.MapCodeToNative[code_name] = native_name;
						ret.MapNativeToCode[native_name] = code_name;
					}
				}

				return ret;
			}
		}
	}
}
