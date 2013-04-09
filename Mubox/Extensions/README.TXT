

!!!
!!! IMPLEMENTATION IS STILL SUBJECT TO CHANGE
!!! IMPLEMENTATION IS STILL SUBJECT TO CHANGE
!!! IMPLEMENTATION IS STILL SUBJECT TO CHANGE
!!!
!!! "MUBOX EXTENSIBILITY" IS 'PRE-RELEASE' AND 'UNSTABLE' WORK
!!!

!!! Planned 'Official' Extensions Include: PIP, Console, AntiAway, and Macros.

# Mubox now supports 'Extensions':

* Extensions can be Loaded and Unloaded.
* Extensions use an API to interact with Mubox.
* Extensions can Receive Events for Input, Game Start/Exit, and more.
* Extensions can Emit their own Output.

## Introduction

Over the years I have received numerous requests to implement new 
functionality that I either had no time to implement, or had no
interest in implementing. Both of these are horrible reasons for a
product to not advance.

Mubox is already Open Source, and I have invited people to contrib,
but each time I was told how complex the code was. That it was too
hard to figure out where modifications needed to be made.

"Mubox Extensibility" is all about making the above easier. Internally
Mubox may be a complicated mash of Windows API and .NET, but the
Extensibility APIs are much simpler to work with. Additionally, these
APIs will allow me to refactor and further abstract the internals of 
Mubox without requiring a lot of code to be rewritten as a result.

## Getting Started

This is the easy part:

* Create a new .NET 4.5 Class Library, I use Visual Studio, any version should do.
* Reference "Mubox.Extensibility.DLL" from your class library, this gives you access to the Extensibility APIs.
* Create a new class that implements Mubox.Extensibility.IExtension
* Compile your Class Library, and place the resulting Assembly inside the /Extensions/ folder where Mubox is installed.

## Any CPU, x86 or x64

I urge Extension Authors to target Any CPU. Currently Mubox runs
as an x86/32bit process. I have been making incremental changes to
Mubox over time to natively support x64. Thus, you should target
"Any CPU" in your class libraries (this is the default) and try to
write code that will function on both 32bit and 64bit versions of
Windows. If you stick with plain old .NET code you probably have
nothing to worry about, this is a word of caution for anyone that
is doing P/Invokes or similar.

## Local vs. Remote Clients

Currently, no distinction is made between a local client and 
a remote client. Similarly, extensions are not provided with
Process IDs, Handles, Host Machine Names, etc. This is by
design, since dealing with the locality of a game is often
problematic, the Mubox Extensibility attempts to abstract
those complexities away by providing a one-size-fits-all
API to working with clients, whether they are local or remote.

## Debugging

You can debug your extensions by setting Mubox.QuickLaunch.exe as 
your target executable, or by attaching to an already executing 
copy. The former is better for active development, since you can 
exit, rebuild and re-run Mubox with less effort.

## Security

Extensions run with the same privilege level as Mubox, therefore you
should not trust any Extension which you have not personally compiled
and reviewed. In the future I will consider making an online repo where
human beings can review and comment on code, and also a place where I 
can pull in trusted Extensions to become part of the official Mubox 
distribution. For now, trust no-one.

## Forward Compatibility

You are not required to reference Mubox.DLL, and I encourage you to
avoid doing so. Over the next year I will be rewriting some of the
Mubox core. One of the purposes of the Extensibility APIs is to ensure
I can do this without breaking 3rd party code (e.g. your code.) If you
reference Mubox.DLL and a change I make breaks what you've written, I'm
very sorry but I won't be reverting my changes.

## Assistance from the Author?

Yes, I will assist you if you need help working with the APIs I've provided.

No, I will not become a partner/contributor in developing your extension(s).
(No offense, but I barely have the time as it is.)

The exception to this will be any extensions which manage to replace 
core/conflicting behaviors of Mubox and which necessarily require Mubox
to change as a result. I'm extremely open to the idea of retiring, moving, 
or otherwise refactoring Mubox internals to make new/better/creative Extensions 
available.


That said, I'm very excited to bring this to you. Extensibility will
become a core focus for Mubox. I only work on Mubox because I need it,
I refuse to pay a monthly or annual subscription for something I can
do on my own. Don't be afraid to contact me if you have questions,
suggestions or just need help getting started.

The best way to contact me these days is on Github. Any old Codeplex, MSN,
or personal e-mail accounts I may have published in the past are no longer
valid. If I see e-mail related to Mubox, I'll ignore it out of principal.

Enjoy!
