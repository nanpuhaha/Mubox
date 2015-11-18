local X4D_Group = LibStub:NewLibrary("X4D_Group", 2000)
if (not X4D_Group) then
	return
end
local X4D = LibStub("X4D")
X4D.Group = X4D_Group

X4D.Group.Leader = nil

function X4D_Group:Write(message)
	if (X4D_Group.Leader ~= nil) then
		SendChatMessage(message, "WHISPER", nil, X4D_Group.Leader)
	end
end

function X4D_Group:FollowCommandHandler()
	X4D_Group:RaiseEvent("SET_LEADER")
end

function X4D_Group:GroupNameCommandHandler(groupName)
	local oldGroupName = X4D.Persistence.Group.Name
	X4D.Persistence.Group.Name = groupName
	X4D.Player:Write(X4D.Name.." "..X4D.Version" group changed from \""..X4D.Colors.TextHighlight..oldGroupName..X4D.Colors.Text.."\" to \""..X4D.Colors.TextHighlight..X4D.Persistence.Group.Name..X4D.Colors.Text.."\"")
end

function X4D_Group:FollowGroupLeader()
	if ((not X4D.Player.IsFollowing) and (X4D_Group.Leader ~= nil)) then
		if ((UnitExists(X4D_Group.Leader) ~= nil) and (UnitInRange(X4D_Group.Leader) ~= nil)) then
			FollowUnit(X4D_Group.Leader)
			X4D.HUD:Write("Following "..X4D_Group.Leader)
		else
			X4D_Group:Write(X4D_Group.Leader.." does not exist, or is too far to follow.")
		end
	end
end

function X4D_Group:OnSetLeader(unitName)
	if (unitName == X4D.Player.Name) then
		unitName = nil
	end
	if ((unitName == nil) or UnitInParty(unitName) or UnitInRaid(unitName) or UnitInBattleground(unitName)) then
		X4D_Group.Leader = unitName
		if (unitName ~= nil) then		
			X4D.Macros:MacroizeInvite(unitName)
			if ((IsPartyLeader() or IsRealPartyLeader() or IsRaidLeader() or IsRealRaidLeader())) then
				PromoteToLeader(unitName, true)
			end
		end
		X4D.Player.IsFollowing = false
		X4D_Group:FollowGroupLeader()
	end
end

function X4D_Group:OnSetTarget(unitName)
	if (unitName == X4D.Player.Name) then
		unitName = nil
	end
	if ((unitName == nil) or UnitInParty(unitName) or UnitInRaid(unitName) or UnitInBattleground(unitName)) then
		X4D.Macros:MacroizeTarget(unitName)
	end
	X4D.Macros:MacroizeFocus(X4D_Group.Leader)
end

function X4D_Group:RaiseEvent(eventName)
	local target = X4D_Group.GetTargetType("player")
	SendAddonMessage("EXBLZZ", X4D.Persistence.Group.Name.." "..eventName.." "..X4D.Player.Name, target)
end

function X4D_Group:SetTarget()
	X4D_Group.RaiseEvent("SET_TARGET")
end

function X4D_Group:MassInvite(unitName)
	X4D.Macros:MacroizeInvite(unitName)
end

function X4D_Group:GetTargetType(unitName)
	if (UnitInBattleground(unitName)) then
		return "BATTLEGROUND"
	elseif (GetNumRaidMembers() > 0) then
		return "RAID"
	else
		return "PARTY"
	end
end

