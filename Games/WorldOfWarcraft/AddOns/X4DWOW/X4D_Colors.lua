local X4D_Colors = LibStub:NewLibrary("X4D_Colors", 2000)
if (not X4D_Colors) then
	return
end
local X4D = LibStub("X4D")
X4D.Colors = X4D_Colors

X4D_Colors.X4D = "|cFFFFAE19"

X4D_Colors.Deposit = "|cFFFFAE19"
X4D_Colors.Withdraw = "|cFFAA33FF"

X4D_Colors.SYSTEM = "|cFFFFFF00"
X4D_Colors.TRACE_DEBUG = "|cFFCCFF70"
X4D_Colors.TRACE_VERBOSE = "|cFFC0C0C0"
X4D_Colors.TRACE_INFORMATION = "|cFF6666FF"
X4D_Colors.TRACE_WARNING = "|cFFCC6600"
X4D_Colors.TRACE_ERROR = "|cFF990000"
X4D_Colors.TRACE_CRITICAL = "|cFFFF0033"

X4D_Colors.BagSpaceLow = "|cFFFFd00b"
X4D_Colors.BagSpaceFull = "|cFFAA0000"
X4D_Colors.StackCount = "|cFFFFFFFF"

X4D_Colors.Subtext = "|cFF5C5C5C"

X4D_Colors.XP = "|cFFAA33FF"

X4D_Colors.Red = "|cFFFF3333"
X4D_Colors.Green = "|cFF33FF33"
X4D_Colors.Blue = "|cFF3333FF"

X4D_Colors.Yellow = "|cFFFFFF00"
X4D_Colors.Cyan = "|cFF00FFFF"
X4D_Colors.Magenta = "|cFFFF00FF"

X4D_Colors.Orange = "|cFFFF9900"

X4D_Colors.White = "|cFFFFFFFF"
X4D_Colors.LightGray = "|cFFC5C5C5"
X4D_Colors.Gray = "|cFF757575"
X4D_Colors.DarkGray = "|cFF353535"
X4D_Colors.Black = "|cFF000000"

function X4D_Colors:Create(r, g, b, a)
	if (a == nil) then
		a = 1
	end
	return "|c" .. X4D.Convert:DEC2HEX(a * 255) .. X4D.Convert:DEC2HEX(r * 255) .. X4D.Convert:DEC2HEX(g * 255) .. X4D.Convert:DEC2HEX(b * 255)
end

function X4D_Colors:Parse(color)
	if (string.len(color) > 8) then
		-- |cRRGGBB (ie. without alpha)
		return (X4D.Convert:HEX2DEC(color, 3) / 255), (X4D.Convert:HEX2DEC(color, 5) / 255), (X4D.Convert:HEX2DEC(color, 7) / 255), 1
	else
		-- |cAARRGGBB (ie. with alpha)
		return (X4D.Convert:HEX2DEC(color, 5) / 255), (X4D.Convert:HEX2DEC(color, 7) / 255), (X4D.Convert:HEX2DEC(color, 9) / 255), (X4D.Convert:HEX2DEC(color, 3) / 255)
	end
end

function X4D_Colors:Lerp(colorFrom, colorTo, percent)
	if (percent == nil) then
		percent = 50
	end
    if (colorTo == nil) then
        colorTo = "|cFFFFFFFF" -- White
    end
    if (colorFrom == nil) then
        colorFrom = X4D_Colors.SYSTEM
    end
	local factor = (percent / 100)
	local rFrom, gFrom, bFrom, aFrom = X4D_Colors:Parse(colorFrom)
	local rTo, gTo, bTo, aTo = X4D_Colors:Parse(colorTo)
    local r = rFrom + ((rTo - rFrom) * factor)
    local g = gFrom + ((gTo - gFrom) * factor)
    local b = bFrom + ((bTo - bFrom) * factor)
    local a = aFrom + ((aTo - aFrom) * factor)
    if (r > 1) then
        r = 1
    elseif (r < 0) then
        r = 0
    end
    if (g > 1) then
        g = 1
    elseif (g < 0) then
        g = 0
    end
    if (b > 1) then
        b = 1
    elseif (b < 0) then
        b = 0
    end
    if (a > 1) then
        a = 1
    elseif (a < 0) then
        a = 0
    end
	return X4D_Colors:Create(r, g, b)
end

function X4D_Colors:DeriveHighlight(color)
    --if (color == nil) then
    --    print("color is nil")
	--	return color
    --end
	return X4D_Colors:Lerp(color, "|cFFFFFFFF", 50)
end

X4D_Colors.ItemQuality = { }
X4D_Colors.ItemQualityNames = {
	[0] = "Poor",
	[1] = "Common",
	[2] = "Uncommon",
	[3] = "Rare",
	[4] = "Epic",
	[5] = "Legendary",
	[6] = "Artifact",
	[7] = "Heirloom",
	[8] = "Token"   
}
X4D_Colors.NUM_ITEM_QUALITIES = 8 -- this constant was found missing, we have our own version now
for i = 0, X4D_Colors.NUM_ITEM_QUALITIES do
	local r, g, b, hex = GetItemQualityColor(i)
	X4D_Colors.ItemQuality[X4D_Colors.ItemQualityNames[i]] = X4D_Colors:Create(r, g, b, 1)
end

X4D_Colors.Gold = "|cffEED700"
X4D_Colors.Text = "|cff9d9d9d"
X4D_Colors.TextHighlight = "|cfff56c00"
X4D_Colors.Purple = "|cffab44ff"
X4D_Colors.Gold = "|cffffff00"
X4D_Colors.Silver = "|cffc7c7c7"
X4D_Colors.Copper = "|cffffbb33"
