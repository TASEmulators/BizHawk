static const vector<string> commandNames = {
  "No_Operation", "Invalid_01", "Invalid_02", "Invalid_03",
  "Invalid_04",   "Invalid_05", "Invalid_06", "Invalid_07",
  "Unshaded_Triangle",
  "Unshaded_Zbuffer_Triangle",
  "Texture_Triangle",
  "Texture_Zbuffer_Triangle",
  "Shaded_Triangle",
  "Shaded_Zbuffer_Triangle",
  "Shaded_Texture_Triangle",
  "Shaded_Texture_Zbuffer_Triangle",
  "Invalid_10", "Invalid_11", "Invalid_12", "Invalid_13",
  "Invalid_14", "Invalid_15", "Invalid_16", "Invalid_17",
  "Invalid_18", "Invalid_19", "Invalid_1a", "Invalid_1b",
  "Invalid_1c", "Invalid_1d", "Invalid_1e", "Invalid_1f",
  "Invalid_20", "Invalid_21", "Invalid_22", "Invalid_23",
  "Texture_Rectangle",
  "Texture_Rectangle_Flip",
  "Sync_Load",
  "Sync_Pipe",
  "Sync_Tile",
  "Sync_Full",
  "Set_Key_GB",
  "Set_Key_R",
  "Set_Convert",
  "Set_Scissor",
  "Set_Primitive_Depth",
  "Set_Other_Modes",
  "Load_Texture_LUT",
  "Invalid_31",
  "Set_Tile_Size",
  "Load_Block",
  "Load_Tile",
  "Set_Tile",
  "Fill_Rectangle",
  "Set_Fill_Color",
  "Set_Fog_Color",
  "Set_Blend_Color",
  "Set_Primitive_Color",
  "Set_Environment_Color",
  "Set_Combine_Mode",
  "Set_Texture_Image",
  "Set_Mask_Image",
  "Set_Color_Image",
};

auto RDP::render() -> void {
  angrylion::ProcessRDPList();
  command.current = command.end;
}

//0x00
auto RDP::noOperation() -> void {
}

//0x01-0x07; 0x10-0x23; 0x31
auto RDP::invalidOperation() -> void {
}

//0x08
auto RDP::unshadedTriangle() -> void {
}

//0x09
auto RDP::unshadedZbufferTriangle() -> void {
}

//0x0a
auto RDP::textureTriangle() -> void {
}

//0x0b
auto RDP::textureZbufferTriangle() -> void {
}

//0x0c
auto RDP::shadedTriangle() -> void {
}

//0x0d
auto RDP::shadedZbufferTriangle() -> void {
}

//0x0e
auto RDP::shadedTextureTriangle() -> void {
}

//0x0f
auto RDP::shadedTextureZbufferTriangle() -> void {
}

//0x24
auto RDP::textureRectangle() -> void {
}

//0x25
auto RDP::textureRectangleFlip() -> void {
}

//0x26
auto RDP::syncLoad() -> void {
}

//0x27
auto RDP::syncPipe() -> void {
}

//0x28
auto RDP::syncTile() -> void {
}

//0x29
auto RDP::syncFull() -> void {
  if(!command.crashed) {
    mi.raise(MI::IRQ::DP);
    command.bufferBusy = 0;
    command.pipeBusy = 0;
  }
  command.startGclk = 0;
}

//0x2a
auto RDP::setKeyGB() -> void {
}

//0x2b
auto RDP::setKeyR() -> void {
}

//0x2c
auto RDP::setConvert() -> void {
}

//0x2d
auto RDP::setScissor() -> void {
}

//0x2e
auto RDP::setPrimitiveDepth() -> void {
}

//0x2f
auto RDP::setOtherModes() -> void {
}

//0x30
auto RDP::loadTLUT() -> void {
}

//0x32
auto RDP::setTileSize() -> void {
}

//0x33
auto RDP::loadBlock() -> void {
}

//0x34
auto RDP::loadTile() -> void {
}

//0x35
auto RDP::setTile() -> void {
}

//0x36
auto RDP::fillRectangle() -> void {
}

//0x37
auto RDP::setFillColor() -> void {
}

//0x38
auto RDP::setFogColor() -> void {
}

//0x39
auto RDP::setBlendColor() -> void {
}

//0x3a
auto RDP::setPrimitiveColor() -> void {
}

//0x3b
auto RDP::setEnvironmentColor() -> void {
}

//0x3c
auto RDP::setCombineMode() -> void {
}

//0x3d
auto RDP::setTextureImage() -> void {
}

//0x3e
auto RDP::setMaskImage() -> void {
}

//0x3f
auto RDP::setColorImage() -> void {
}
