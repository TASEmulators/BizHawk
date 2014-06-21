using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Generex
{
	public class Decoder
	{
		TextReader core;

		public List<List<string>> impls = new List<List<string>>();

		List<string> ops = new List<string>();

		public Decoder(TextReader core, TextReader ops)
		{
			this.core = core;
			string s;
			while ((s = ops.ReadLine()) != null)
			{
				this.ops.Add(s);
			}
		}

		void ProcMethod(string openline, int opsindex, string submeth)
		{
			List<string> impl = new List<string>();
			impl.Add("// " + openline);

			string replacant = null;
			if (submeth != null)
			{
				var r = new Regex(@">\(([^\)]*)");
				var m = r.Match(openline);
				if (m.Success)
				{
					replacant = m.Groups[1].Value;
				}
				else
				{
					throw new Exception(string.Format("no find in \"{0}\"", openline));
				}
			}

			for (int i = opsindex; i < ops.Count; i++)
			{
				if (string.IsNullOrWhiteSpace(ops[i]))
					break;
				if (submeth != null && ops[i].Contains("call"))
				{
					var s = ops[i].Replace("call", submeth);
					// also have to replace 'r' tokens
					//Console.WriteLine("\"{0}\"{1}\"", s, replacant);
					impl.Add(s);
				}
				else
					impl.Add(ops[i]);
			}
			impls.Add(impl);
		}

		public void Scan()
		{
			string s;
			while ((s = core.ReadLine()) != null)
			{
				var r = new Regex(@": return ([^<\(]*)");

				var m = r.Match(s);
				
				if (!m.Success)
					continue;

				var methname = m.Groups[1].Value;

				r = new Regex(@"<&SMPcore::([^>]*)");

				m = r.Match(s);

				string submeth = m.Success ? m.Groups[1].Value : null;

				// find method

				string findu = "void SMPcore::" + methname;

				int i;
				for (i = 0; i < ops.Count; i++)
				{
					if (ops[i].Contains(findu))
					{
						ProcMethod(s, i, submeth);
						break;
					}

				}
				if (i == ops.Count)
					throw new Exception(string.Format("Couldn't find! \"{0}\"", findu));

			}

		}


	}
}
