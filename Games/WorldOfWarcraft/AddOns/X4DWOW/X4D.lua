local X4D = LibStub:NewLibrary("X4D", 2000)
if (not X4D) then
	print("X4D Add-on has failed to load")
    return
end

X4D.Name = "X4D"
X4D.Version = "2.1"

-- TODO: localize names

X4D.StaticConfig = 
	{
		VendorDoNotSell =
		{
			"Quest",
			"Hearthstone",
		},
		ClassDoNotUse =
		{
			-- 'Do Not Use' lists to determine the item subtypes to ignore during quest reward selection
			DEATHKNIGHT =
			{
				"Cloth",
				"Leather",
				"Staff",
				"Mace",
			},
			DRUID = 
			{			
			},
			HUNTER = 
			{ 
			},
			MAGE = 
			{ 
				"Mail",
				"Leather",
				"Shields",
				"Plate", 
				"One-Handed Axes",
				"Two-Handed Axes",
				"Bows",
				"Guns",
				"Polearms",
				"Crossbows",
			},
			PALADIN = 
			{ 
			},
			PRIEST = 
			{
				"Mail",
				"Leather",
				"Shields",
				"Plate", 
				"One-Handed Axes",
				"Two-Handed Axes",
				"Bows",
				"Guns",
				"Polearms",
				"Crossbows",
			},
			ROGUE = 
			{ 
			},
			SHAMAN = 
			{ 
			},
			WARLOCK = 
			{
				"Mail",
				"Leather",
				"Shields",
				"Plate", 
				"One-Handed Axes",
				"Two-Handed Axes",
				"Bows",
				"Guns",
				"Polearms",
				"Crossbows",
			},
			WARRIOR =
			{
			}
		},
		ClassStatFactors =
		{
			-- 'Stat Factors' determine which item stats are used to improve an item score. if not defined a stat will have no effect on item score.
			-- TODO: specialization-specific stat factor tables, Fx: Affliction vs. Destruction
			DEATHKNIGHT =
			{
				ITEM_MOD_HASTE_MELEE_RATING_SHORT = 33,
				ITEM_MOD_POWER_REGEN6_SHORT = 20,
				ITEM_MOD_STAMINA_SHORT = 10,
				ITEM_MOD_STRENGTH_SHORT = 10
			},
			DRUID = 
			{ 
			},
			HUNTER = 
			{ 
			},
			MAGE = 
			{ 
				ITEM_MOD_SPELL_PENETRATION_SHORT = 1.75,
				ITEM_MOD_SPELL_POWER_SHORT = 1,
				ITEM_MOD_POWER_REGEN0_SHORT = 33,
				ITEM_MOD_INTELLECT_SHORT = 3,
				ITEM_MOD_STAMINA_SHORT = 1.5,
				ITEM_MOD_SPIRIT_SHORT  = 1,
				ITEM_MOD_MANA_SHORT = 1
			},
			PALADIN = 
			{ 
			},
			PRIEST = 
			{ 
				ITEM_MOD_SPELL_POWER_SHORT = 2,
				ITEM_MOD_POWER_REGEN0_SHORT = 33,
				ITEM_MOD_MANA_SHORT = 2,
				ITEM_MOD_SPELL_PENETRATION_SHORT = 0.66,
				ITEM_MOD_STAMINA_SHORT = 1.5,
				ITEM_MOD_INTELLECT_SHORT = 3,
				ITEM_MOD_SPIRIT_SHORT  = 3,
				ITEM_MOD_STRENGTH_SHORT = 0.1,
				ITEM_MOD_AGILITY_SHORT = 0.1
			},
			ROGUE = 
			{ 
			},
			SHAMAN = 
			{ 
				ITEM_MOD_SPELL_POWER_SHORT = 1,
				ITEM_MOD_POWER_REGEN0_SHORT = 20,
				ITEM_MOD_MANA_SHORT = 0.5,
				ITEM_MOD_SPELL_PENETRATION_SHORT = 0.66,
				ITEM_MOD_STAMINA_SHORT = 2.5,
				ITEM_MOD_INTELLECT_SHORT = 2,
				ITEM_MOD_SPIRIT_SHORT  = 2,
				ITEM_MOD_STRENGTH_SHORT = 1.5,
				ITEM_MOD_AGILITY_SHORT = 1.5
			},
			WARLOCK = 
			{
				ITEM_MOD_SPELL_PENETRATION_SHORT = 1.75,
				ITEM_MOD_SPELL_POWER_SHORT = 1,
				ITEM_MOD_POWER_REGEN0_SHORT = 33,
				ITEM_MOD_INTELLECT_SHORT = 3,
				ITEM_MOD_STAMINA_SHORT = 1.5,
				ITEM_MOD_SPIRIT_SHORT  = 1,
				ITEM_MOD_MANA_SHORT = 1
			},
			WARRIOR =
			{
				ITEM_MOD_STRENGTH_SHORT = 3,
				ITEM_MOD_STAMINA_SHORT = 3,
				ITEM_MOD_AGILITY_SHORT = 3,
				ITEM_MOD_POWER_REGEN1_SHORT = 2
			}
		}
	}

X4D._eventHandlers = {}

--print("X4D Loading..")
X4D.Frame = CreateFrame("FRAME", "X4D");

function X4D:RegisterForEvent(scope, eventName, eventHandler)
	--print("RegisterForEvent(" .. scope .. "," .. eventName .. ",handler)")
	local scoped = self._eventHandlers[scope]
	if (scoped == nil) then
		scoped = {}
		self._eventHandlers[scope] = scoped
	end
	local previousHandler = scoped[eventName]
	scoped[eventName] = eventHandler
	if (previousHandler == nil) then
		X4D.Frame:RegisterEvent(eventName)
	end
	return previousHandler
end

function X4D:UnregisterEvent(scope, eventName, eventHandler)
	local scoped = self._eventHandlers[scope]
	if (scoped ~= nil) then
		local previousHandler = scoped[eventName]
		X4D.Frame:UnregisterEvent(eventName)
		scoped[eventName] = nil
		return previousHandler
	end
end

local _playerInfoEvictionTime = 0
local _playerInfo = nil

function X4D:OnEvent(...)
	-- throttle api calls
	local startTime = debugprofilestop()
	if (_playerInfoEvictionTime <= startTime) then
		_playerInfoEvictionTime = startTime + 1000
		_playerInfo = { CastingInfo = { UnitCastingInfo("player"), nil }, IsAffectingCombat = UnitAffectingCombat("player") }
	end
	--X4D.Log:Debug({...})

	if (IsShiftKeyDown() or (X4D.Persistence ~= nil and (not X4D.Persistence.IsEnabled)) or (_playerInfo.IsAffectingCombat or _playerInfo.CastingInfo[0] ~= nil)) then
		-- user can press and hold shift key to bypass some of X4D's behaviors, such as not talking to quest NPCs.. 
		-- this is meant to work in conjunction with pressing shift to not auto-loot
		return false
	end
	local arg0, event, addon = ...
	local wasHandled = false
    for _,scoped in pairs(self._eventHandlers) do
        for eventName,eventHandler in pairs(scoped) do
            if (eventHandler ~= nil and eventName == event) then
				eventHandler(event, ...)
				wasHandled = true
			--else
				--X4D.Log:Information(...)
            end
        end
    end
	if (not wasHandled) then
		if (event ~= "ADDON_LOADED") then
			X4D.Log:Debug({"X4D Unexpected Event: ", ...})
		end
	end
	return wasHandled
end

function X4D:OnUpdate(elapsed)
	-- TODO: reimplement using X4D.Async:CreateTimer() instead
	if ((not X4D.Persistence) or (not X4D.Persistence.IsEnabled)) then
		return
	end

	local timeNow = time()
	if (X4D.TimeOfNextUpdate == nil) then
		X4D.TimeOfNextUpdate = timeNow
	end
	if (X4D.TimeOfNextUpdate >= timeNow) then
		return
	end
	X4D.TimeOfNextUpdate = time() + 5 -- NOTE: we only allow updates once every 5 seconds

	-- note: we only begin one process per tick, e.g. sorting bags, or queuing for a bg, etc. these are in priority. thus, bag sorts will suppress queuing for a bg.
	if (X4D.Inventory.ShouldSort) then
		X4D.Inventory:Sort(4, 1)
	else
		X4D.PvP:TryQueuePvP()
	end
end

X4D:RegisterForEvent(X4D.Name, "ADDON_LOADED", function (...)
	local arg0, arg1, event, name = ...
	if (name ~= "X4DWOW") then
		return
	end
	if (X4DPersistence ~= nil) then
		X4D.Persistence = X4DPersistence
		if (X4D.Persistence.PvP == nil) then
			X4D.Persistence.PvP =
			{
				Battleground =
				{
					AutoQueue = nil
				},
				Arena = 
				{
					AutoQueue = nil
				}
			}
		end
	else
		X4D.Persistence = 
		{
			IsEnabled = false,
			Player =
			{
				XP = 
				{
					Total = 0,
					Needed = 0
				},
				Faction = "",
				Level = 0,
				Name = "",
				Class = "",
				Realm = "",
				GuildName = "",
				Money = 0
			},
			Group =
			{
				Name = "DEFAULT"
			},
			PvP =
			{
				Battleground =
				{
					AutoQueue = nil
				},
				Arena = 
				{
					AutoQueue = nil
				}
			},
			MaxQualityAutoVendor = 0,
			InviteList = nil
		}
	end
	X4D.Persistence.Player.Class = UnitClass("player")
	X4D.Persistence.Player.Level = UnitLevel("player")
end)

X4D:RegisterForEvent(X4D.Name, "BAG_UPDATE", function (self, event, ...)
	X4D.Inventory:Sort(4, 1)
end)

X4D:RegisterForEvent(X4D.Name, "ITEM_LOCK_CHANGED", function (self, event, ...)
	local arg1, arg2, arg3, arg4, arg5 = ...
	local ok, _, _, leftLocked = pcall(GetContainerItemInfo, arg1, arg2)
	if (ok) then
		if (not leftLocked) then
			X4D.Inventory:Sort(arg1, arg2)
		else
			X4D.Inventory:Sort(4, 1)
		end
	end
end)

X4D:RegisterForEvent(X4D.Name, "PARTY_LOOT_METHOD_CHANGED", function (self, event, ...)
	X4D.Player:Write(X4D.Name.." "..X4D.Version.." group is \""..X4D.Colors.TextHighlight..X4D.Persistence.Group.Name..X4D.Colors.Text.."\"")
	X4D.Macros:MacroizeFollow()
	X4D.Macros:MacroizeTarget(nil)
	X4D.Macros:MacroizeFocus(nil)
end)

X4D:RegisterForEvent(X4D.Name, "PLAYER_TARGET_CHANGED", function (self, event, ...)
	local arg1, arg2, arg3, arg4, arg5 = ...
	if (arg1 == "up") then
		if (UnitExists("target")) then
			X4D.Group.SetTarget()
		end
	end
end)

X4D:RegisterForEvent(X4D.Name, "CHAT_MSG_ADDON", function (self, event, ...)
	local arg1, arg2, arg3, arg4, arg5 = ...
	local args = { }
	local idx = 0
	local subEvent = nil
	for param in string.gmatch(arg2, "[^%s]+") do
		if (idx == 0) then
			if (param ~= X4D.Persistence.Group.Name) then
				return
			end
		elseif (idx == 1) then
			subEvent = param
		else
			table.insert(args, param)
		end
		idx = idx + 1
	end
	if (subEvent == "SET_LEADER") then
		X4D.Group:OnSetTarget(args[1])
		X4D.Group:OnSetLeader(args[1])
	elseif (subEvent == "SET_TARGET") then
		X4D.Group:OnSetTarget(args[1])
	end
end)

X4D:RegisterForEvent(X4D.Name, "PLAYER_XP_UPDATE", function (self, event, ...)
	OnPlayerXPUpdate()
end)

X4D:RegisterForEvent(X4D.Name, "AUTOFOLLOW_BEGIN", function (self, event, ...)
	OnAutoFollowBegin()
end)

X4D:RegisterForEvent(X4D.Name, "AUTOFOLLOW_END", function (self, event, ...)
	OnAutoFollowEnd()
end)

X4D:RegisterForEvent(X4D.Name, "PLAYER_LEAVE_COMBAT", function (self, event, ...)
	OnPlayerLeaveCombat()
end)

X4D:RegisterForEvent(X4D.Name, "PLAYER_REGEN_ENABLED", function (self, event, ...)
	OnPlayerLeaveCombat()
end)

X4D:RegisterForEvent(X4D.Name, "UNIT_SPELLCAST_CHANNEL_STOP", function (self, event, ...)
	OnUnitSpellcastChannelStop()
end)

X4D:RegisterForEvent(X4D.Name, "LOOT_CLOSED", function (self, event, ...)
	OnLootClosed()
end)

X4D:RegisterForEvent(X4D.Name, "ZONE_CHANGED_NEW_AREA", function (self, event, ...)
	OnZoneChangedNewArea()
end)

X4D:RegisterForEvent(X4D.Name, "CHAT_MSG_RAID", function (self, event, ...)
	local arg1, arg2, arg3, arg4, arg5 = ...
	-- accept follow invitations from party/raid members
	if (string.find(string.lower(arg1), "follow me", 1, true) ~= nil) then
		X4D.Group:OnSetLeader(arg2)
	end
end)

X4D:RegisterForEvent(X4D.Name, "CHAT_MSG_PARTY", function (self, event, ...)
	local arg1, arg2, arg3, arg4, arg5 = ...
	-- accept follow invitations from party/raid members
	if (string.find(string.lower(arg1), "follow me", 1, true) ~= nil) then
		X4D.Group:OnSetLeader(arg2)
	end
end)

X4D:RegisterForEvent(X4D.Name, "CHAT_MSG_WHISPER", function (self, event, ...)
	local arg1, arg2, arg3, arg4, arg5 = ...
	-- accept follow invitations from party/raid members
	if (string.find(string.lower(arg1), "follow me", 1, true) ~= nil) then
		X4D.Group:OnSetLeader(arg2)
	end
end)

X4D:RegisterForEvent(X4D.Name, "UI_ERROR_MESSAGE", function (self, event, ...)
	local arg1, arg2, arg3, arg4, arg5 = ...
	if (X4D.Group.Leader ~= nil) then
		if ((arg1 == ERR_AUTOFOLLOW_TOO_FAR) or (arg1 == ERR_UNIT_NOT_FOUND)) then
			X4D.Group:Write(arg1)
		end
	end
end)

X4D:RegisterForEvent(X4D.Name, "PLAYER_LOGOUT", function (...)
	OnPlayerLogout()
end)

X4D:RegisterForEvent(X4D.Name, "PLAYER_CONTROL_GAINED", function (...)
	OnPlayerControlGained()
end)

X4D:RegisterForEvent(X4D.Name, "GOSSIP_SHOW", function (...)
	OnGossipShow()
end)
X4D:RegisterForEvent(X4D.Name, "QUEST_GREETING", function (...)
	OnQuestGreeting()
end)
X4D:RegisterForEvent(X4D.Name, "QUEST_DETAIL", function (...)
	OnQuestDetail()
end)
X4D:RegisterForEvent(X4D.Name, "QUEST_ACCEPT_CONFIRM", function (...)
	ConfirmAcceptQuest()
end)
X4D:RegisterForEvent(X4D.Name, "QUEST_PROGRESS", function (...)
	OnQuestProgress()
end)

X4D:RegisterForEvent(X4D.Name, "QUEST_COMPLETE", function (...)
	OnQuestComplete()
end)

X4D:RegisterForEvent(X4D.Name, "QUEST_FINISHED", function (...)
	OnQuestFinished()
end)

X4D:RegisterForEvent(X4D.Name, "MERCHANT_SHOW", function (...)
	OnMerchantShow()
end)

X4D:RegisterForEvent(X4D.Name, "PARTY_INVITE_REQUEST", function (...)
	OnPartyInvite()
end)

X4D:RegisterForEvent(X4D.Name, "PARTY_MEMBERS_CHANGED", function (...)
	OnPartyMembersChanged()
end)

-- TODO: AutoTrade function to trade profession materials, negotiated via addon chat

function OnGossipShow()
	local L_unitName = UnitName("npc")
	if ((X4D.Player.NPC.Name ~= L_unitName) or (X4D.Player.NPC.ActiveCount ~= GetNumGossipActiveQuests()) or (X4D.Player.NPC.AvailableCount ~= GetNumGossipAvailableQuests())) then
		X4D.Player.NPC.Name = L_unitName
		X4D.Player.NPC.ActiveCount = GetNumGossipActiveQuests()
		X4D.Player.NPC.ActiveIndex = 0
		X4D.Player.NPC.AvailableCount = GetNumGossipAvailableQuests()
		X4D.Player.NPC.AvailableIndex = 0						
		X4D.Player.NPC.TrainerCount = GetNumTrainerServices()
		X4D.Player.NPC.TrainerIndex = 0						
		X4D.Player.NPC.OptionCount = GetNumGossipOptions()
		X4D.Player.NPC.OptionIndex = 0						
	end
	if (X4D.Player.NPC.ActiveIndex < X4D.Player.NPC.ActiveCount) then
		X4D.Player.NPC.ActiveIndex = X4D.Player.NPC.ActiveIndex + 1
		SelectGossipActiveQuest(X4D.Player.NPC.ActiveIndex)
	elseif (X4D.Player.NPC.AvailableIndex < X4D.Player.NPC.AvailableCount) then
		X4D.Player.NPC.AvailableIndex = X4D.Player.NPC.AvailableIndex + 1
		SelectGossipAvailableQuest(X4D.Player.NPC.AvailableIndex)
	elseif (X4D.Player.NPC.TrainerIndex < X4D.Player.NPC.TrainerCount) then
		X4D.Player.NPC.TrainerIndex = X4D.Player.NPC.TrainerIndex + 1
		OnGossipShow() -- iterate services
	elseif (X4D.Player.NPC.OptionCount == 0) then
		-- do not close trainer frames that open as gossip frames
		if (X4D.Player.NPC.TrainerCount == 0) then
			CloseGossip()
			X4D.Group:FollowGroupLeader()
		end
	end
end

local _npcCacheEvictTime = 0

function OnQuestGreeting()
	local timeNow = debugprofilestop()
	local L_unitName = UnitName("npc")
	if (timeNow >= _npcCacheEvictTime or ((X4D.Player.NPC.Name ~= L_unitName) or (X4D.Player.NPC.ActiveCount ~= GetNumActiveQuests()) or (X4D.Player.NPC.AvailableCount ~= GetNumAvailableQuests()))) then
		_npcCacheEvictTime = timeNow + 5000 -- after 5 seconds we re-evaluate the target NPC, this is less expensive than tracking quests
		X4D.Player.NPC.Name = L_unitName
		X4D.Player.NPC.ActiveCount = GetNumActiveQuests()
		X4D.Player.NPC.ActiveIndex = 0
		X4D.Player.NPC.AvailableCount = GetNumAvailableQuests()
		X4D.Player.NPC.AvailableIndex = 0						
	end
	if (X4D.Player.NPC.ActiveIndex < X4D.Player.NPC.ActiveCount) then
		X4D.Player.NPC.ActiveIndex = X4D.Player.NPC.ActiveIndex + 1
		SelectActiveQuest(X4D.Player.NPC.ActiveIndex)
	elseif (X4D.Player.NPC.AvailableIndex < X4D.Player.NPC.AvailableCount) then
		X4D.Player.NPC.AvailableIndex = X4D.Player.NPC.AvailableIndex + 1
		SelectAvailableQuest(X4D.Player.NPC.AvailableIndex)
	elseif (X4D.Player.NPC.OptionCount == 0) then
		CloseQuest()
		X4D.Group:FollowGroupLeader()
	end
end

function OnQuestComplete()
	--X4D.Log:Debug("OnQuestComplete")
    X4D.Async:CreateTimer(OnQuestCompleteAsync, 250, { }):Start(nil,nil,"OnQuestComplete")
end

function OnQuestCompleteAsync(timer, state)
	--X4D.Log:Debug("OnQuestCompleteAsync")
    timer:Stop()
	local num_choices = GetNumQuestChoices()
	if (num_choices == nil) then
		--X4D.Log:Debug("nil choices")
		GetQuestReward(nil)
	elseif (num_choices <= 1) then
		--X4D.Log:Debug("<=1 choices")
		GetQuestReward(1)
	elseif (num_choices > 1) then
		--X4D.Log:Debug(">=2 choices")
		local preferredChoiceIndex = 0
		for choiceIndex=1,num_choices do
			local itemLink = GetQuestItemLink("choice", choiceIndex)
			if (itemLink ~= nil) then
				--X4D.Log:Debug("Choice #"..choiceIndex..": "..itemLink)
				local name, link, quality, iLevel, reqLevel, class, subclass, maxStack, equipSlot, texture, vendorPrice = GetItemInfo(itemLink)
				local itemScore = X4D.Equipment:GetItemScore(link, nil, true)
				if (preferredChoiceIndex == 0) then
					preferredChoiceIndex = choiceIndex
				else				
					local preferredName, preferredLink, preferredQuality, preferredILevel, preferredReqLevel, preferredClass, preferredSubclass, preferredMaxStack, preferredEquipSlot, preferredTexture, preferredVendorPrice = 
						GetItemInfo(GetQuestItemLink("choice", preferredChoiceIndex))
					if (preferredName ~= nil) then
						if (X4D.Equipment:CompareItemScore(link, preferredLink)) then
							preferredChoiceIndex = choiceIndex
						end
					end					
				end
			else
				-- NOTE: this should not happen anymore
				--X4D.Log:Debug("Choice #"..choiceIndex..": nil??? retrying!")
				--OnQuestComplete()
				return
			end
		end
		if (preferredChoiceIndex ~= 0) then
			local rewardLink = GetQuestItemLink("choice", preferredChoiceIndex)
			GetQuestReward(preferredChoiceIndex)			
		end
	end
end

function OnQuestDetail()
	--X4D.Log:Debug("OnQuestDetail")
	AcceptQuest()
end

function OnQuestProgress()
	if (IsQuestCompletable()) then
		--X4D.Log:Debug("OnQuestProgress - Completable")
		CompleteQuest()
	else
		--X4D.Log:Debug("OnQuestProgress - NOT Completable - skipping")
		DeclineQuest()
	end
end

function OnQuestFinished()
	-- NOP
end

function OnMerchantShow()
	X4D.Inventory:AutoVendor()
	X4D.Equipment:Repair()
	X4D.Group:FollowGroupLeader()
end

function OnPartyInvite()
	AcceptGroup()
end

function OnPartyMembersChanged()
	StaticPopup_Hide("PARTY_INVITE")
end

function OnPlayerLogout()
	X4D.Persistence.Player.Name = UnitName("player")
	X4D.Persistence.Player.Realm = GetRealmName("player")
	X4D.Persistence.Player.Faction = UnitFactionGroup("player")
	X4D.Persistence.Player.Class = UnitClass("player")
	X4D.Persistence.Player.Level = UnitLevel("player")
	X4D.Persistence.Player.XP.Needed = UnitXPMax("player")
	X4D.Persistence.Player.XP.Total = UnitXP("player")
	if (GetGuildInfo("player") == nil) then
		X4D.Persistence.Player.GuildName = ""
	else
		X4D.Persistence.Player.GuildName = GetGuildInfo("player")
	end
	X4DPersistence = X4D.Persistence
end

function OnPlayerXPUpdate()
	if (UnitXP("player") < X4D.Persistence.Player.XP.Total) then
		X4D.Persistence.Player.XP.Needed = UnitXPMax("player")
	end
	if (UnitLevel("player") > X4D.Persistence.Player.Level) then
		X4D.Persistence.Player.Level = UnitLevel("player")
		--X4D.Player:Write("Level Up! ("..X4D.Colors.TextHighlight..X4D.Persistence.Player.Level..X4D.Colors.Text..")")
	end
	X4D.Persistence.Player.XP.Total = UnitXP("player")
end

function OnPlayerControlGained()
	X4D.Group:FollowGroupLeader()
end

function OnPlayerLeaveCombat()
	-- X4D.Group:FollowGroupLeader()
end

function OnUnitSpellcastChannelStop()
	-- X4D.Group:FollowGroupLeader()
end

function OnZoneChangedNewArea()
	X4D.Group:FollowGroupLeader()
	X4D.Player.IsFollowing = false
end

function OnLootClosed()
	X4D.Group:FollowGroupLeader()
end

function OnAutoFollowBegin()
	X4D.Player.IsFollowing = true
end

function OnAutoFollowEnd()
	X4D.Player.IsFollowing = false
end
