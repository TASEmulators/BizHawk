using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Generex
{
	public class Lister
	{
		List<string> lines = new List<string>();

		Dictionary<string, int> uops = new Dictionary<string, int>();
		int nextindex = 1;
		int nextmulti = 0;

		StringWriter output = new StringWriter();

		public Lister(TextReader src)
		{
			string s;
			while ((s = src.ReadLine()) != null)
				lines.Add(s);
		}

		int GetIndex(string uop)
		{
			uop = uop.Trim();
			int ret;
			if (uops.TryGetValue(uop, out ret))
				return ret;
			else
			{
				uops.Add(uop, nextindex++);
				return nextindex - 1;
			}
		}

		void ReadOpcode(ref int idx)
		{
			if (lines[idx] != "//##IMPL")
				throw new Exception("missing IMPL tag");
			idx++;
			output.WriteLine(lines[idx++]);
			output.WriteLine("//   " + lines[idx++]);
			output.WriteLine("{");
			while (lines[idx].Length > 1)
			{
				var s = lines[idx].Trim();

				if (s == "//[[") // special multiline hack
				{
					idx++;
					while ((s = lines[idx].Trim()) != "//]]")
					{
						output.WriteLine("  //{0}", s);
						idx++;
					}
					string f = string.Format("//!!MULTI{0}", nextmulti++);
					int j = GetIndex(f);
					output.WriteLine("  {0}, // {1}", j, f);
					idx++;
				}
				else
				{
					int j = GetIndex(s);
					output.WriteLine("  {0}, // {1}", j, s);

					idx++;
				}
			}
			output.WriteLine("  {0}, // //!!NEXT", GetIndex("//!!NEXT"));
			output.WriteLine("},");
			idx++;
		}

		public void Scan()
		{
			for (int idx = 0; idx < lines.Count; ReadOpcode(ref idx))
			{
			}
		}

		void PrintUops()
		{
			Console.WriteLine("{");
			Console.WriteLine("  switch (uop)");
			Console.WriteLine("  {");
			foreach (var kv in uops)
			{
				Console.WriteLine("    case {0}:", kv.Value);
				Console.WriteLine("      {0}", kv.Key);
				Console.WriteLine("      break;");
			}
			Console.WriteLine("  }");
			Console.WriteLine("}");

		}

		public void PrintStuff()
		{
			Console.WriteLine(output.ToString());

			for (int i = 0; i < 8; i++)
				Console.WriteLine();

			PrintUops();
		}

	}
}
