# Mubox Features

This is an attempt to provide an exhaustive list and explaination of features in Mubox.

Mubox does not make any attempt to duplicate the experience, workflow, usabaility nor features of any other tools. We're open to suggestions, but not interesting in duplicating what is already out there. Mubox has always provided a unique experience.

- [System Tray Menu](#system-tray)
- [Game Profiles](#game-profiles)
- [Toggle Input Capture](#input-capture)
- [Keyboard Broadcast](#keyboard-broadcast)
- [Keyboard Multicast](#keyboard-multicast) (and [Round-Robin Keys](#round-robin-keys))
- [Mouse Broadcast](#mouse-broadcast)
- [Mouse Multicast](#mouse-multicast)
- [Control-Alt-Shift Modifiers](#cas-modifiers)
- [Soloboxing](#soloboxing) multiple clients on a single computer.
- [Sand Box](#sandbox) using limited user accounts.
- [Multiboxing](#multiboxing) multiple clients on multiple computers.
- [Fast Client Switching](#fast-client-switching) via ALT-TAB.

<div id="system-tray"></div>
## System Tray Menu

Mubox is accessed from the system tray. If Input Capture is enabled, it may need to be disabled first to interact with the system tray and the tray menu.

<div id="toggle-capture"></div>
Typically you can toggle Input Capture by pressing the NUMLOCK key (sorry for anyone this interferes with, we will make these sort of hotkeys configurable at some point.)

Additionally, it can be toggled by pressing PAUSE/BREAK, the only difference is that PAUSE/BREAK also displays the switcher/server UI (which is a good indicator that Input Capture is disabled, but it can get in the way.)

Whenever input capture is disabled a series of short clicks and beeps are made on the local computer.


<div id="game-profiles"></div>
## Game Profiles

Mubox supports multiple game profiles, which are collections of Clients and Key Settings. This allows users to configure Mubox for multiple games, or multiple configurations for a single game.

Switching the active profile also affects [Fast Client Switching](#fast-client-switching), the Client Switcher only displays and switches between clients configured for the 'Active Profile'. This means, if you have the CPU and Memory, you can Multibox multiple games (like WoW and EvE) at the same time and use Mubox to control which set of clients you're controlling.

You can also use this feature to run multiple teams, such as a healer team and a DPS team, and rely on game profiles to switch between them. This allows you to configure clients and key settings depending on team function or play style, such as when playing with friends.


<div id="keyboard-multicast"></div>
## Keyboard Multicast

Mubox will simultaneously broadcast keystrokes from one keyboard to multiple clients locally or remotely, this is referred to as "Keyboard Multicast".

By default, this feature must be explicitly enabled (either from the System Tray, or from the Switcher UI->Keyboard Settings dialog.)

<div id="round-robin-keys"></div>
Mubox also supports "round-robin" keys, instead of being simultaneously broadcast each press of the key will broadcast the key to only one client at a time and each keypress will send it to the next client in the list.

<div id="keyboard-broadcast"></div>
## Keyboard Broadcast
Even when Keyboard Multicast is not enabled, if the 'active' client is on another machine events will be broadcast to the remote client (as if you were operating the client locally.)


<div id="mouse-multicast"></div>
## Mouse Multicast

Mubox will simultaneously broadcast mouse movement and mouse clicks from one moust to multiple clients locally or remotely, this is referred to as "Mouse Multicast" or "Mouse Clone".

This feature must be enabled from the System Tray first, it is activated using the CAPSLOCK key, and offers both a 'when CAPSLOCK is toggled' mode and 'only when CAPSLOCK is pressed' mode of operation.


<div id="mouse-broadcast"></div>
## Mouse Broadcast
If the 'active' client is on another machine all local Mouse events will be broadcast to the remote client (as if you were operating the mouse locally.)


<div id="cas-modifiers"></div>
## Control-Alt-Shift Modifiers

To support 'follow the leader' (FTL) configurations and 'mutating actions' between a team of clients, Mubox offers "CAS Modifiers".

CAS Modifiers can be configured on a per-Client basis, and are accessible from the Client UI before you launch the game.

The UI allows you to configure the Control-Alt-Shift state of a client when it is NOT the active client (and to also optionally strip all access modifiers when it IS the active client.)


<div id="soloboxing"></div>
## Soloboxing

Mubox allows you to run Multiple clients on the local machine. 

This is best done with clients configured for **Windowed** or **Windowed Fullscreen** mode. If you configure multiple local clents for "Fullscreen Exlusive" mode you may experience graphics hardware delays switching to/from your games.


<div id="sandbox"></div>
## Sand Box

Each Mubox Client and Game is run in a user "sand box", this is a special user account created for each instance of the game to ensure that no two instances of the game share the same execution environment (that is, a game launched by Mubox typically doesn't have API access to your desktop and is unable to see any other running programs.

Games which prompt to be run as an administrator will still have access to your desktop. Do not authorize them if you wish to keep them from detecting Mubox.

This is not a fool-proof anti-detection mechanism, and was primarily implemented to ensure that games were launched with limited user access and rights.


<div id="multiboxing"></div>
## Multiboxing

Mubox allows you to run Multiple clients on remote machines, as well. In this mode the 'server' or 'hub' machine must open a port number (by default, it is port 17520, but can be changed by the user.)

When configuring a Client you have the option of specifying the machine name (or IP) and the port number of a Mubox Hub/Server.

A Mubox Hub/Server is a computer that is running Mubox Switcher, typically this is the computer with a keyboard and mouse you will be using to play.


<div id="fast-client-switching"></div>
## Fast Client Switching

Mubox offers fast client switching, this should NOT be confused with features of other tools which go by the same name.

Fast game switching is a feature of Mubox that replaces the Windows Task Switcher (ALT-TAB) with a custom task switcher when Input Capture is enabled (disabling input capture will once again give you access to Windows Task Switcher.)

When you switch active clients your input is immediately dispatched to the active client, whether or not it is visible, locally running or on a remote machine.

This function makes more sense when soloboxing, or when you're not using [Keyboard Multicast](#keyboard-multicast) and [Mouse Clone](#mouse-clone) features, since the input from your keyboard and mouse must be manually switched to control specific clients.


## Summary

The features and behavior of Mubox was designed to fit a broad set of users that contacted the author looking for more information or new features.

Some of the terminology and behavior may seem obtuse and awkward, most users will not use "all" features. Typically you will configure for a specific set of features, 
for example, I always solobox, I always have multicast enabled, I have a few CAS modifiers configured and rely heavily on mouse clone configured for 'pressed' mode. 
I rely on Fast Client Switching to switch between multiple clients running locally.