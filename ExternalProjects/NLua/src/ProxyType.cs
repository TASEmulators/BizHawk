using System;
using System.Reflection;

namespace NLua
{
	public class ProxyType
	{
		private readonly Type _proxy;

		public ProxyType(Type proxy)
		{
			_proxy = proxy;
		}

		/// <summary>
		/// Provide human readable short hand for this proxy object
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "ProxyType(" + UnderlyingSystemType + ")";
		}

		public Type UnderlyingSystemType => _proxy;

		public override bool Equals(object obj)
		{
			if (obj is Type)
				return _proxy == (Type)obj;
			if (obj is ProxyType)
				return _proxy == ((ProxyType)obj).UnderlyingSystemType;
			return _proxy.Equals(obj);
		}

		public override int GetHashCode()
		{
			return _proxy.GetHashCode();
		}

		public MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
		{
			return _proxy.GetMember(name, bindingAttr);
		}

		public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Type[] signature)
		{
			return _proxy.GetMethod(name, bindingAttr, null, signature, null);
		}
	}
}