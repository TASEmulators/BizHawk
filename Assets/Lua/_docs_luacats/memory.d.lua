-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---These functions behavior identically to the mainmemory functions but the user can set the memory domain to read and write from. The default domain is the system bus. Use getcurrentmemorydomain(), and usememorydomain() to control which domain is used. Each core has its own set of valid memory domains. Use getmemorydomainlist() to get a list of memory domains for the current core loaded.
---@class memory
memory = {}

---Returns a string name of the current memory domain selected by Lua. The default is Main memory
---
---Example:
---
---	local stmemget = memory.getcurrentmemorydomain( );
---@return string
function memory.getcurrentmemorydomain() end

---Returns the number of bytes of the current memory domain selected by Lua. The default is Main memory
---
---Example:
---
---	local uimemget = memory.getcurrentmemorydomainsize( );
---@return integer
function memory.getcurrentmemorydomainsize() end

---Returns a string of the memory domains for the loaded platform core. List will be a single string delimited by line feeds
---
---Example:
---
---	local nlmemget = memory.getmemorydomainlist();
---@return table # Zero-indexed array.
function memory.getmemorydomainlist() end

---Returns the number of bytes of the specified memory domain. If no domain is specified, or the specified domain doesn't exist, returns the current domain size
---
---Example:
---
---	local uimemget = memory.getmemorydomainsize( mainmemory.getname( ) );
---@param name? string
---@return integer
function memory.getmemorydomainsize(name) end

---Returns a hash as a string of a region of memory, starting from addr, through count bytes. If the domain is unspecified, it uses the current region.
---
---Example:
---
---	local stmemhas = memory.hash_region( 0x100, 50, mainmemory.getname( ) );
---@param addr integer
---@param count integer
---@param domain? string
---@return string
function memory.hash_region(addr, count, domain) end

---Reads length bytes starting at addr into an array-like table (1-indexed).
---
---Example:
---
---	local bytes = memory.read_bytes_as_array(0x100, 30, "WRAM");
---@param addr integer
---@param length integer
---@param domain? string
---@return table
function memory.read_bytes_as_array(addr, length, domain) end

---Reads `length` bytes starting at `addr` into a binary string. This string can be read with functions such as `string.byte` and `string.unpack`. This string can contain any bytes including null bytes, and is not suitable for display as text.
---
---Example:
---
---	local data = memory.read_bytes_as_binary_string(0x100, 32, "WRAM")
---	local some_s32_le, some_float = string.unpack("<i4f", data)
---	for i = 1, #data do
---		print(data:byte(i))
---	end
---@param addr integer
---@param length integer
---@param domain? string
---@return string
function memory.read_bytes_as_binary_string(addr, length, domain) end

---Reads length bytes starting at addr into a dict-like table (where the keys are the addresses, relative to the start of the domain).
---
---Example:
---
---	local bytes = memory.read_bytes_as_dict(0x100, 30, "WRAM");
---@param addr integer
---@param length integer
---@param domain? string
---@return table # Zero-indexed array.
function memory.read_bytes_as_dict(addr, length, domain) end

---read signed 2 byte value, big endian
---
---Example:
---
---	local inmemrea = memory.read_s16_be( 0x100, mainmemory.getname( ) );
---@param addr integer
---@param domain? string
---@return integer
function memory.read_s16_be(addr, domain) end

---read signed 2 byte value, little endian
---
---Example:
---
---	local inmemrea = memory.read_s16_le( 0x100, mainmemory.getname( ) );
---@param addr integer
---@param domain? string
---@return integer
function memory.read_s16_le(addr, domain) end

---read signed 24 bit value, big endian
---
---Example:
---
---	local inmemrea = memory.read_s24_be( 0x100, mainmemory.getname( ) );
---@param addr integer
---@param domain? string
---@return integer
function memory.read_s24_be(addr, domain) end

---read signed 24 bit value, little endian
---
---Example:
---
---	local inmemrea = memory.read_s24_le( 0x100, mainmemory.getname( ) );
---@param addr integer
---@param domain? string
---@return integer
function memory.read_s24_le(addr, domain) end

---read signed 4 byte value, big endian
---
---Example:
---
---	local inmemrea = memory.read_s32_be( 0x100, mainmemory.getname( ) );
---@param addr integer
---@param domain? string
---@return integer
function memory.read_s32_be(addr, domain) end

---read signed 4 byte value, little endian
---
---Example:
---
---	local inmemrea = memory.read_s32_le( 0x100, mainmemory.getname( ) );
---@param addr integer
---@param domain? string
---@return integer
function memory.read_s32_le(addr, domain) end

---read signed byte
---
---Example:
---
---	local inmemrea = memory.read_s8( 0x100, mainmemory.getname( ) );
---@param addr integer
---@param domain? string
---@return integer
function memory.read_s8(addr, domain) end

---read unsigned 2 byte value, big endian
---
---Example:
---
---	local uimemrea = memory.read_u16_be( 0x100, mainmemory.getname( ) );
---@param addr integer
---@param domain? string
---@return integer
function memory.read_u16_be(addr, domain) end

---read unsigned 2 byte value, little endian
---
---Example:
---
---	local uimemrea = memory.read_u16_le( 0x100, mainmemory.getname( ) );
---@param addr integer
---@param domain? string
---@return integer
function memory.read_u16_le(addr, domain) end

---read unsigned 24 bit value, big endian
---
---Example:
---
---	local uimemrea = memory.read_u24_be( 0x100, mainmemory.getname( ) );
---@param addr integer
---@param domain? string
---@return integer
function memory.read_u24_be(addr, domain) end

---read unsigned 24 bit value, little endian
---
---Example:
---
---	local uimemrea = memory.read_u24_le( 0x100, mainmemory.getname( ) );
---@param addr integer
---@param domain? string
---@return integer
function memory.read_u24_le(addr, domain) end

---read unsigned 4 byte value, big endian
---
---Example:
---
---	local uimemrea = memory.read_u32_be( 0x100, mainmemory.getname( ) );
---@param addr integer
---@param domain? string
---@return integer
function memory.read_u32_be(addr, domain) end

---read unsigned 4 byte value, little endian
---
---Example:
---
---	local uimemrea = memory.read_u32_le( 0x100, mainmemory.getname( ) );
---@param addr integer
---@param domain? string
---@return integer
function memory.read_u32_le(addr, domain) end

---read unsigned byte
---
---Example:
---
---	local uimemrea = memory.read_u8( 0x100, mainmemory.getname( ) );
---@param addr integer
---@param domain? string
---@return integer
function memory.read_u8(addr, domain) end

---gets the value from the given address as an unsigned byte
---
---Example:
---
---	local uimemrea = memory.readbyte( 0x100, mainmemory.getname( ) );
---@param addr integer
---@param domain? string
---@return integer
function memory.readbyte(addr, domain) end

---Reads the address range that starts from address, and is length long. Returns a zero-indexed table containing the read values (an array of bytes.)
---@deprecated
---@param addr integer
---@param length integer
---@param domain? string
---@return table # Zero-indexed array.
function memory.readbyterange(addr, length, domain) end

---Reads the given address as a 32-bit float value from the main memory domain with th e given endian
---
---Example:
---
---	local simemrea = memory.readfloat( 0x100, false, mainmemory.getname( ) );
---@param addr integer
---@param bigendian boolean
---@param domain? string
---@return number
function memory.readfloat(addr, bigendian, domain) end

---Attempts to set the current memory domain to the given domain. If the name does not match a valid memory domain, the function returns false, else it returns true
---
---Example:
---
---	if ( memory.usememorydomain( mainmemory.getname( ) ) ) then
---		console.log( "Attempts to set the current memory domain to the given domain. If the name does not match a valid memory domain, the function returns false, else it returns true" );
---	end;
---@param domain string
---@return boolean
function memory.usememorydomain(domain) end

---Writes sequential bytes starting at addr.
---
---Example:
---
---	memory.write_bytes_as_array(0x100, { 0xAB, 0x12, 0xCD, 0x34 });
---@param addr integer
---@param bytes table
---@param domain? string
function memory.write_bytes_as_array(addr, bytes, domain) end

---Writes bytes from a binary string to `addr`. The string can be created with functions such as `string.pack`, and can contain any bytes including null bytes. This is not a text encoding function.
---
---Example:
---
---	memory.write_bytes_as_binary_string(0x100, string.pack("<i4f", 1234, 456.789), "WRAM")
---	memory.write_bytes_as_binary_string(0x108, "\xFE\xED", "WRAM")
---	memory.write_bytes_as_binary_string(0x10A, string.char(0xBE, 0xEF), "WRAM")
---@param addr integer
---@param bytes string
---@param domain? string
function memory.write_bytes_as_binary_string(addr, bytes, domain) end

---Writes bytes at arbitrary addresses (the keys of the given table are the addresses, relative to the start of the domain).
---
---Example:
---
---	memory.write_bytes_as_dict({ [0x100] = 0xAB, [0x104] = 0xCD, [0x106] = 0x12, [0x107] = 0x34, [0x108] = 0xEF });
---@param addrMap table Zero-indexed array.
---@param domain? string
function memory.write_bytes_as_dict(addrMap, domain) end

---write signed 2 byte value, big endian
---
---Example:
---
---	memory.write_s16_be( 0x100, -1000, mainmemory.getname( ) );
---@param addr integer
---@param value integer
---@param domain? string
function memory.write_s16_be(addr, value, domain) end

---write signed 2 byte value, little endian
---
---Example:
---
---	memory.write_s16_le( 0x100, -1000, mainmemory.getname( ) );
---@param addr integer
---@param value integer
---@param domain? string
function memory.write_s16_le(addr, value, domain) end

---write signed 24 bit value, big endian
---
---Example:
---
---	memory.write_s24_be( 0x100, -1000, mainmemory.getname( ) );
---@param addr integer
---@param value integer
---@param domain? string
function memory.write_s24_be(addr, value, domain) end

---write signed 24 bit value, little endian
---
---Example:
---
---	memory.write_s24_le( 0x100, -1000, mainmemory.getname( ) );
---@param addr integer
---@param value integer
---@param domain? string
function memory.write_s24_le(addr, value, domain) end

---write signed 4 byte value, big endian
---
---Example:
---
---	memory.write_s32_be( 0x100, -1000, mainmemory.getname( ) );
---@param addr integer
---@param value integer
---@param domain? string
function memory.write_s32_be(addr, value, domain) end

---write signed 4 byte value, little endian
---
---Example:
---
---	memory.write_s32_le( 0x100, -1000, mainmemory.getname( ) );
---@param addr integer
---@param value integer
---@param domain? string
function memory.write_s32_le(addr, value, domain) end

---write signed byte
---
---Example:
---
---	memory.write_s8( 0x100, 1000, mainmemory.getname( ) );
---@param addr integer
---@param value integer
---@param domain? string
function memory.write_s8(addr, value, domain) end

---write unsigned 2 byte value, big endian
---
---Example:
---
---	memory.write_u16_be( 0x100, 1000, mainmemory.getname( ) );
---@param addr integer
---@param value integer
---@param domain? string
function memory.write_u16_be(addr, value, domain) end

---write unsigned 2 byte value, little endian
---
---Example:
---
---	memory.write_u16_le( 0x100, 1000, mainmemory.getname( ) );
---@param addr integer
---@param value integer
---@param domain? string
function memory.write_u16_le(addr, value, domain) end

---write unsigned 24 bit value, big endian
---
---Example:
---
---	memory.write_u24_be( 0x100, 1000, mainmemory.getname( ) );
---@param addr integer
---@param value integer
---@param domain? string
function memory.write_u24_be(addr, value, domain) end

---write unsigned 24 bit value, little endian
---
---Example:
---
---	memory.write_u24_le( 0x100, 1000, mainmemory.getname( ) );
---@param addr integer
---@param value integer
---@param domain? string
function memory.write_u24_le(addr, value, domain) end

---write unsigned 4 byte value, big endian
---
---Example:
---
---	memory.write_u32_be( 0x100, 1000, mainmemory.getname( ) );
---@param addr integer
---@param value integer
---@param domain? string
function memory.write_u32_be(addr, value, domain) end

---write unsigned 4 byte value, little endian
---
---Example:
---
---	memory.write_u32_le( 0x100, 1000, mainmemory.getname( ) );
---@param addr integer
---@param value integer
---@param domain? string
function memory.write_u32_le(addr, value, domain) end

---write unsigned byte
---
---Example:
---
---	memory.write_u8( 0x100, 1000, mainmemory.getname( ) );
---@param addr integer
---@param value integer
---@param domain? string
function memory.write_u8(addr, value, domain) end

---Writes the given value to the given address as an unsigned byte
---
---Example:
---
---	memory.writebyte( 0x100, 1000, mainmemory.getname( ) );
---@param addr integer
---@param value integer
---@param domain? string
function memory.writebyte(addr, value, domain) end

---Writes the given values to the given addresses as unsigned bytes
---@deprecated
---@param memoryblock table Zero-indexed array.
---@param domain? string
function memory.writebyterange(memoryblock, domain) end

---Writes the given 32-bit float value to the given address and endian
---
---Example:
---
---	memory.writefloat( 0x100, 10.0, false, mainmemory.getname( ) );
---@param addr integer
---@param value number
---@param bigendian boolean
---@param domain? string
function memory.writefloat(addr, value, bigendian, domain) end

