using System;
using System.IO;
using System.Security.Cryptography;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{

	public class N64JaboManager
	{
		string dllDir, rawPath, patchedPath;

		public N64JaboManager()
		{
			//THIS IS HORRIBLE! PATH MUST BE PASSED IN SOME OTHER WAY
			dllDir = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "dll");
			rawPath = Path.Combine(dllDir, "Jabo_Direct3D8.dll");
			patchedPath = Path.Combine(dllDir, "Jabo_Direct3D8_patched.dll");
		}

		public JaboStatus Status { get; private set; }

		public enum JaboStatus
		{
			NotReady,
			ReadyToPatch,
			Ready,
			WrongVersion21,
			WrongVersion16
		};

		public void Scan()
		{
			//check whether the file is patched and intact
			if (File.Exists(patchedPath))
			{
				using (var md5 = MD5.Create())
				{
					byte[] hash = md5.ComputeHash(File.ReadAllBytes(patchedPath));
					string hash_string = BitConverter.ToString(hash).Replace("-", "");
					if (hash_string == "F4D6E624489CD88C68A5850426D4D70E")
					{
						Status = JaboStatus.Ready;
					}
				}
			}
			else if (File.Exists(rawPath))
			{
				using (var md5 = MD5.Create())
				{
					byte[] hash = md5.ComputeHash(File.ReadAllBytes(rawPath));
					string hash_string = BitConverter.ToString(hash).Replace("-", "");
					if (hash_string == "4F353AA71E7455B81205D8EC0AA339E1")
					{
						// jabo will be patched when a rom is loaded. user is ready to go
						Status = JaboStatus.ReadyToPatch;
					}
					if (hash_string == "4A4173928ED33735157A8D8CD14D4C9C")
					{
						// wrong jabo installed (2.0)
						Status = JaboStatus.WrongVersion21;
					}
					else if (hash_string == "FF57F60C58EDE6364B980EDCB311873B")
					{
						// wrong jabo installed (1.6)
						Status = JaboStatus.WrongVersion16;
					}
				}
			}
		}

		public void Patch()
		{
			byte[] jaboDLL = File.ReadAllBytes(rawPath);

			//this converts PE sections to have some different security flags (read+write instead of just read, maybe? I can't remember the details)
			//without it, NX protection would trip. Why are these flags set oddly? The dll packer, I think.
			jaboDLL[583] = 0xA0;
			jaboDLL[623] = 0xA0;
			jaboDLL[663] = 0xA0;
			jaboDLL[703] = 0xA0;
			jaboDLL[743] = 0xA0;
			jaboDLL[783] = 0xA0;
			jaboDLL[823] = 0xA0;
			jaboDLL[863] = 0xA0;
			File.WriteAllBytes(patchedPath, jaboDLL);

			//re-scan, in case the file didnt get written for some weird reason
			Scan();
		}

	}


}