using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Generex
{
	class Program
	{
		// contains case list
		const string corecpp = @"..\..\..\..\bsnes\snes\smp\core\core.cpp";

		const string opcodescpp = @"..\..\..\..\bsnes\snes\smp\core\opcodes.cpp";

		const string fixedcpp = @".\fixed.cpp";

		static void Main(string[] args)
		{
			try
			{
				// GENEREX PHASE 1
				/*
				TextReader core = new StreamReader(corecpp);
				TextReader ops = new StreamReader(opcodescpp);

				Decoder d = new Decoder(core, ops);

				d.Scan();
				
				foreach (var s in d.impls)
				{
					Console.WriteLine("//##IMPL");
					foreach (var ss in s)
						Console.WriteLine(ss);
				}
				*/
				// GENERX PHASE 2

				TextReader fixedt = new StreamReader(fixedcpp);
				Lister l = new Lister(fixedt);
				l.Scan();
				l.PrintStuff();
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("EXCEPTION KILLED ME");
				Console.Error.WriteLine(e.ToString());

			}
		}
	}
}
