-- kalimag, 2025

local utils = {}

 -- Read the high word of every pointer and check that it matches the expected base address (for debugging)
utils.check_pointers = false



local NULL_OBJECT <const>    = 0x88888888 -- no object at that index
local OUT_OF_BOUNDS <const>  = 0xFFFFFFFF -- no such index
local WBX_POINTER_HI <const> = 0x36F
local BusDomain <const>      = "System Bus"



local module_prefix = (...):match([[^(.-)[^./\]+$]])

local read_u32  = memory.read_u32_le
local readfloat = memory.readfloat
local tointeger = math.tointeger

local function asinteger(value)
	return type(value) == "number" and tointeger(value) or nil
end

local function read_float_le(addr, domain)
	return readfloat(addr, false, domain)
end

local function assertf(condition, format, ...)
	if not condition then
		error(string.format(format, ...), 2)
	end
	return condition
end

function utils.read_s64_le(addr, domain)
	return read_u32(addr, domain) | read_u32(addr + 4, domain) << 32
end

-- Returns the lower 4 bytes of an 8 byte pointer
function utils.read_ptr(addr, domain)
	local lo = read_u32(addr, domain)
	if utils.check_pointers then
		local hi = read_u32(addr + 4, domain)
		assertf(hi == WBX_POINTER_HI or hi | lo == 0, "Invalid pointer 0x%08X%08X at %s 0x%X", hi, lo, domain, addr)
	end
	return lo
end

function utils.read_bool(addr, domain)
	return read_u32(addr, domain) ~= 0
end

local function next_linked(state, prev)
	local next
	if prev == nil then
		next = state.start
	else
		next = prev[state.key]
	end
	local value = state.value
	return next, next and value and next[value]
end

function utils.links(start, key, value)
	return next_linked, { start = start, key = key, value = value }
end



local array_meta = {}
function array_meta:__index(index)
	index = asinteger(index)
	if not index or index < 1 or index > self._length then
		return nil
	end
	local offset = (index - 1) * self._size
	return self._read(self._address + offset, self._domain)
end
function array_meta:__len()
	return self._length
end
function array_meta:__pairs()
	return ipairs(self)
end

function utils.array(address, domain, length, size, read_func)
	return setmetatable({
		_address = address,
		_domain = domain,
		_length = length,
		_size = size,
		_read = read_func,
	}, array_meta)
end



local pointer_array_meta = {}
function pointer_array_meta:__index(index)
	index = asinteger(index)
	if not index or index < 1 or index > self._length then
		return nil
	end
	local offset = (index - 1) * 8
	local ptr = utils.read_ptr(self._address + offset, self._domain)
	return self._read(ptr, BusDomain)
end
function pointer_array_meta:__len()
	return self._length
end
function pointer_array_meta:__pairs()
	return ipairs(self)
end

function utils.pointer_array(address, domain, length, read_func)
	return setmetatable({
		_address = address,
		_domain = domain,
		_length = length,
		_read = read_func,
	}, pointer_array_meta)
end



function utils.struct_layout(struct_name)
	local struct = {}
	struct.name = struct_name
	struct.size = 0
	struct.alignment = 1
	struct.offsets = {}

	local item_props = {}
	local item_funcs = {}
	local item_meta = {}
	local function create_item(address, domain)
		assert(domain ~= nil, "No domain specified")
		local item = {
			_address = address,
			_domain = domain,
		}
		setmetatable(item, item_meta)
		return item
	end

	-- iterator for each readable field in the struct
	local function next_item_prop(item, key)
		local next_key = next(item_props, key)
		return next_key, item[next_key]
	end

	struct.from_address_unchecked = create_item

	function struct.from_address(address, domain)
		assertf(address % struct.alignment == 0, "Unaligned address %X", address)
		return create_item(address, domain)
	end

	-- Get a struct instance from the system bus
	function struct.from_pointer(pointer)
		if pointer == 0 then return nil end
		assertf(pointer >> 32 == 0 or pointer >> 32 == WBX_POINTER_HI, "Invalid pointer %X", pointer)
		assertf(pointer % struct.alignment == 0, "Unaligned pointer %X", pointer)
		return create_item(pointer & 0xFFFFFFFF, BusDomain)
	end

	function item_meta:__index(name)
		local prop = item_props[name]
		if prop then return prop(self) end
		local func = item_funcs[name]
		if func then return func end
	end

	function item_meta:__pairs()
		return next_item_prop, self, nil
	end

	function item_meta:__tostring()
		return string.format("%s 0x%X (%s)", struct_name, self._address, self._domain)
	end

	function item_funcs:cast(target_struct)
		return target_struct.from_address_unchecked(self._address, self._domain)
	end


	local builder = {}
	builder.built_struct = struct
	function builder.add(name, size, alignment, read_func)
		assertf(struct.offsets[name] == nil, "Duplicate %s name %s", struct_name, name)

		if alignment == true then alignment = size end
		builder.align(alignment)
		local offset = struct.size
		struct.offsets[name] = offset
		struct.size = offset + size
		builder.align(alignment) -- add padding to structs
		--print(string.format("  %-19s %3X %3X", name, size, offset));

		if read_func then
			item_props[name] = function(self)
				return read_func(self._address + offset, self._domain)
			end
		end
		return builder
	end
	function builder.align(alignment)
		struct.alignment = math.max(struct.alignment, alignment or 1)
		if alignment and struct.size % alignment > 0 then
			--print(string.format("  %i bytes padding", alignment - (struct.size % alignment)))
			builder.pad(alignment - (struct.size % alignment))
		end
		return builder
	end
	function builder.pad(size)
		struct.size = struct.size + size
		return builder
	 end
	function builder.s8   (name) return builder.add(name, 1, true, memory.read_s8) end
	function builder.s16  (name) return builder.add(name, 2, true, memory.read_s16_le) end
	function builder.s32  (name) return builder.add(name, 4, true, memory.read_s32_le) end
	function builder.u8   (name) return builder.add(name, 1, true, memory.read_u8) end
	function builder.u16  (name) return builder.add(name, 2, true, memory.read_u16_le) end
	function builder.u32  (name) return builder.add(name, 4, true, memory.read_u32_le) end
	function builder.s64  (name) return builder.add(name, 8, true, utils.read_s64_le) end
	function builder.float(name) return builder.add(name, 4, true, read_float_le) end
	function builder.ptr  (name) return builder.add(name, 8, true, utils.read_ptr) end
	function builder.bool (name) return builder.add(name, 4, true, utils.read_bool) end
	function builder.array(name, type, length, ...)
		assertf(asinteger(length) and length > 0, "%s.%s: invalid length", struct_name, name)
		--print(string.format("  %-19s %s[%i]", name, type, count))
		local element_props = {}
		for i = 1, length do
			local element_name = name..i
			builder[type](element_name, ...)
			element_props[i] = item_props[element_name]
		end
		struct.offsets[name] = struct.offsets[name.."1"]
		if element_props[1] then
			local array_meta = {}
			function array_meta:__index(index)
				local prop = element_props[index]
				return prop and prop(self._item)
			end
			function array_meta:__len()
				return length
			end
			function array_meta:__pairs()
				return ipairs(self)
			end
			item_props[name] = function(self)
				local array = setmetatable({ _item = self }, array_meta)
				self[name] = array
				return array
			end
		end
		return builder
	end
	function builder.ptrto(name, target_struct)
		assertf(target_struct ~= nil, "%s.%s: target_struct is nil", struct_name, name)
		target_struct = target_struct.built_struct or target_struct
		local size = struct.size
		builder.ptr(name .. "_ptr")
		struct.size = size
		builder.add(name, 8, true, function(addr, domain)
			local ptr = utils.read_ptr(addr, domain)
			return target_struct.from_pointer(ptr)
		end)
		return builder
	end
	function builder.embed(name, target_struct)
		assertf(target_struct.built_struct == nil, "%s.%s: target_struct must be a finished struct and not a builder", struct_name, name)
		builder.add(name, target_struct.size, target_struct.alignment, function(addr, domain)
			return target_struct.from_address_unchecked(addr, domain)
		end)
		return builder
	end
	function builder.prop(name, func)
		item_props[name] = func
		return builder
	end
	function builder.func(name, func)
		item_funcs[name] = func
		return builder
	end
	function builder.build()
		builder.align(struct.alignment)
		return struct
	end
	return builder
end



function utils.domain_struct_layout(struct_name, padded_size, domain, max_count)
	local builder = utils.struct_layout(struct_name)

	local struct = builder.built_struct
	struct.padded_size = padded_size
	struct.domain = domain
	struct.items = {} -- This should be iterated with pairs()

	local max_address
	-- Core must be loaded to get memory domain sizes. Throw an error so Lua doesn't cache the module in an invalid state
	assert(emu.getsystemid() == "Doom", "Doom core not loaded")
	max_count = max_count or math.floor(memory.getmemorydomainsize(domain) / padded_size)
	max_address = (max_count - 1) * padded_size
	struct.max_count = max_count
	struct.max_address = max_address

	local items_meta = {}
	setmetatable(struct.items, items_meta)

	local function get_item(address)
		assertf(address >= 0 and address <= max_address and address % padded_size == 0,
			"Invalid %s address %X", struct_name, address)

		local peek = read_u32(address, domain)
		if peek == NULL_OBJECT then
			return nil
		elseif peek == OUT_OF_BOUNDS then
			return false
		end

		return struct.from_address_unchecked(address, domain)
	end

	-- iterator for each instance of the struct in the domain
	local function next_item(_, address)
		address = address and address + padded_size or 0
		while address <= max_address do
			local item = get_item(address)
			if item then
				return address, item
			elseif item == false then -- OUT_OF_BOUNDS
				break
			else -- NULL_OBJECT
				address = address + padded_size
			end
		end
	end

	-- Get a struct instance from its dedicated memory domain
	function struct.from_address(address, domain)
		assertf(address % struct.alignment == 0, "Unaligned address %X", address)
		if domain == nil or domain == struct.domain then
			return get_item(address) or nil
		else
			return struct.from_address_unchecked(address, domain)
		end
	end

	-- Get a struct instance from its dedicated memory domain
	function struct.from_index(index)
		return get_item((index - 1) * (padded_size or 0)) or nil
	end

	function items_meta:__index(index)
		return struct.from_address(index)
	end

	function items_meta:__pairs()
		return next_item
	end

	return builder
end



function utils.global_layout()
	local symbols = require(module_prefix.."symbols")
	---@class global_builder : builder
	local builder = utils.struct_layout("[global]")

	function builder.global(type, symbol, ...)
		local pointer = assertf(symbols[symbol], "Undefined symbol %s", symbol)
		builder.built_struct.size = pointer
		builder[type](symbol, ...)
		return builder
	end

	return builder
end



-- Assign (v, k) for every (k, v) so that the enums can be accessed by name, e.g. `mobjtype.PLAYER`
function utils.assign_enum_keys(table, from, to)
	for i = from or 0, to or math.huge do
		local name = table[i]
		if name ~= nil then
			--assert(table[name] == nil, "duplicate name "..name)
			table[name] = i
		elseif to == nil then
			return
		end
	end
end

return utils
