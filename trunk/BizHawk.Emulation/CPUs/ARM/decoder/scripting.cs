using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.JScript;
using Microsoft.JScript.Vsa;
using Microsoft.Vsa;

#pragma warning disable 0618

class ScriptEngine : IDisposable
{
	string[] commonNamespaces =
		{
		"System"
		};

	string[] commonAssemblies =
		{
		"mscorlib.dll",
		"System.dll",
		System.Reflection.Assembly.GetExecutingAssembly().Location
		};


	VsaEngine engine;
	//Microsoft.JScript.GlobalScope jscriptGlobalScope = null;

	public Dictionary<string, object> variables = new Dictionary<string, object>();

	class MySite : Microsoft.Vsa.BaseVsaSite
	{
		ScriptEngine se;
		public MySite(ScriptEngine se)
		{
			this.se = se;
		}
		public override object GetGlobalInstance(string name)
		{
			return se.variables[name];
		}
	}

	public ScriptEngine()
	{
		InitializeJScriptDotNetEngine();
	}

	public void Dispose()
	{
		UninitializeJScriptDotNetEngine();
	}

	bool hasVariables = false;
	public object Eval(string script)
	{
		if (!hasVariables)
		{
			hasVariables = true;
			foreach (KeyValuePair<string, object> kvp in variables)
			{
				IVsaGlobalItem globItem = (IVsaGlobalItem)engine.Items.CreateItem(kvp.Key, VsaItemType.AppGlobal, VsaItemFlag.None);
				globItem.TypeString = kvp.Value.GetType().FullName;
			}
			engine.CompileEmpty();
			engine.RunEmpty();
		}

		StringBuilder sb = new StringBuilder();
		foreach (KeyValuePair<string, object> kvp in variables)
			sb.AppendFormat("{0} = {1};\n",kvp.Key,kvp.Value);
		sb.AppendLine(script);
		
		object o = Microsoft.JScript.Eval.JScriptEvaluate(sb.ToString(), engine);

		return o;
	}

	private void UninitializeJScriptDotNetEngine()
	{

		//if (jscriptGlobalScope != null)

		//    jscriptGlobalScope.engine.Close();

		//jscriptGlobalScope = null;
		if(engine != null)
			engine.Close();
		engine = null;
	}

	private void InitializeJScriptDotNetEngine()
	{

		if (engine == null)
		{

			try
			{
				engine = new VsaEngine();
				engine.InitVsaEngine("blah://x",new MySite(this));
				foreach (string str in commonAssemblies)
				{
					IVsaReferenceItem reference = (IVsaReferenceItem)engine.Items.CreateItem(str, VsaItemType.Reference, VsaItemFlag.None);
					reference.AssemblyName = str;
				}


			}

			catch { }

			foreach (string ns in commonNamespaces)
			{

				try
				{

					if ((ns != null) && (ns != String.Empty))

						Microsoft.JScript.Import.JScriptImport(ns, engine);

				}

				catch { }

			}

		}

	}
}


