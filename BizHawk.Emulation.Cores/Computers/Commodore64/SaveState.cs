using System;
using System.Drawing;
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

		private static readonly Encoding Encoding = Encoding.Unicode;

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
			                ser.Sync(member.Name, ref refBit);
			                currentValue = refBit;
			                break;
			            case "Boolean":
			                var refBool = (bool)currentValue;
			                ser.Sync(member.Name, ref refBool);
			                currentValue = refBool;
			                break;
			            case "Boolean[]":
			            {
			                var tmp = (bool[])currentValue;
			                ser.Sync(member.Name, ref tmp, false);
			                currentValue = tmp;
			            }
			                break;
			            case "Byte":
			                var refByte = (byte)currentValue;
			                ser.Sync(member.Name, ref refByte);
			                currentValue = refByte;
			                break;
			            case "Byte[]":
			                refByteBuffer = new ByteBuffer((byte[])currentValue);
			                ser.Sync(member.Name, ref refByteBuffer);
			                currentValue = refByteBuffer.Arr;
			                break;
			            case "ByteBuffer":
			                refByteBuffer = (ByteBuffer)currentValue;
			                ser.Sync(member.Name, ref refByteBuffer);
			                currentValue = refByteBuffer;
			                break;
			            case "Func`1":
                        case "Func`2":
                            break;
			            case "Int16":
			                var refInt16 = (short)currentValue;
			                ser.Sync(member.Name, ref refInt16);
			                currentValue = refInt16;
			                break;
			            case "Int32":
			                refInt32 = (int)currentValue;
			                ser.Sync(member.Name, ref refInt32);
			                currentValue = refInt32;
			                break;
			            case "Int32[]":
			                refIntBuffer = new IntBuffer((int[])currentValue);
			                ser.Sync(member.Name, ref refIntBuffer);
			                currentValue = refIntBuffer.Arr;
			                break;
			            case "IntBuffer":
			                refIntBuffer = (IntBuffer)currentValue;
			                ser.Sync(member.Name, ref refIntBuffer);
			                currentValue = refIntBuffer;
			                break;
			            case "Point":
			                refPointX = ((Point)currentValue).X;
			                refPointY = ((Point)currentValue).Y;
			                ser.Sync(member.Name + "_X", ref refPointX);
			                ser.Sync(member.Name + "_Y", ref refPointY);
			                currentValue = new Point(refPointX, refPointY);
			                break;
			            case "Rectangle":
			                refPointX = ((Rectangle)currentValue).X;
			                refPointY = ((Rectangle)currentValue).Y;
			                var refRectWidth = ((Rectangle)currentValue).Width;
			                var refRectHeight = ((Rectangle)currentValue).Height;
			                ser.Sync(member.Name + "_X", ref refPointX);
			                ser.Sync(member.Name + "_Y", ref refPointY);
			                ser.Sync(member.Name + "_Height", ref refRectHeight);
			                ser.Sync(member.Name + "_Width", ref refRectWidth);
			                currentValue = new Rectangle(refPointX, refPointY, refRectWidth, refRectHeight);
			                break;
			            case "SByte":
			                var refSByte = (sbyte)currentValue;
			                ser.Sync(member.Name, ref refSByte);
			                currentValue = refSByte;
			                break;
			            case "String":
			                var refString = (string)currentValue;
			                var refVal = new ByteBuffer(Encoding.GetBytes(refString));
			                ser.Sync(member.Name, ref refVal);
			                currentValue = Encoding.GetString(refVal.Arr);
			                break;
			            case "UInt16":
			                var refUInt16 = (ushort)currentValue;
			                ser.Sync(member.Name, ref refUInt16);
			                currentValue = refUInt16;
			                break;
			            case "UInt32":
			                var refUInt32 = (uint)currentValue;
			                ser.Sync(member.Name, ref refUInt32);
			                currentValue = refUInt32;
			                break;
			            default:
			                var t = currentValue.GetType();
			                if (t.IsEnum)
			                {
			                    refInt32 = (int)currentValue;
			                    ser.Sync(member.Name, ref refInt32);
			                    currentValue = refInt32;
			                }
                            else if (t.IsArray)
                            {
                                var currentValueArray = (Array) currentValue;
                                for (var i = 0; i < currentValueArray.Length; i++)
                                {
                                    ser.BeginSection(string.Format("{0}_{1}", member.Name, i));
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
