-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---Main memory library reads and writes from the Main memory domain (the default memory domain set by any given core)
---@class mainmemory
mainmemory = {}

---Returns the number of bytes of the domain defined as main memory
---
---Example:
---
---	local uimaiget = mainmemory.getcurrentmemorydomainsize( );
---@return integer
function mainmemory.getcurrentmemorydomainsize() end

---returns the name of the domain defined as main memory for the given core
---
---Example:
---
---	local stmaiget = mainmemory.getname( );
---@return string
function mainmemory.getname() end

---Reads length bytes starting at addr into an array-like table (1-indexed).
---
---Example:
---
---	local bytes = mainmemory.read_bytes_as_array(0x100, 30);
---@param addr integer
---@param length integer
---@return table
function mainmemory.read_bytes_as_array(addr, length) end

---Reads `length` bytes starting at `addr` into a binary string. This string can be read with functions such as `string.byte` and `string.unpack`. This string can contain any bytes including null bytes, and is not suitable for display as text.
---
---Example:
---
---	local data = mainmemory.read_bytes_as_binary_string(0x100, 32)
---	local some_s32_le, some_float = string.unpack("<i4f", data)
---	for i = 1, #data do
---		print(data:byte(i))
---	end
---@param addr integer
---@param length integer
---@return string
function mainmemory.read_bytes_as_binary_string(addr, length) end

---Reads length bytes starting at addr into a dict-like table (where the keys are the addresses, relative to the start of the main memory).
---
---Example:
---
---	local bytes = mainmemory.read_bytes_as_dict(0x100, 30);
---@param addr integer
---@param length integer
---@return table # Zero-indexed array.
function mainmemory.read_bytes_as_dict(addr, length) end

---read signed 2 byte value, big endian
---
---Example:
---
---	local inmairea = mainmemory.read_s16_be( 0x100 );
---@param addr integer
---@return integer
function mainmemory.read_s16_be(addr) end

---read signed 2 byte value, little endian
---
---Example:
---
---	local inmairea = mainmemory.read_s16_le( 0x100 );
---@param addr integer
---@return integer
function mainmemory.read_s16_le(addr) end

---read signed 24 bit value, big endian
---
---Example:
---
---	local inmairea = mainmemory.read_s24_be( 0x100 );
---@param addr integer
---@return integer
function mainmemory.read_s24_be(addr) end

---read signed 24 bit value, little endian
---
---Example:
---
---	local inmairea = mainmemory.read_s24_le( 0x100 );
---@param addr integer
---@return integer
function mainmemory.read_s24_le(addr) end

---read signed 4 byte value, big endian
---
---Example:
---
---	local inmairea = mainmemory.read_s32_be( 0x100 );
---@param addr integer
---@return integer
function mainmemory.read_s32_be(addr) end

---read signed 4 byte value, little endian
---
---Example:
---
---	local inmairea = mainmemory.read_s32_le( 0x100 );
---@param addr integer
---@return integer
function mainmemory.read_s32_le(addr) end

---read signed byte
---
---Example:
---
---	local inmairea = mainmemory.read_s8( 0x100 );
---@param addr integer
---@return integer
function mainmemory.read_s8(addr) end

---read unsigned 2 byte value, big endian
---
---Example:
---
---	local uimairea = mainmemory.read_u16_be( 0x100 );
---@param addr integer
---@return integer
function mainmemory.read_u16_be(addr) end

---read unsigned 2 byte value, little endian
---
---Example:
---
---	local uimairea = mainmemory.read_u16_le( 0x100 );
---@param addr integer
---@return integer
function mainmemory.read_u16_le(addr) end

---read unsigned 24 bit value, big endian
---
---Example:
---
---	local uimairea = mainmemory.read_u24_be( 0x100 );
---@param addr integer
---@return integer
function mainmemory.read_u24_be(addr) end

---read unsigned 24 bit value, little endian
---
---Example:
---
---	local uimairea = mainmemory.read_u24_le( 0x100 );
---@param addr integer
---@return integer
function mainmemory.read_u24_le(addr) end

---read unsigned 4 byte value, big endian
---
---Example:
---
---	local uimairea = mainmemory.read_u32_be( 0x100 );
---@param addr integer
---@return integer
function mainmemory.read_u32_be(addr) end

---read unsigned 4 byte value, little endian
---
---Example:
---
---	local uimairea = mainmemory.read_u32_le( 0x100 );
---@param addr integer
---@return integer
function mainmemory.read_u32_le(addr) end

---read unsigned byte
---
---Example:
---
---	local uimairea = mainmemory.read_u8( 0x100 );
---@param addr integer
---@return integer
function mainmemory.read_u8(addr) end

---gets the value from the given address as an unsigned byte
---
---Example:
---
---	local uimairea = mainmemory.readbyte( 0x100 );
---@param addr integer
---@return integer
function mainmemory.readbyte(addr) end

---Reads the address range that starts from address, and is length long. Returns a zero-indexed table containing the read values (an array of bytes.)
---@deprecated
---@param addr integer
---@param length integer
---@return table # Zero-indexed array.
function mainmemory.readbyterange(addr, length) end

---Reads the given address as a 32-bit float value from the main memory domain with th e given endian
---
---Example:
---
---	local simairea = mainmemory.readfloat(0x100, false);
---@param addr integer
---@param bigendian boolean
---@return number
function mainmemory.readfloat(addr, bigendian) end

---Writes sequential bytes starting at addr.
---
---Example:
---
---	mainmemory.write_bytes_as_array(0x100, { 0xAB, 0x12, 0xCD, 0x34 });
---@param addr integer
---@param bytes table
function mainmemory.write_bytes_as_array(addr, bytes) end

---Writes bytes from a binary string to `addr`. The string can be created with functions such as `string.pack`, and can contain any bytes including null bytes. This is not a text encoding function.
---
---Example:
---
---	mainmemory.write_bytes_as_binary_string(0x100, string.pack("<i4f", 1234, 456.789))
---	mainmemory.write_bytes_as_binary_string(0x108, "\xFE\xED")
---	mainmemory.write_bytes_as_binary_string(0x10A, string.char(0xBE, 0xEF))
---@param addr integer
---@param bytes string
function mainmemory.write_bytes_as_binary_string(addr, bytes) end

---Writes bytes at arbitrary addresses (the keys of the given table are the addresses, relative to the start of the main memory).
---
---Example:
---
---	mainmemory.write_bytes_as_dict({ [0x100] = 0xAB, [0x104] = 0xCD, [0x106] = 0x12, [0x107] = 0x34, [0x108] = 0xEF });
---@param addrMap table Zero-indexed array.
function mainmemory.write_bytes_as_dict(addrMap) end

---write signed 2 byte value, big endian
---
---Example:
---
---	mainmemory.write_s16_be( 0x100, -1000 );
---@param addr integer
---@param value integer
function mainmemory.write_s16_be(addr, value) end

---write signed 2 byte value, little endian
---
---Example:
---
---	mainmemory.write_s16_le( 0x100, -1000 );
---@param addr integer
---@param value integer
function mainmemory.write_s16_le(addr, value) end

---write signed 24 bit value, big endian
---
---Example:
---
---	mainmemory.write_s24_be( 0x100, -1000 );
---@param addr integer
---@param value integer
function mainmemory.write_s24_be(addr, value) end

---write signed 24 bit value, little endian
---
---Example:
---
---	mainmemory.write_s24_le( 0x100, -1000 );
---@param addr integer
---@param value integer
function mainmemory.write_s24_le(addr, value) end

---write signed 4 byte value, big endian
---
---Example:
---
---	mainmemory.write_s32_be( 0x100, -1000 );
---@param addr integer
---@param value integer
function mainmemory.write_s32_be(addr, value) end

---write signed 4 byte value, little endian
---
---Example:
---
---	mainmemory.write_s32_le( 0x100, -1000 );
---@param addr integer
---@param value integer
function mainmemory.write_s32_le(addr, value) end

---write signed byte
---
---Example:
---
---	mainmemory.write_s8( 0x100, 1000 );
---@param addr integer
---@param value integer
function mainmemory.write_s8(addr, value) end

---write unsigned 2 byte value, big endian
---
---Example:
---
---	mainmemory.write_u16_be( 0x100, 1000 );
---@param addr integer
---@param value integer
function mainmemory.write_u16_be(addr, value) end

---write unsigned 2 byte value, little endian
---
---Example:
---
---	mainmemory.write_u16_le( 0x100, 1000 );
---@param addr integer
---@param value integer
function mainmemory.write_u16_le(addr, value) end

---write unsigned 24 bit value, big endian
---
---Example:
---
---	mainmemory.write_u24_be( 0x100, 1000 );
---@param addr integer
---@param value integer
function mainmemory.write_u24_be(addr, value) end

---write unsigned 24 bit value, little endian
---
---Example:
---
---	mainmemory.write_u24_le( 0x100, 1000 );
---@param addr integer
---@param value integer
function mainmemory.write_u24_le(addr, value) end

---write unsigned 4 byte value, big endian
---
---Example:
---
---	mainmemory.write_u32_be( 0x100, 1000 );
---@param addr integer
---@param value integer
function mainmemory.write_u32_be(addr, value) end

---write unsigned 4 byte value, little endian
---
---Example:
---
---	mainmemory.write_u32_le( 0x100, 1000 );
---@param addr integer
---@param value integer
function mainmemory.write_u32_le(addr, value) end

---write unsigned byte
---
---Example:
---
---	mainmemory.write_u8( 0x100, 1000 );
---@param addr integer
---@param value integer
function mainmemory.write_u8(addr, value) end

---Writes the given value to the given address as an unsigned byte
---
---Example:
---
---	mainmemory.writebyte( 0x100, 1000 );
---@param addr integer
---@param value integer
function mainmemory.writebyte(addr, value) end

---Writes the given values to the given addresses as unsigned bytes
---@deprecated
---@param memoryblock table Zero-indexed array.
function mainmemory.writebyterange(memoryblock) end

---Writes the given 32-bit float value to the given address and endian
---
---Example:
---
---	mainmemory.writefloat( 0x100, 10.0, false );
---@param addr integer
---@param value number
---@param bigendian boolean
function mainmemory.writefloat(addr, value, bigendian) end

