using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq.Expressions;
using System.IO;
using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Common
{
	public class BinaryQuickSerializer
	{
		// fields are serialized/deserialized in their memory order as reported by Marshal.OffsetOf
		// to do anything useful, passed targets should be [StructLayout.Sequential] or [StructLayout.Explicit]

		#region reflection infrastructure
		private static MethodInfo FromExpression(Expression e)
		{
			var caller = e as MethodCallExpression;
			if (caller == null)
				throw new ArgumentException("Expression must be a method call");
			return caller.Method;
		}

		private static MethodInfo Method(Expression<Action> f)
		{
			return FromExpression(f.Body);
		}
		private static MethodInfo Method<T>(Expression<Action<T>> f)
		{
			return FromExpression(f.Body);
		}

		private static Dictionary<Type, MethodInfo> readhandlers = new Dictionary<Type, MethodInfo>();
		private static Dictionary<Type, MethodInfo> writehandlers = new Dictionary<Type, MethodInfo>();

		private static void AddR<T>(Expression<Action<BinaryReader>> f)
		{
			var method = Method(f);
			if (!typeof(T).IsAssignableFrom(method.ReturnType))
				throw new InvalidOperationException("Type mismatch");
			readhandlers.Add(typeof(T), method);
		}
		private static void AddW<T>(Expression<Action<BinaryWriter>> f)
		{
			var method = Method(f);
			if (!method.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(T)))
				throw new InvalidOperationException("Type mismatch");
			writehandlers.Add(typeof(T), Method(f));
		}

		static BinaryQuickSerializer()
		{
			AddR<bool>(r => r.ReadBoolean());
			AddW<bool>(r => r.Write(false));

			AddR<sbyte>(r => r.ReadSByte());
			AddW<sbyte>(r => r.Write((sbyte)0));

			AddR<byte>(r => r.ReadByte());
			AddW<byte>(r => r.Write((byte)0));

			AddR<short>(r => r.ReadInt16());
			AddW<short>(r => r.Write((short)0));

			AddR<ushort>(r => r.ReadUInt16());
			AddW<ushort>(r => r.Write((ushort)0));

			AddR<int>(r => r.ReadInt32());
			AddW<int>(r => r.Write((int)0));

			AddR<uint>(r => r.ReadUInt32());
			AddW<uint>(r => r.Write((uint)0));

			AddR<long>(r => r.ReadInt64());
			AddW<long>(r => r.Write((long)0));

			AddR<ulong>(r => r.ReadUInt64());
			AddW<ulong>(r => r.Write((ulong)0));

		}

		private delegate void Reader(object target, BinaryReader r);
		private delegate void Writer(object target, BinaryWriter w);

		private class SerializationFactory
		{
			public Type Type;
			public Reader Read;
			public Writer Write;
		}

		private static SerializationFactory CreateSer(Type t)
		{
			var fields = DeepEquality.GetAllFields(t)
				//.OrderBy(fi => (int)fi.GetManagedOffset()) // [StructLayout.Sequential] doesn't work with this
				.OrderBy(fi => (int)Marshal.OffsetOf(t, fi.Name))
				.ToList();

			foreach (var field in DeepEquality.GetAllFields(t))
			{
				Console.WriteLine("{0} @{1}", field.Name, field.GetManagedOffset());
			}

			var rmeth = new DynamicMethod(t.Name + "_r", null, new[] { typeof(object), typeof(BinaryReader) }, true);
			var wmeth = new DynamicMethod(t.Name + "_w", null, new[] { typeof(object), typeof(BinaryWriter) }, true);

			{
				var il = rmeth.GetILGenerator();
				var target = il.DeclareLocal(t);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Castclass, t);
				il.Emit(OpCodes.Stloc, target);

				foreach (var field in fields)
				{
					il.Emit(OpCodes.Ldloc, target);
					il.Emit(OpCodes.Ldarg_1);
					MethodInfo m;
					if (!readhandlers.TryGetValue(field.FieldType, out m))
						throw new InvalidOperationException("(R) Can't handle nested type " + field.FieldType);
					il.Emit(OpCodes.Callvirt, m);
					il.Emit(OpCodes.Stfld, field);
					Console.WriteLine("(R) {0} {1}: {2}", field.Name, field.FieldType, m);
				}
				il.Emit(OpCodes.Ret);
			}
			{
				var il = wmeth.GetILGenerator();
				var target = il.DeclareLocal(t);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Castclass, t);
				il.Emit(OpCodes.Stloc, target);

				foreach (var field in fields)
				{
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Ldloc, target);
					il.Emit(OpCodes.Ldfld, field);
					MethodInfo m;
					if (!writehandlers.TryGetValue(field.FieldType, out m))
						throw new InvalidOperationException("(W) Can't handle nested type " + field.FieldType);
					il.Emit(OpCodes.Callvirt, m);
					Console.WriteLine("(W) {0} {1}: {2}", field.Name, field.FieldType, m);
				}
				il.Emit(OpCodes.Ret);
			}

			return new SerializationFactory
			{
				Type = t,
				Read = (Reader)rmeth.CreateDelegate(typeof(Reader)),
				Write = (Writer)wmeth.CreateDelegate(typeof(Writer))
			};
		}
		#endregion

		private static IDictionary<Type, SerializationFactory> sers =
			new ConcurrentDictionary<Type, SerializationFactory>();

		private static SerializationFactory GetFactory(Type t)
		{
			SerializationFactory f;
			if (!sers.TryGetValue(t, out f))
			{
				f = CreateSer(t);
				sers[t] = f;
			}
			return f;
		}

		public static void Read(object target, BinaryReader r)
		{
			GetFactory(target.GetType()).Read(target, r);
		}

		public static void Write(object target, BinaryWriter w)
		{
			GetFactory(target.GetType()).Write(target, w);
		}
	}
}
