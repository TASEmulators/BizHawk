using System;
using System.Reflection;

namespace NLua
{
	internal class ProxyType
	{
		public ProxyType(Type proxy)
		{
			UnderlyingSystemType = proxy;
		}

		/// <summary>
		/// Provide human readable short hand for this proxy object
		/// </summary>
		/// <returns></returns>
		public override string ToString()
			=> "ProxyType(" + UnderlyingSystemType + ")";

		public Type UnderlyingSystemType { get; }

		public override bool Equals(object obj) => obj switch
		{
			Type type => UnderlyingSystemType == type,
			ProxyType type => UnderlyingSystemType == type.UnderlyingSystemType,
			_ => UnderlyingSystemType.Equals(obj)
		};

		public override int GetHashCode()
			=> UnderlyingSystemType.GetHashCode();

		public MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
			=> UnderlyingSystemType.GetMember(name, bindingAttr);

		public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Type[] signature)
			=> UnderlyingSystemType.GetMethod(name, bindingAttr, null, signature, null);
	}
}