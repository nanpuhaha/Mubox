local X4D_Convert = LibStub:NewLibrary("X4D_Convert", 2000)
if (not X4D_Convert) then
	return
end
local X4D = LibStub("X4D")
X4D.Convert = X4D_Convert

function X4D_Convert:HEX2DEC(input, offset)
	if (offset == nil) then
		offset = 0
	end
	local h = tonumber(input:sub(offset, offset), 16)
	if (h == nil) then
		h = 0
	end
	local l = tonumber(input:sub(offset + 1, offset + 1), 16)
	if (l == nil) then
		l = 0
	end
	return (h * 16) + l
end

function X4D_Convert:DEC2HEX(input)
	if (input == nil) then
		input = 255
	end
	local h = (input / 16)
	local l = (input - (h * 16))
	return string.format("%x%x", h, l)
end

function X4D_Convert:ConvertMoneyToString(input)
	local gold = math.floor(input / (100 * 100))
	local silver = math.floor((input / (100)) - (gold * 100))
	local copper = math.floor(input - (silver * 100) - (gold * 100 * 100))
	local moneyString = ""
	if (gold > 0) then
		moneyString = moneyString..X4D.Colors.Gold..gold.."|TInterface\\MoneyFrame\\UI-GoldIcon:14:14:2:0|t "
	end
	if (silver > 0) then
		moneyString = moneyString..X4D.Colors.Silver..silver.."|TInterface\\MoneyFrame\\UI-SilverIcon:14:14:2:0|t "
	end
	if (copper > 0) then
		moneyString = moneyString..X4D.Colors.Copper..copper.."|TInterface\\MoneyFrame\\UI-CopperIcon:14:14:2:0|t "
	end
	return moneyString
end
