local X4D_Equipment = LibStub:NewLibrary("X4D_Equipment", 2000)
if (not X4D_Equipment) then
	return
end
local X4D = LibStub("X4D")
X4D.Equipment = X4D_Equipment

X4D.Equipment.SlotNames =
{
	-- TODO: these need to be verified
	"AmmoSlot",
	"BackSlot",
	"Bag0Slot",
	"Bag1Slot",
	"Bag2Slot",
	"Bag3Slot",
	"ChestSlot",
	"FeetSlot",
	"Finger0Slot",
	"Finger1Slot",
	"HandsSlot",
	"HeadSlot",
	"LegsSlot",
	"MainHandSlot",
	"NeckSlot",
--	"RangedSlot",
	"SecondaryHandSlot",
	"ShirtSlot",
	"ShoulderSlot",
	"TabardSlot",
	"Trinket0Slot",
	"Trinket1Slot",
	"WaistSlot",
	"WristSlot"
};

function X4D_Equipment:Repair()
	local noRepairNecessary = true
	-- custom repair of equipment, and not inventory, inventory slots are ordered by repair priority (e.g. chest before feet)
	local playerMoney = GetMoney()
	local shouldResetCursor = false
	if (CanMerchantRepair()) then
		if (not InRepairMode()) then
			ShowRepairCursor()
			shouldResetCursor = true
		end
		if (InRepairMode()) then
			for _,v in pairs(X4D.Equipment.SlotNames) do
				--X4D.Log:Debug(v)
				local inventoryId = GetInventorySlotInfo(v)
				local itemId = GetInventoryItemID("player", inventoryId)
				if (itemId ~= nil) then					
					local name, link, quality, iLevel, reqLevel, class, subclass, maxStack, equipSlot, texture, vendorPrice, itemClassId, itemSubClassId = GetItemInfo(itemId)
					--X4D.Log:Debug({name, link, quality, iLevel, reqLevel, class, subclass, maxStack, equipSlot, texture, vendorPrice})
					if (link ~= nil) then
						local durability,max = GetInventoryItemDurability(inventoryId)
						--X4D.Log:Debug({playerMoney,quality,durability,max})
						if (durability ~= max) then
							local estimatedRepairCost = 0.010
							if (quality <= 1) then -- common
								estimatedRepairCost = 0.010
							elseif (quality == 2) then -- uncommon
								estimatedRepairCost = 0.020
							elseif (quality == 3) then -- rare
								estimatedRepairCost = 0.025
							elseif (quality >= 4) then -- epic
								estimatedRepairCost = 0.050
							end

							-- TODO determine/implement legendary and trash repair costs
							-- TODO verify repair costs for weapons, armor, and shields/etc
							-- TODO implement the above as a table, instead of hard-coded values
							local iLevelAdjusted = (iLevel - 32.5)
							if (iLevelAdjusted <= 1) then
								iLevelAdjusted = 1
							end
							estimatedRepairCost = (estimatedRepairCost * (max - durability) * iLevelAdjusted) * 100
--X4D.Log:Debug({playerMoney,quality,durability,max,estimatedRepairCost, iLevel})
							
							-- TODO determine/implement faction discount
							
							if (estimatedRepairCost > 0) then
								noRepairNecessary = false
								if (playerMoney >= estimatedRepairCost) then
									PickupInventoryItem(inventoryId)
									X4D.Player:Write("Repaired "..link.." for "..X4D.Convert:ConvertMoneyToString(estimatedRepairCost))
									playerMoney = playerMoney - estimatedRepairCost
								end
							end
						end
					end
				end
			end
		end
		if (shouldResetCursor) then
			HideRepairCursor()
		end
		if (noRepairNecessary) then
			X4D.Player:Write("No repairs were necessary.")
		end
--	else
--		X4D.Log:Debug("NPC Cannot Repair")
	end
	X4D.Persistence.Player.Money = GetMoney()
end

function X4D_Equipment:CompareItemScore(item1, item2, class, log)
	return X4D.Equipment:GetItemScore(item1, class, log) > X4D.Equipment:GetItemScore(item2, class, log)
end

local _auctionItemClassSortKey = nil
function GetItemClassSortKey(itemClassId, itemSubClassId)
	if (_auctionItemClassSortKey == nil) then
		_auctionItemClassSortKey = {
			LE_ITEM_CLASS_RECIPE = "A",
			LE_ITEM_CLASS_MISCELLANEOUS = "B",
			LE_ITEM_CLASS_CONSUMABLE = "C",
			LE_ITEM_CLASS_CONTAINER = "D",
			LE_ITEM_CLASS_ITEM_ENHANCEMENT = "E",
			LE_ITEM_CLASS_GLYPH = "F",
			LE_ITEM_CLASS_GEM = "G",
			LE_ITEM_CLASS_TRADEGOODS = "H",
			LE_ITEM_CLASS_WEAPON = "I",
			LE_ITEM_CLASS_ARMOR = "J",
			LE_ITEM_CLASS_BATTLEPET = "K",
			LE_ITEM_CLASS_QUESTITEM = "L",
		}
	end
	local lval = _auctionItemClassSortKey[itemClassId]
	if (lval == nil) then
		lval = "!"
	end
	if (itemSubClassId == nil) then
		itemSubClassId = "!"
	end
	return lval..itemSubClassId
end

function X4D_Equipment:GetItemSortKey(item)
	local key = ""
	if (item ~= nil) then
		local itemName, itemLink, itemQuality, itemLevel, itemMinLevel, itemType, itemSubType, itemStackCount, itemEquipLoc, itemTexture, itemSellPrice, itemClassId, itemSubClassId = GetItemInfo(item)
		-- sort hearthstone to edge of inventory
		-- sort by quality (move trash to top), type, subtype, quality level, name
		local isTrash = (itemQuality == nil or tostring(itemQuality) == "0")
		if (isTrash) then
			key = key.."!"
		else
			if (itemName ~= nil) then
				if (itemName:find(" Hearthstone") ~= nil) then
					return "Y"..itemName
				elseif (itemName:find("Hearthstone") ~= nil) then
					return "Z"..itemName
				end
			end
		end
		key = key..GetItemClassSortKey(itemClassId, itemSubClassId)
		if (not isTrash) then
			key = key..tostring(itemQuality)
		end
		if (itemName == nil) then
			key = key.."!"
		else
			key = key..strsub(itemName, 1, 3)
		end
	end
	return key
end

function X4D_Equipment:GetItemScore(item, class, log)
	if (item == nil) then
		return -1
	end
	local itemName, itemLink, itemQuality, itemLevel, itemMinLevel, itemType, itemSubType, itemStackCount, itemEquipLoc, itemTexture, itemSellPrice = GetItemInfo(item)
	if (itemLink == nil) then
		return -1
	end

	local itemScoreLog = ""
	
	if (class == nil) then
		class = X4D.Persistence.Player.Class
	end

	local itemScore = 0

	-- adjust score based on equipped chest armor or mainhand weapon type	
	if (itemType == "Armor") then		
		local armorInventoryId = GetInventorySlotInfo("ChestSlot")
		if (armorInventoryId ~= nil) then
			local armorItemId = GetInventoryItemID("player", armorInventoryId)
			if (armorItemId ~= nil) then
				local armorName, armorLink, armorQuality, armorLevel, armorMinLevel, armorType, armorSubType, armorStackCount, armorEquipLoc, armorTexture, armorSellPrice = GetItemInfo(armorItemId)
				if (armorSubType == itemSubType) then
					itemScoreLog = itemScoreLog..armorSubType
					itemScore = itemScore + 100 + string.len(armorSubType)
				end
			end
		end
	elseif (itemType == "Weapon") then	
		local weaponInventoryId = GetInventorySlotInfo("MainHandSlot")
		if (weaponInventoryId ~= nil) then
			local weaponItemId = GetInventoryItemID("player", weaponInventoryId)
			if (weaponItemId ~= nil) then
				local weaponName, weaponLink, weaponQuality, weaponLevel, weaponMinLevel, weaponType, weaponSubType, weaponStackCount, weaponEquipLoc, weaponTexture, weaponSellPrice = GetItemInfo(weaponItemId)
				if (weaponSubType == itemSubType) then
					itemScoreLog = itemScoreLog..weaponSubType
					itemScore = itemScore + 100 + string.len(weaponSubType)
				end
			end
		end
	elseif (itemType == "Container") then
		return 0
	elseif (itemType == "Recipe") then
		return 1
	elseif (itemType == "Trade Goods") then
		return 2
	elseif (itemType == "Gem") then
		return 3
	elseif (itemType == "Quest") then
		return 4
	elseif (itemType == "Consumable") then
		itemScore = itemScore + 9000
	elseif (itemType == "Miscellaneous") then
		itemScore = itemScore + 11000
	end

	-- adjust score based on stat factors for specified class
	local stats = GetItemStats(itemLink)
	if (stats ~= nil) then
		for stat, value in pairs(stats) do
			local statFactors = X4D.StaticConfig.ClassStatFactors[string.upper(class)]
			if (statFactors ~= nil) then
				local statFactor = statFactors[stat]
				if (statFactor ~= nil) then
					itemScore = itemScore + math.floor(value * statFactor)
					itemScoreLog = itemScoreLog.." ".._G[stat]
				end
			end
			itemScore = itemScore + math.ceil(value)
		end
	end
	
	if (itemType ~= nil) then	
		itemScore = itemScore + string.len(itemType)
	end
	if (itemSubType ~= nil) then
		itemScore = itemScore + string.len(itemSubType)
	end
	
	-- and last we adjust for item level, so that higher level items always produce higher scores except for lower level items with exceptional stats
	itemScore = itemScore + (((itemQuality + 1) * 100) + (itemLevel * 12))
	
	local doNotUseList = X4D.StaticConfig.ClassDoNotUse[X4D.Player.Class]
	if (doNotUseList ~= nil) then
		for _,doNotUse in ipairs(doNotUseList) do
			if ((itemType == doNotUse) or (itemSubType == doNotUse)) then
				itemScore = -1 * itemScore
			end
		end
	end

	if (log) then
		itemScoreLog = strtrim(itemScoreLog)
		if (string.len(itemScoreLog) > 0) then
			X4D.Player:Write(itemLink.." "..X4D.Colors.TextHighlight..itemScore..X4D.Colors.Text.." ("..itemScoreLog..")")
		else
			X4D.Player:Write(itemLink.." "..itemScore)
		end
	end
	
	return itemScore
end 
