local helpers = {};
helpers.EmuHawk_pre_2_9_bit = function()
	local wrapped_bit = {
		band = function(val, amt) return val & amt; end,
		bnot = function(val) return ~val; end,
		bor = function(val, amt) return val | amt; end,
		bxor = function(val, amt) return val ~ amt; end, -- not a typo
		lshift = function(val, amt) return val << amt; end,
		rol = bit.rol,
		ror = bit.ror,
		rshift = function(val, amt) return val >> amt; end,
		arshift = bit.arshift,
		check = bit.check,
		set = bit.set,
		clear = bit.clear,
		byteswap_16 = bit.byteswap_16,
		byteswap_32 = bit.byteswap_32,
		byteswap_64 = bit.byteswap_64,
	};
	return wrapped_bit;
end;
return helpers;
