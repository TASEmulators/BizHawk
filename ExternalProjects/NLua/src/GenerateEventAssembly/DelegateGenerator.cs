using System;

using NLua.Native;

namespace NLua.GenerateEventAssembly
{
	internal class DelegateGenerator
	{
		private readonly ObjectTranslator _translator;
		private readonly Type _delegateType;

		public DelegateGenerator(ObjectTranslator objectTranslator, Type type)
		{
			_translator = objectTranslator;
			_delegateType = type;
		}

		public object ExtractGenerated(LuaState luaState, int stackPos)
			=> CodeGeneration.Instance.GetDelegate(_delegateType, _translator.GetFunction(luaState, stackPos));
	}
}
