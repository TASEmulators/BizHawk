// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace NymaTypes
{

using global::System;
using global::System.Collections.Generic;
using global::Google.FlatBuffers;

public struct NInputInfo : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_22_9_24(); }
  public static NInputInfo GetRootAsNInputInfo(ByteBuffer _bb) { return GetRootAsNInputInfo(_bb, new NInputInfo()); }
  public static NInputInfo GetRootAsNInputInfo(ByteBuffer _bb, NInputInfo obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
  public NInputInfo __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public string SettingName { get { int o = __p.__offset(4); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetSettingNameBytes() { return __p.__vector_as_span<byte>(4, 1); }
#else
  public ArraySegment<byte>? GetSettingNameBytes() { return __p.__vector_as_arraysegment(4); }
#endif
  public byte[] GetSettingNameArray() { return __p.__vector_as_array<byte>(4); }
  public string Name { get { int o = __p.__offset(6); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetNameBytes() { return __p.__vector_as_span<byte>(6, 1); }
#else
  public ArraySegment<byte>? GetNameBytes() { return __p.__vector_as_arraysegment(6); }
#endif
  public byte[] GetNameArray() { return __p.__vector_as_array<byte>(6); }
  public short ConfigOrder { get { int o = __p.__offset(8); return o != 0 ? __p.bb.GetShort(o + __p.bb_pos) : (short)0; } }
  public ushort BitOffset { get { int o = __p.__offset(10); return o != 0 ? __p.bb.GetUshort(o + __p.bb_pos) : (ushort)0; } }
  public NymaTypes.InputType Type { get { int o = __p.__offset(12); return o != 0 ? (NymaTypes.InputType)__p.bb.Get(o + __p.bb_pos) : NymaTypes.InputType.Padding; } }
  public NymaTypes.AxisFlags Flags { get { int o = __p.__offset(14); return o != 0 ? (NymaTypes.AxisFlags)__p.bb.Get(o + __p.bb_pos) : 0; } }
  public byte BitSize { get { int o = __p.__offset(16); return o != 0 ? __p.bb.Get(o + __p.bb_pos) : (byte)0; } }
  public NymaTypes.NInputExtra ExtraType { get { int o = __p.__offset(18); return o != 0 ? (NymaTypes.NInputExtra)__p.bb.Get(o + __p.bb_pos) : NymaTypes.NInputExtra.NONE; } }
  public TTable? Extra<TTable>() where TTable : struct, IFlatbufferObject { int o = __p.__offset(20); return o != 0 ? (TTable?)__p.__union<TTable>(o + __p.bb_pos) : null; }
  public NymaTypes.NButtonInfo ExtraAsButton() { return Extra<NymaTypes.NButtonInfo>().Value; }
  public NymaTypes.NAxisInfo ExtraAsAxis() { return Extra<NymaTypes.NAxisInfo>().Value; }
  public NymaTypes.NSwitchInfo ExtraAsSwitch() { return Extra<NymaTypes.NSwitchInfo>().Value; }
  public NymaTypes.NStatusInfo ExtraAsStatus() { return Extra<NymaTypes.NStatusInfo>().Value; }

  public static Offset<NymaTypes.NInputInfo> CreateNInputInfo(FlatBufferBuilder builder,
      StringOffset SettingNameOffset = default(StringOffset),
      StringOffset NameOffset = default(StringOffset),
      short ConfigOrder = 0,
      ushort BitOffset = 0,
      NymaTypes.InputType Type = NymaTypes.InputType.Padding,
      NymaTypes.AxisFlags Flags = 0,
      byte BitSize = 0,
      NymaTypes.NInputExtra Extra_type = NymaTypes.NInputExtra.NONE,
      int ExtraOffset = 0) {
    builder.StartTable(9);
    NInputInfo.AddExtra(builder, ExtraOffset);
    NInputInfo.AddName(builder, NameOffset);
    NInputInfo.AddSettingName(builder, SettingNameOffset);
    NInputInfo.AddBitOffset(builder, BitOffset);
    NInputInfo.AddConfigOrder(builder, ConfigOrder);
    NInputInfo.AddExtraType(builder, Extra_type);
    NInputInfo.AddBitSize(builder, BitSize);
    NInputInfo.AddFlags(builder, Flags);
    NInputInfo.AddType(builder, Type);
    return NInputInfo.EndNInputInfo(builder);
  }

  public static void StartNInputInfo(FlatBufferBuilder builder) { builder.StartTable(9); }
  public static void AddSettingName(FlatBufferBuilder builder, StringOffset SettingNameOffset) { builder.AddOffset(0, SettingNameOffset.Value, 0); }
  public static void AddName(FlatBufferBuilder builder, StringOffset NameOffset) { builder.AddOffset(1, NameOffset.Value, 0); }
  public static void AddConfigOrder(FlatBufferBuilder builder, short ConfigOrder) { builder.AddShort(2, ConfigOrder, 0); }
  public static void AddBitOffset(FlatBufferBuilder builder, ushort BitOffset) { builder.AddUshort(3, BitOffset, 0); }
  public static void AddType(FlatBufferBuilder builder, NymaTypes.InputType Type) { builder.AddByte(4, (byte)Type, 0); }
  public static void AddFlags(FlatBufferBuilder builder, NymaTypes.AxisFlags Flags) { builder.AddByte(5, (byte)Flags, 0); }
  public static void AddBitSize(FlatBufferBuilder builder, byte BitSize) { builder.AddByte(6, BitSize, 0); }
  public static void AddExtraType(FlatBufferBuilder builder, NymaTypes.NInputExtra ExtraType) { builder.AddByte(7, (byte)ExtraType, 0); }
  public static void AddExtra(FlatBufferBuilder builder, int ExtraOffset) { builder.AddOffset(8, ExtraOffset, 0); }
  public static Offset<NymaTypes.NInputInfo> EndNInputInfo(FlatBufferBuilder builder) {
    int o = builder.EndTable();
    return new Offset<NymaTypes.NInputInfo>(o);
  }
  public NInputInfoT UnPack() {
    var _o = new NInputInfoT();
    this.UnPackTo(_o);
    return _o;
  }
  public void UnPackTo(NInputInfoT _o) {
    _o.SettingName = this.SettingName;
    _o.Name = this.Name;
    _o.ConfigOrder = this.ConfigOrder;
    _o.BitOffset = this.BitOffset;
    _o.Type = this.Type;
    _o.Flags = this.Flags;
    _o.BitSize = this.BitSize;
    _o.Extra = new NymaTypes.NInputExtraUnion();
    _o.Extra.Type = this.ExtraType;
    switch (this.ExtraType) {
      default: break;
      case NymaTypes.NInputExtra.Button:
        _o.Extra.Value = this.Extra<NymaTypes.NButtonInfo>().HasValue ? this.Extra<NymaTypes.NButtonInfo>().Value.UnPack() : null;
        break;
      case NymaTypes.NInputExtra.Axis:
        _o.Extra.Value = this.Extra<NymaTypes.NAxisInfo>().HasValue ? this.Extra<NymaTypes.NAxisInfo>().Value.UnPack() : null;
        break;
      case NymaTypes.NInputExtra.Switch:
        _o.Extra.Value = this.Extra<NymaTypes.NSwitchInfo>().HasValue ? this.Extra<NymaTypes.NSwitchInfo>().Value.UnPack() : null;
        break;
      case NymaTypes.NInputExtra.Status:
        _o.Extra.Value = this.Extra<NymaTypes.NStatusInfo>().HasValue ? this.Extra<NymaTypes.NStatusInfo>().Value.UnPack() : null;
        break;
    }
  }
  public static Offset<NymaTypes.NInputInfo> Pack(FlatBufferBuilder builder, NInputInfoT _o) {
    if (_o == null) return default(Offset<NymaTypes.NInputInfo>);
    var _SettingName = _o.SettingName == null ? default(StringOffset) : builder.CreateString(_o.SettingName);
    var _Name = _o.Name == null ? default(StringOffset) : builder.CreateString(_o.Name);
    var _Extra_type = _o.Extra == null ? NymaTypes.NInputExtra.NONE : _o.Extra.Type;
    var _Extra = _o.Extra == null ? 0 : NymaTypes.NInputExtraUnion.Pack(builder, _o.Extra);
    return CreateNInputInfo(
      builder,
      _SettingName,
      _Name,
      _o.ConfigOrder,
      _o.BitOffset,
      _o.Type,
      _o.Flags,
      _o.BitSize,
      _Extra_type,
      _Extra);
  }
}

public class NInputInfoT
{
  public string SettingName { get; set; }
  public string Name { get; set; }
  public short ConfigOrder { get; set; }
  public ushort BitOffset { get; set; }
  public NymaTypes.InputType Type { get; set; }
  public NymaTypes.AxisFlags Flags { get; set; }
  public byte BitSize { get; set; }
  public NymaTypes.NInputExtraUnion Extra { get; set; }

  public NInputInfoT() {
    this.SettingName = null;
    this.Name = null;
    this.ConfigOrder = 0;
    this.BitOffset = 0;
    this.Type = NymaTypes.InputType.Padding;
    this.Flags = 0;
    this.BitSize = 0;
    this.Extra = null;
  }
}


}
