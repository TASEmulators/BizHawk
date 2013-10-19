using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
    static class SaveState
    {
        static public void SyncObject(Serializer ser, object obj)
        {
            BindingFlags defaultFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            MemberInfo[] members = obj.GetType().GetMembers(defaultFlags);

            Bit refBit;
            Boolean refBool;
            Byte refByte;
            ByteBuffer refByteBuffer;
            Int16 refInt16;
            Int32 refInt32;
            IntBuffer refIntBuffer;
            Int32 refPointX;
            Int32 refPointY;
            SByte refSByte;
            UInt16 refUInt16;
            UInt32 refUInt32;
            Int32 refRectHeight;
            Int32 refRectWidth;

            foreach (MemberInfo member in members)
            {
                object currentValue = null;
                bool fail = false;
                FieldInfo fieldInfo = null;
                PropertyInfo propInfo = null;
                Type valueType = null;

                if (member.MemberType == MemberTypes.Field)
                {
                    fieldInfo = member.ReflectedType.GetField(member.Name, defaultFlags);
                    valueType = fieldInfo.FieldType;
                    currentValue = fieldInfo.GetValue(obj);
                }
                else
                {
                    fail = true;
                }

                if (!fail)
                {
                    if (valueType.IsArray)
                    {
                    }

                    if (currentValue != null)
                    {
                        switch (valueType.Name)
                        {
                            case "Bit":
                                refBit = (Bit)currentValue;
                                ser.Sync(member.Name, ref refBit);
                                currentValue = refBit;
                                break;
                            case "Boolean":
                                refBool = (Boolean)currentValue;
                                ser.Sync(member.Name, ref refBool);
                                currentValue = refBool;
                                break;
                            case "Byte":
                                refByte = (Byte)currentValue;
                                ser.Sync(member.Name, ref refByte);
                                currentValue = refByte;
                                break;
                            case "Byte[]":
                                refByteBuffer = new ByteBuffer((byte[])currentValue);
                                ser.Sync(member.Name, ref refByteBuffer);
                                currentValue = refByteBuffer.arr;
                                break;
                            case "ByteBuffer":
                                refByteBuffer = (ByteBuffer)currentValue;
                                ser.Sync(member.Name, ref refByteBuffer);
                                currentValue = refByteBuffer;
                                break;
                            case "Int16":
                                refInt16 = (Int16)currentValue;
                                ser.Sync(member.Name, ref refInt16);
                                currentValue = refInt16;
                                break;
                            case "Int32":
                                refInt32 = (Int32)currentValue;
                                ser.Sync(member.Name, ref refInt32);
                                currentValue = refInt32;
                                break;
                            case "Int32[]":
                                refIntBuffer = new IntBuffer((int[])currentValue);
                                ser.Sync(member.Name, ref refIntBuffer);
                                currentValue = refIntBuffer.arr;
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
                                refRectWidth = ((Rectangle)currentValue).Width;
                                refRectHeight = ((Rectangle)currentValue).Height;
                                ser.Sync(member.Name + "_X", ref refPointX);
                                ser.Sync(member.Name + "_Y", ref refPointY);
                                ser.Sync(member.Name + "_Height", ref refRectHeight);
                                ser.Sync(member.Name + "_Width", ref refRectWidth);
                                currentValue = new Rectangle(refPointX, refPointY, refRectWidth, refRectHeight);
                                break;
                            case "SByte":
                                refSByte = (SByte)currentValue;
                                ser.Sync(member.Name, ref refSByte);
                                currentValue = refSByte;
                                break;
                            case "UInt16":
                                refUInt16 = (UInt16)currentValue;
                                ser.Sync(member.Name, ref refUInt16);
                                currentValue = refUInt16;
                                break;
                            case "UInt32":
                                refUInt32 = (UInt32)currentValue;
                                ser.Sync(member.Name, ref refUInt32);
                                currentValue = refUInt32;
                                break;
                            default:
                                fail = true;
                                break;
                        }
                    }

                    if (member.MemberType == MemberTypes.Property)
                    {
                        if (propInfo.CanWrite && !fail)
                        {
                            MethodInfo setMethod = propInfo.GetSetMethod();
                            setMethod.Invoke(obj, new object[] { currentValue });
                        }
                    }

                    if (member.MemberType == MemberTypes.Field)
                    {
                        fieldInfo.SetValue(obj, currentValue);
                    }
                }
            }
        }
    }
}
