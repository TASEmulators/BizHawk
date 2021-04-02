using System;
using System.Collections.Generic;
using System.Reflection;

using BizHawk.Client.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json.Linq;

namespace BizHawk.Tests.Client.Common.config
{
	[TestClass]
	public sealed class SerializationStabilityTests
	{
		private static readonly IReadOnlySet<Type> KnownGoodFromStdlib = new HashSet<Type>
		{
			typeof(bool),
			typeof(DateTime),
			typeof(Dictionary<,>),
			typeof(int),
			typeof(JToken),
			typeof(Nullable<>),
			typeof(object),
			typeof(float),
			typeof(string),
		};

		private static readonly IReadOnlyDictionary<Type, string> KnownGoodFromBizHawk = new Dictionary<Type, string>
		{
			[typeof(AnalogBind)] = "TODO",
			[typeof(BindingCollection)] = "TODO",
			[typeof(CheatConfig)] = "TODO",
			[typeof(FeedbackBind)] = "TODO",
			[typeof(MessagePosition)] = "TODO",
			[typeof(MovieConfig)] = "TODO",
			[typeof(PathEntryCollection)] = "TODO",
			[typeof(RecentFiles)] = "TODO",
			[typeof(RewindConfig)] = "TODO",
			[typeof(SaveStateConfig)] = "TODO",
			[typeof(ToolDialogSettings)] = "TODO",
			[typeof(ZoomFactors)] = "TODO",
		};

		[TestMethod]
		public void AssertAllTypesKnownSerializable()
		{
			static void CheckMemberAndTypeParams(Type t, string groupDesc)
			{
				if (t.IsEnum) return;
				if (t.IsConstructedGenericType)
				{
					CheckMemberAndTypeParams(t.GetGenericTypeDefinition(), groupDesc);
					foreach (var typeParam in t.GenericTypeArguments) CheckMemberAndTypeParams(typeParam, groupDesc);
					return;
				}
				Assert.IsTrue(KnownGoodFromStdlib.Contains(t) || KnownGoodFromBizHawk.ContainsKey(t), $"type {t.FullName}, present in {groupDesc}, may not be serializable");
			}
			static void CheckAll<T>(string? groupDesc = null)
			{
				var t = typeof(T);
				groupDesc ??= t.Name;
				foreach (var mi in t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					if (mi is PropertyInfo pi) CheckMemberAndTypeParams(pi.PropertyType, groupDesc);
					else if (mi is FieldInfo fi) CheckMemberAndTypeParams(fi.FieldType, groupDesc);
				}
			}
			CheckAll<Config>();
		}

#if false
		[TestMethod]
		public void TestRoundTripSerialization()
		{
			foreach (var kvp in KnownGoodFromBizHawk)
			{
				//TODO deserialize kvp.Value as an instance of the type kvp.Key, then reserialize it and compare that to kvp.Value
				// should probably clean up the types in question first
			}
		}
#endif
	}
}
