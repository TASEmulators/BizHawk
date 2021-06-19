--
-- Author: HyagoGow
-- A common Vector2 class
--

local class = require("middleclass")
local Vector2 = class('Vector2')

function Vector2:initialize(x, y)
    self.x = x or 0
    self.y = y or 0
end

function Vector2:log()
    console.log(tostring(self))
end

function Vector2:logHex()
    local text = string.format("(%s, %s)", bizstring.hex(self.x), bizstring.hex(self.y))
    console.log(text)
end

function Vector2:print(x, y, label, forecolor, anchor)
    local text = string.format("%s: %s", label, tostring(self))
    gui.text(x, y, text, forecolor, anchor)
end

function Vector2:drawAxis(color, size, surfacename)
    size = size or 2
    color = color or "red"
    gui.drawAxis(self.x, self.y, size, color, surfacename)
end

function Vector2:__tostring()
    return string.format("(%d, %d)", self.x, self.y)
end

function Vector2:__eq(other)
    return self.x == other.x and self.y == other.y
end

function Vector2:__add(other)
    return Vector2:new(self.x + other.x, self.y + other.y)
end

function Vector2:__sub(other)
    return Vector2:new(self.x - other.x, self.y - other.y)
end

function Vector2:__mul(i)
    return Vector2:new(self.x * i, self.y * i)
end

return Vector2
