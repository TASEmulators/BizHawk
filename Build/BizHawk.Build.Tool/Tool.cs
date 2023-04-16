using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Build.Tool
{
	class Program
	{
		static void Main(string[] args)
		{
			string cmd = args[0];
			string[] cmdArgs = Crop(args, 1);
			switch (cmd.ToUpperInvariant())
			{
				case "SVN_REV": SVN_REV(true,cmdArgs); break;
				case "GIT_REV": SVN_REV(false,cmdArgs); break;
				case "NXCOMPAT": NXCOMPAT(cmdArgs); break;
				case "LARGEADDRESS": LARGEADDRESS(cmdArgs); break;
				case "TIMESTAMP": TIMESTAMP(cmdArgs); break;
			}
		}

		static string[] Crop(string[] arr, int from)
		{
			return arr.Reverse().Take(arr.Length - from).Reverse().ToArray();
		}

		static string EscapeArgument(string s)
		{
			return "\"" + Regex.Replace(s, @"(\\+)$", @"$1$1") + "\"";
		}

		static string RunTool(string path, params string[] args)
		{
			string args_combined = "";
			foreach (var a in args)
				args_combined += EscapeArgument(a) + " ";
			args_combined.TrimEnd(' ');

			var psi = new System.Diagnostics.ProcessStartInfo();
			psi.Arguments = args_combined;
			psi.FileName = path;
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			psi.RedirectStandardOutput = true;
			var proc = new System.Diagnostics.Process();
			proc.StartInfo = psi;
			proc.Start();

			string output = "";
			while (!proc.StandardOutput.EndOfStream)
			{
				output += proc.StandardOutput.ReadLine();
				// do something with line
			}

			return output;
		}

		static void WriteTextIfChanged(string path, string content)
		{
			if (!File.Exists(path))
				goto WRITE;
			string old = File.ReadAllText(path);
			if (old == content) return;
		WRITE:
			File.WriteAllText(path, content);
		}

		static void LARGEADDRESS(string[] args)
		{
			string target = null, strValue = "0";
			int idx = 0;
			while (idx < args.Length)
			{
				string a = args[idx++];
				string au = a.ToUpperInvariant();
				if (au == "--TARGET")
					target = args[idx++];
				if (au == "--VALUE")
					strValue = args[idx++];
			}
			if (target == null)
			{
				Console.WriteLine("LARGEADDRESS: No target EXE specified");
				return;
			}

			//http://stackoverflow.com/questions/9054469/how-to-check-if-exe-is-set-as-largeaddressaware
			using (var fs = new FileStream(target, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
			{
				var br = new BinaryReader(fs);

				if (br.ReadInt16() != 0x5A4D)       //No MZ Header
					return;

				br.BaseStream.Position = 0x3C;
				var peloc = br.ReadInt32();         //Get the PE header location.

				br.BaseStream.Position = peloc;
				if (br.ReadInt32() != 0x4550)       //No PE header
					return;

				br.BaseStream.Position += 0x12;
				var characteristics = br.ReadUInt16();
				characteristics &= unchecked((ushort)~0x20); //IMAGE_FILE_LARGE_ADDRESS_AWARE
				if (strValue == "1") characteristics |= 0x20;
				fs.Position -= 2; //move back to characteristics
				var bw = new BinaryWriter(fs);
				bw.Write(characteristics);
				bw.Flush();
			}
		}

		//clears the timestamp in PE header (for deterministic builds)
		static void TIMESTAMP(string[] args)
		{
			using (var fs = new FileStream(args[0], FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
			{
				fs.Position = 0x88;
				fs.WriteByte(0); fs.WriteByte(0); fs.WriteByte(0); fs.WriteByte(0);
			}
		}

		//sets NXCOMPAT bit in PE header
		static void NXCOMPAT(string[] args)
		{
			string target = null, strValue = "0";
			int idx = 0;
			while (idx < args.Length)
			{
				string a = args[idx++];
				string au = a.ToUpperInvariant();
				if (au == "--TARGET")
					target = args[idx++];
				if (au == "--VALUE")
					strValue = args[idx++];
			}
			if (target == null)
			{
				Console.WriteLine("NXCOMPAT: No target EXE specified");
				return;
			}

			//we're going to skip around through the file and edit only the minimum required bytes (to speed things up by avoiding loading and rewriting the entire exe)
			using(var fs = new FileStream(target,FileMode.Open,FileAccess.ReadWrite,FileShare.Read))
			{
				var br = new BinaryReader(fs);
				fs.Position = 0x3C;
				fs.Position = br.ReadUInt16(); //move to NT_HEADERS
				fs.Position += 0x18; //move to OPTIONAL_HEADER
				fs.Position += 0x46; //move to DllCharacteristics
				var dllCharacteristics = br.ReadUInt16();
				dllCharacteristics &= unchecked((ushort)~0x100);
				if (strValue == "1") dllCharacteristics |= 0x100;
				fs.Position -= 2; //move back to DllCharacteristics
				var bw = new BinaryWriter(fs);
				bw.Write(dllCharacteristics);
				bw.Flush();
			}
		}

		//gets the working copy version. use this command:
		//BizHawk.Build.Tool.exe SCM_REV --wc c:\path\to\wcdir --template c:\path\to\templatefile --out c:\path\to\outputfile.cs
		//if the required tools aren't found 
		static void SVN_REV(bool svn, string[] args)
		{
			string wcdir = null, templatefile = null, outfile = null;
			int idx=0;
			while (idx < args.Length)
			{
				string a = args[idx++];
				string au = a.ToUpperInvariant();
				if(au == "--WC")
					wcdir = args[idx++];
				if(au == "--TEMPLATE")
					templatefile = args[idx++];
				if (au == "--OUT")
					outfile = args[idx++];
			}

			//first read the template
			string templateContents = File.ReadAllText(templatefile);

			//pick revision 0 in case the WC investigation fails
			int rev = 0;

			//pick branch unnamed in case investigation fails (or isnt git)
			string branch = "";

			//pick no hash in case investigation fails (or isnt git)
			string shorthash = "";

			//try to find an SVN or GIT and run it
			if (svn)
			{
				string svntool = FileLocator.LocateTool("svnversion");
				if (svntool != "")
				{
					try
					{
						string output = RunTool(svntool, wcdir);
						var parts = output.Split(':');
						var rstr = parts[parts.Length - 1];
						rstr = Regex.Replace(rstr, "[^0-9]", "");
						rev = int.Parse(rstr);
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex);
					}
				}
			}
			else
			{
				string gittool = FileLocator.LocateTool("git");
				if (gittool != "")
				{
					try
					{
						string output = RunTool(gittool, "-C", wcdir, "rev-list", "HEAD", "--count");
						if(int.TryParse(output, out rev))
						{
							output = RunTool(gittool, "-C", wcdir, "rev-parse", "--abbrev-ref", "HEAD");
							if(output.StartsWith("fatal")) {}
							else branch = output;

							output = RunTool(gittool, "-C", wcdir, "log", "-1", "--format=\"%h\"");
							if (output.StartsWith("fatal")) { }
							else shorthash = output;
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex);
					}
				}
			}

			//replace the template and dump the results if needed
			templateContents = templateContents.Replace("$WCREV$", rev.ToString());
			templateContents = templateContents.Replace("$WCBRANCH$", branch);
			templateContents = templateContents.Replace("$WCSHORTHASH$", shorthash);
			WriteTextIfChanged(outfile, templateContents);
		}
	}
}
