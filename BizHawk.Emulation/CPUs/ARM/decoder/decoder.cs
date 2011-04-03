using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class BS
{
	public BS(string str)
	{
		ca = str.ToCharArray();
		Array.Reverse(ca);
	}
	char[] ca;
	public static bool operator ==(uint lhs, BS rhs) { return rhs == lhs; }
	public static bool operator !=(uint lhs, BS rhs) { return rhs != lhs; }
	public static bool operator ==(int lhs, BS rhs) { return rhs == (uint)lhs; }
	public static bool operator !=(int lhs, BS rhs) { return rhs != (uint)lhs; }
	public static bool operator ==(BS lhs, int rhs) { return (uint)rhs == lhs; }
	public static bool operator !=(BS lhs, int rhs) { return (uint)rhs != lhs; }
	public static bool operator ==(BS lhs, uint rhs)
	{
		foreach (char c in lhs.ca)
		{
			uint bit = rhs & 1;
			switch (c)
			{
				case '0': if (bit != 0) return false; break;
				case '1': if (bit != 1) return false; break;
			}
			rhs >>= 1;
		}
		if(rhs != 0) return false;
		return true;
	}
	public static bool operator !=(BS lhs, uint rhs) { return !(lhs == rhs); }
	public override bool Equals(object obj)
	{
		if (obj is uint) return this == (uint)obj;
		if (obj is int) return this == (int)obj;
		if (obj is BS) throw new InvalidOperationException();
		return false;
	}
	public override int GetHashCode()
	{
		return ca.GetHashCode();
	}
}

class Decoder
{
	bool constructed = false;
	public void Ensure(Action constructor)
	{
		if (constructed) return;
		constructed = true;
		constructor();
		Compile();
	}

	//public Decoder(params object[] args)
	//{
	//    if(args.Length %2 != 0) throw new ArgumentException("need odd params to Decoder");
		
	//    for(int i=0;i<args.Length/2;i++)
	//    {
	//        string str = (string)args[i * 2];
	//        object o = args[i * 2 + 1];
	//        if (o is int)
	//            Declare(str, (int)o);
	//        else AddRule(str, (Action)o);
	//    }
	//}

	class Rule
	{
		public Rule(string program,Action action)
		{
			this.program = program;
			this.action = action;
		}
		public string program;
		public Action action;
	}
	public void AddRule(string rule, Action action)
	{
		rules.Add(new Rule(rule,action));
	}
	public Decoder r(string rule, Action action)
	{
		AddRule(rule, action);
		return this;
	}

	List<Rule> rules = new List<Rule>();

	class Variable
	{
		public Variable(string name, int bits)
		{
			this.name = name;
			this.bits = bits;
		}
		public string name;
		public int bits;
	}
	List<Variable> variables = new List<Variable>();

	public void Declare(string name, int bits)
	{
		variables.Add(new Variable(name, bits));
	}
	public Decoder d(string name, int bits)
	{
		Declare(name, bits);
		return this;
	}

	static Regex rxBits = new Regex(@"(\#([01xX]*))", RegexOptions.Compiled);
	bool compiled = false;

	public void Compile()
	{
		if (compiled) return;
		compiled = true;
		int numbits = 0;
		ScriptEngine se = new ScriptEngine();
		foreach (Variable v in variables) numbits += v.bits;
		int cases = 1 << numbits;
		for (uint i = 0; i < cases; i++)
		{
			//split apart key
			uint itemp = i;
			for (int j = 0; j < variables.Count; j++)
			{
				Variable v = variables[j];
				uint x = (uint)(itemp & ((1 << v.bits)-1));
				itemp >>= v.bits;
				se.variables[v.name] = x;
			}

			//default table value is an exception
			table[i] = () => { throw new InvalidOperationException("auto-decoder fail (missing case)"); };

			foreach (Rule rule in rules)
			{
				string program = rule.program;
				foreach (Match m in rxBits.Matches(program))
					program = program.Replace(m.Value, "new BS(\"" + m.Groups[2].Value + "\")");
				object o = se.Eval(program);
				bool? b = o as bool?;
				if(!b.HasValue) throw new InvalidOperationException();
				if (b.Value)
				{
					table[i] = rule.action;
				}
			}
		}

		se.Dispose();
	}

	//TODO - make overloads for different param number (array create overhead will be high)
	public void Evaluate(params uint[] args)
	{
		Compile();
		uint key = 0;
		for (int i = variables.Count-1; i >= 0; i--)
		{
			Variable v = variables[i];
			key <<= v.bits;
			if (args[i] > (1 << v.bits))
				throw new ArgumentException();
			key |= args[i];
		}
		table[key]();
	}

	Dictionary<uint, Action> table = new Dictionary<uint, Action>();
}