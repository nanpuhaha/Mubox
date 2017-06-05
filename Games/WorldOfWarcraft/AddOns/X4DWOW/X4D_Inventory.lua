local X4D_Inventory = LibStub:NewLibrary("X4D_Inventory", 2000)
if (not X4D_Inventory) then
	return
end
local X4D = LibStub("X4D")
X4D.Inventory = X4D_Inventory

function X4D_Inventory:AutoVendor()
	local noSalesPossible = true
	if (X4D.Persistence.MaxQualityAutoVendor ~= nil) then
		local playerBeginMoney = GetMoney()
		for x = 0, 4 do 
			for y = 1, GetContainerNumSlots(x) do
				local link = GetContainerItemLink(x,y)
				if (link) then
					local name, link, quality, itemLevel, itemMinLevel, itemType, itemSubType, itemStackCount, itemEquipLoc, itemTexture, itemSellPrice = 
						GetItemInfo(link)
					local vendorOkay = (quality <= X4D.Persistence.MaxQualityAutoVendor)
					if (X4D.Persistence.MaxItemLevelAutoVendor ~= nil) then
						if (itemLevel > X4D.Persistence.MaxItemLevelAutoVendor) then
							vendorOkay = false
						end
					end
				
					for _,itemTypeNoSales in pairs(X4D.StaticConfig.VendorDoNotSell) do
						if ((itemType == itemTypeNoSales) or (itemSubType == itemTypeNoSales)) then
							vendorOkay = false
						end 
					end

					if (vendorOkay) then
						-- TODO do not sell soulbound items
						-- TODO do not sell instance loot with an outstanding trade duration
						-- TODO do not sell crafting items for own professions
						PickupContainerItem(x,y)
						PickupMerchantItem()
						X4D.Player:Write("Sold "..link)
						noSalesPossible = false
					end
				end		
			end
		end
		local playerEndMoney = GetMoney() -- NOTE: this hasn't worked for some time due to an API change
		if (playerBeginMoney < playerEndMoney) then
			X4D.Player:Write("Sales Total: "..X4D.Convert:ConvertMoneyToString(playerEndMoney - playerBeginMoney))
		end	
	end
	X4D.Persistence.Player.Money = GetMoney()
	if (noSalesPossible) then
		--X4D.Player:Write("Nothing was sold.")
	else
		X4D_Inventory.ShouldSort = true
	end
end

local _isMerchantVisible = false
X4D:RegisterForEvent("X4D_Inventory", "MERCHANT_SHOW", function (...)
	_isMerchantVisible = true
end)
X4D:RegisterForEvent("X4D_Inventory", "MERCHANT_CLOSED", function (...)
	_isMerchantVisible = false
	X4D_Inventory:Sort()
end)


function X4D_Inventory:Sort(resumeBagId, resumeSlotId)
	if (_isMerchantVisible) then
		-- TODO: do not sort if vendor active, instead perform when vendor closed
		-- NOTE: this short-circuit is required to prevent breaking UI engine (ie. perma-locked/grayed items in bags)
		return
	end

	if (IsShiftKeyDown() or (not X4D.Persistence.IsEnabled)) then
		-- user short circuit
		X4D_Inventory.ShouldSort = true
		return
	end

	-- if player has an item picked up, do not attempt to sort bags
	if (CursorHasItem()) then
		X4D_Inventory.ShouldSort = true
		return
	end

	if (X4D_Inventory.IsBusySorting) then
		X4D_Inventory.ShouldSort = true
		return
	end
	X4D_Inventory.IsBusySorting = true

	if (resumeBagId == nil or tonumber(resumeBagId) ~= resumeBagId) then
		resumeBagId = 4
	end
	if (resumeSlotId == nil or tonumber(resumeSlotId) ~= resumeSlotId) then
		resumeSlotId = 1
	end
	X4D_Inventory.ShouldSort = false
	for leftBag = resumeBagId, 0, -1 do 
		local leftBagNumSlots = GetContainerNumSlots(leftBag)
		for leftSlot = resumeSlotId, leftBagNumSlots do
			-- resolve left and right targets
			for rightBag = leftBag, 0, -1 do
				local L_rightSlot = leftSlot
				if (rightBag ~= leftBag) then
					L_rightSlot = 1
				end
				local rightBagNumSlots = GetContainerNumSlots(rightBag)
				for rightSlot = L_rightSlot, rightBagNumSlots do
					if (rightSlot > rightBagNumSlots) then
						rightBag = rightBag - 1
						rightSlot = 1--GetContainerNumSlots(rightBag)
						if (rightBag > -1) then
							rightBagNumSlots = GetContainerNumSlots(rightBag)
						end
					end
					-- compare targets and swap if possible
					if (rightBag > -1) then
						local _, leftSlotCount, leftLocked, _, _, _, leftLink = GetContainerItemInfo(leftBag, leftSlot)
						if (not leftLocked and leftLink ~= nil) then
							local _, rightSlotCount, rightLocked, _, _, _, rightLink = GetContainerItemInfo(rightBag, rightSlot)
							if ((not rightLocked) and rightLink == nil and X4D.Persistence.ShouldCompressInventory) then
								PickupContainerItem(leftBag, leftSlot)
								PickupContainerItem(rightBag, rightSlot)
							elseif (rightLocked) then
								rightSlot = rightSlot + 1
								X4D_Inventory.ShouldSort = true
							else
								-- derive scores
								if (leftSlotCount == nil) then
									leftSlotCount = 0
								end
								local leftScore = X4D.Equipment:GetItemSortKey(leftLink, nil, false) --.. leftSlotCount
								if (rightSlotCount == nil) then
									rightSlotCount = 0
								end
								local rightScore = X4D.Equipment:GetItemSortKey(rightLink, nil, false) --.. rightSlotCount
						
								-- apply derived scores
								if (leftScore > rightScore) then
									--X4D.HUD:Debug({"L>R",leftScore,rightScore,leftBag,leftSlot,rightBag,rightSlot})
									PickupContainerItem(leftBag, leftSlot)
									PickupContainerItem(rightBag, rightSlot)
									X4D_Inventory.ShouldSort = true
								end
							end
						end
					end
				end
			end
		end
	end
	X4D_Inventory.IsBusySorting = false
end
