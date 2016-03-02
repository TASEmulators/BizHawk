using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	internal static class SaveState
	{
	    public class DoNotSave : Attribute
	    {
	    }

	    public class SaveWithName : Attribute
	    {
            public string Name { get; set; }

	        public SaveWithName(string name)
	        {
	            Name = name;
	        }
	    }

		private static readonly Encoding Encoding = Encoding.Unicode;

        private static int[] GetDelta(IList<int> source, IList<int> data)
        {
            var length = Math.Min(source.Count, data.Count);
            var delta = new int[length];
            for (var i = 0; i < length; i++)
            {
                delta[i] = source[i] ^ data[i];
            }
            return delta;
        }

	    private static byte[] CompressInts(int[] data)
	    {
	        unchecked
	        {
                var length = data.Length;
                var bytes = new byte[length * 4];
	            for (int i = 0, j = 0; i < length; i++)
	            {
	                var c = data[i];
	                bytes[j++] = (byte)(c);
                    bytes[j++] = (byte)(c >> 8);
                    bytes[j++] = (byte)(c >> 16);
                    bytes[j++] = (byte)(c >> 24);
                }
                using (var mem = new MemoryStream())
                {
                    using (var compressor = new DeflateStream(mem, CompressionMode.Compress))
                    {
                        var writer = new BinaryWriter(compressor);
                        writer.Write(bytes.Length);
                        writer.Write(bytes);
                        compressor.Flush();
                    }
                    mem.Flush();
                    return mem.ToArray();
                }
            }
        }

	    private static int[] DecompressInts(byte[] data)
	    {
            unchecked
            {
                using (var mem = new MemoryStream(data))
                {
                    using (var decompressor = new DeflateStream(mem, CompressionMode.Decompress))
                    {
                        var reader = new BinaryReader(decompressor);
                        var length = reader.ReadInt32();
                        var bytes = reader.ReadBytes(length);
                        var result = new int[length >> 2];
                        for (int i = 0, j = 0; i < length; i++)
                        {
                            int d = bytes[i++];
                            d |= bytes[i++] << 8;
                            d |= bytes[i++] << 16;
                            d |= bytes[i] << 24;
                            result[j++] = d;
                        }
                        return result;
                    }
                }
            }
	    }

        public static void SyncDelta(string name, Serializer ser, int[] source, ref int[] data)
        {
            int[] delta = null;
            if (ser.IsWriter && data != null)
            {
                delta = GetDelta(source, data);
            }
            ser.Sync(name, ref delta, false);
            if (ser.IsReader && delta != null)
            {
                data = GetDelta(source, delta);
            }
        }

        public static void SyncObject(Serializer ser, object obj)
		{
			const BindingFlags defaultFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
		    var objType = obj.GetType();
            var members = objType.GetMembers(defaultFlags);

		    foreach (var member in members)
			{
                if (member.GetCustomAttributes(true).Any(a => a is DoNotSave))
                {
                    continue;
                }

			    var name = member.Name;
			    var nameAttribute = member.GetCustomAttributes(true).FirstOrDefault(a => a is SaveWithName);
			    if (nameAttribute != null)
			    {
			        name = ((SaveWithName) nameAttribute).Name;
			    }


                object currentValue = null;
				var fail = false;
				var fieldInfo = member as FieldInfo;
			    Type valueType = null;

                if ((member.MemberType == MemberTypes.Field) && member.ReflectedType != null)
				{
                    valueType = fieldInfo.FieldType;
                    currentValue = fieldInfo.GetValue(obj);
                }

                if (currentValue != null)
			    {
			        ByteBuffer refByteBuffer;
			        int refInt32;
			        IntBuffer refIntBuffer;
			        int refPointX;
			        int refPointY;
			        switch (valueType.Name)
			        {
                        case "Action`1":
                        case "Action`2":
                            break;
                        case "Bit":
			                var refBit = (Bit)currentValue;
			                ser.Sync(name, ref refBit);
			                currentValue = refBit;
			                break;
			            case "Boolean":
			                var refBool = (bool)currentValue;
			                ser.Sync(name, ref refBool);
			                currentValue = refBool;
			                break;
			            case "Boolean[]":
			            {
			                var tmp = (bool[])currentValue;
			                ser.Sync(name, ref tmp, false);
			                currentValue = tmp;
			            }
			                break;
			            case "Byte":
			                var refByte = (byte)currentValue;
			                ser.Sync(name, ref refByte);
			                currentValue = refByte;
			                break;
			            case "Byte[]":
			                refByteBuffer = new ByteBuffer((byte[])currentValue);
			                ser.Sync(name, ref refByteBuffer);
			                currentValue = refByteBuffer.Arr.Select(d => d).ToArray();
                            refByteBuffer.Dispose();
			                break;
			            case "ByteBuffer":
			                refByteBuffer = (ByteBuffer)currentValue;
			                ser.Sync(name, ref refByteBuffer);
			                currentValue = refByteBuffer;
			                break;
			            case "Func`1":
                        case "Func`2":
                            break;
			            case "Int16":
			                var refInt16 = (short)currentValue;
			                ser.Sync(name, ref refInt16);
			                currentValue = refInt16;
			                break;
			            case "Int32":
			                refInt32 = (int)currentValue;
			                ser.Sync(name, ref refInt32);
			                currentValue = refInt32;
			                break;
			            case "Int32[]":
			                refIntBuffer = new IntBuffer((int[])currentValue);
			                ser.Sync(name, ref refIntBuffer);
			                currentValue = refIntBuffer.Arr.Select(d => d).ToArray();
                            refIntBuffer.Dispose();
			                break;
			            case "IntBuffer":
			                refIntBuffer = (IntBuffer)currentValue;
			                ser.Sync(name, ref refIntBuffer);
			                currentValue = refIntBuffer;
			                break;
			            case "Point":
			                refPointX = ((Point)currentValue).X;
			                refPointY = ((Point)currentValue).Y;
			                ser.Sync(name + "_X", ref refPointX);
			                ser.Sync(name + "_Y", ref refPointY);
			                currentValue = new Point(refPointX, refPointY);
			                break;
			            case "Rectangle":
			                refPointX = ((Rectangle)currentValue).X;
			                refPointY = ((Rectangle)currentValue).Y;
			                var refRectWidth = ((Rectangle)currentValue).Width;
			                var refRectHeight = ((Rectangle)currentValue).Height;
			                ser.Sync(name + "_X", ref refPointX);
			                ser.Sync(name + "_Y", ref refPointY);
			                ser.Sync(name + "_Height", ref refRectHeight);
			                ser.Sync(name + "_Width", ref refRectWidth);
			                currentValue = new Rectangle(refPointX, refPointY, refRectWidth, refRectHeight);
			                break;
			            case "SByte":
			                var refSByte = (sbyte)currentValue;
			                ser.Sync(name, ref refSByte);
			                currentValue = refSByte;
			                break;
			            case "String":
			                var refString = (string)currentValue;
			                var refVal = new ByteBuffer(Encoding.GetBytes(refString));
			                ser.Sync(name, ref refVal);
			                currentValue = Encoding.GetString(refVal.Arr);
                            refVal.Dispose();
			                break;
			            case "UInt16":
			                var refUInt16 = (ushort)currentValue;
			                ser.Sync(name, ref refUInt16);
			                currentValue = refUInt16;
			                break;
			            case "UInt32":
			                var refUInt32 = (uint)currentValue;
			                ser.Sync(name, ref refUInt32);
			                currentValue = refUInt32;
			                break;
			            default:
			                var t = currentValue.GetType();
			                if (t.IsEnum)
			                {
			                    refInt32 = (int)currentValue;
			                    ser.Sync(name, ref refInt32);
			                    currentValue = refInt32;
			                }
                            else if (t.IsArray)
                            {
                                var currentValueArray = (Array) currentValue;
                                for (var i = 0; i < currentValueArray.Length; i++)
                                {
                                    ser.BeginSection(string.Format("{0}_{1}", name, i));
                                    SyncObject(ser, currentValueArray.GetValue(i));
                                    ser.EndSection();
                                }
                            }
                            else if (t.IsValueType)
			                {
			                    fail = true;
			                }
			                else if (t.IsClass)
			                {
			                    fail = true;
			                    foreach (var method in t.GetMethods().Where(method => method.Name == "SyncState"))
			                    {
			                        ser.BeginSection(fieldInfo.Name);
			                        method.Invoke(currentValue, new object[] { ser });
			                        ser.EndSection();
			                        fail = false;
			                        break;
			                    }
			                }
			                else
			                {
			                    fail = true;
			                }
			                break;
			        }
			    }

			    if (!fail)
			    {
                    if (member.MemberType == MemberTypes.Property)
                    {
                        var propInfo = member as PropertyInfo;
                        if (propInfo.CanWrite)
                        {
                            var setMethod = propInfo.GetSetMethod();
                            if (setMethod != null)
                            {
                                setMethod.Invoke(obj, new[] { currentValue });
                            }
                        }
                    }
                    else if (member.MemberType == MemberTypes.Field)
                    {
                        fieldInfo.SetValue(obj, currentValue);
                    }
                }
            }
		}
	}
}
