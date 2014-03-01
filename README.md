# Mubox

Mubox is a multiplexer emulator written in C#/.NET utilizing WPF and WinAPI. 

It offers "Keyboard Multicast", "Mouse Clone" and multiple-clients locally and remotely. 

Mubox is an Open-Source variation of pre-existing closed-source and/or proprietary multiplexer emulators such as 'Octopus', 'Multibox', and 'KeyClone'; though it makes no attempt to replicate the workflow nor features of these applications.

## Features

For a full list of features, refer to FEATURES.md, here are the highlights:

- Game Profiles for multiple games, or multiple configs of a single game.
- Keyboard Multicast
- Mouse Cloning
- Control-Alt-Shift Modifiers (to configure "FTL" in some games)
- Multibox on a single computer, or multiple computers over a network.
- Game Sandboxing - Games are run using limited user accounts, preventing 'some' games from detecting Mubox.
- Fast game switching via ALT-TAB, when Mubox is enabled you will never accidentally switch to a non-Game window.

## Unsupported

- **Muboxer Add-In for WoW** is no longer actively maintained. If nobody is interested in maintaining Muboxer it will be removed from the source tree.

## History

Mubox was born more than 5 years ago as a personal project. 
It was released to a limited number of players in WoW forums in early 2009.
Because it was not received well, being cursed as spyware and a keylogger, the source code was made open to everyone to provide gamers with full transparency.
Mubox has had two contributors and numerous changes ensuring users were able to use Mubox to play their favorite games. It may not be perfect, it may not even work for you, but it does exactly what the author needs it to do. Hopefully you find it useful, too.

**April 2008** - Mubox was born as a very simple multi-computer Keyboard and Mouse broadcasting toolset, it was composed of a Server application and separate desktop application. In the earliest versions Mubox had an overlay window for all clients, and Windows was responsible for ALT+TAB switching between them. This initial version was only released to friends and coworkers, it had numerous bugs and a very poor user experienced. Most people couldn't figure out how to make it work.

**April 2009** - Mubox source code was opened to the public and uploaded to codeplex, making Mubox the only actively maintained Open Source multiboxing application available.

**April 2010** - Mubox source code was taken offline due to a cease and desist letter issued to CodePlex. The author retained all rights and privileges to Mubox and its source code. 

**January 2012** - Mubox source code was updated to build using the latest tools from Microsoft and re-published to CodePlex, it remains on codeplex as a historical snapshot of Mubox prior to any new development.

**January 2013** - Mubox source code was forked and pushed to http://github.com/wilson0x4d/Mubox where it is once again actively maintained. The version on github now represents the latest active version of the project, meanwhile the version which remains on CodePlex represents the old, legacy version of source code before active development resumed.

## Disclaimer

Mubox is not fit for use by anyone. You acquire and/or use Mubox at your own risk. 

By downloading Mubox in source code or binary form you accept that it may cause damages and losses too numerous to list here.

Contributors will not be responsible for loss or damage due to use, misuse or non-use of the software.

The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.


## License

Mubox is an Open Source software project release under the MS-RL license terms. This ensures the work, and derivative works, continue to provide access the to original and modified source code.

Please see LICENSE.MD for MS-RL license details.

Further, contributors retain all rights to intellectual property unique to their contribution. Use of intellectual property that cannot be demonstrated as public domain prior to introduction in Mubox will be subject to license terms and be considered a portion of the software.


## Support

For official support you can contact contributors via Facebook:

https://www.facebook.com/groups/mubox/

If you hate Facebook you can try e-mailing mubox@mrshaunwilson.com, however, it may be several months before your message is seen.


## Downloads

Currently there is no end-user downloadable. 

We will update with download locations when available.

For tracking and discussion, the current development version is somewhere between 1.6 and 1.9 as an "unreleased beta" line of code. The next downloadable version will come stamped with a 2.0 version number.


## Compiling and Executing

Mubox currently requires MSBuild and Microsoft .NET Framework 4.5 to build, you can build from a command-line using MSBuild:

		MSBuild Mubox.sln

However, in practice, Mubox contributors usually build and run Mubox from within Visual Studio. You should be able to use a free version of Visual Studio as we do not make use of any Pro, Premium nor Ultimate features during development.


## Contributions

Mubox is currently an *UNSTABLE BETA* and is available for *DEVELOPMENT AND FEEDBACK* only.

Everything you see in the current version is in flux, if you wish to contribute please contact before comitting your time to ensure your effort is properly supported and protected.

