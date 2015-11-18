local X4D_Equipment = LibStub:NewLibrary("X4D_Equipment", 2000)
if (not X4D_Equipment) then
	return
end
local X4D = LibStub("X4D")
X4D.Equipment = X4D_Equipment

function X4D_Equipment:Repair()
	local noRepairNecessary = true
	-- custom repair of equipment, and not inventory, inventory slots are ordered by repair priority (e.g. chest before feet)
	local playerMoney = GetMoney()
	local shouldResetCursor = false
	if (CanMerchantRepair()) then
		if (InRepairMode() == nil) then
			ShowRepairCursor()
			shouldResetCursor = true
		end
		if (InRepairMode()) then
			for _,v in pairs(X4D.Equipment.SlotNames) do
				local inventoryId = GetInventorySlotInfo(v)
				local itemId = GetInventoryItemID("player", inventoryId)
				if (itemId ~= nil) then
					local name, link, quality, iLevel, reqLevel, class, subclass, maxStack, equipSlot, texture, vendorPrice = GetItemInfo(itemId)
					X4D.Log:Debug({name, link, quality, iLevel, reqLevel, class, subclass, maxStack, equipSlot, texture, vendorPrice})
					if (link ~= nil) then
						local durability,max  = GetInventoryItemDurability(inventoryId)
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

							estimatedRepairCost = (estimatedRepairCost * (max - durability) * (iLevel - 32.5)) * 100
							
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
			--X4D.Player:Write("No repairs were necessary. (this message was broken in patch 401 and may be incorrect)")
		end
	end
	X4D.Persistence.Player.Money = GetMoney()
end

function X4D_Equipment:CompareItemScore(item1, item2, class, log)
	return X4D.Equipment:GetItemScore(item1, class, log) > X4D.Equipment:GetItemScore(item2, class, log)
end

function X4D_Equipment:GetItemSortKey(item)
	if (item == nil) then
		return ""
	end
	local itemName, itemLink, itemQuality, itemLevel, itemMinLevel, itemType, itemSubType, itemStackCount, itemEquipLoc, itemTexture, itemSellPrice = 
		GetItemInfo(item)
	local key = ""
	if (itemName == nil) then
		key = key.."!!!"
	elseif (itemName:find(" Hearthstone") ~= nil) then
		return "000"
	elseif (itemName:find("Hearthstone") ~= nil) then
		return "001"
	else
		key = key..strsub(itemName, 1, 3)
	end
	if (itemType == nil) then
		key = key.."!!!"
	else
		key = key..strsub(itemType, 1, 3)
	end
	if (itemSubType == nil) then
		key = key.."!!!"
	else
		key = key..strsub(itemSubType, 1, 3)
	end
	if (itemQuality == nil) then
		key = key.."0"
	else
		key = key..tostring(itemQuality)
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
