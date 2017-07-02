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
end)

function X4D_Inventory:Sort()--resumeBagId, resumeSlotId)
	-- NOP: removed, what we really want is a virtualized inventory -- plenty of great bag/inventory UI addons already -- REMOVED!
end
