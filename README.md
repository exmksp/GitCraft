GitCraft version 0.9b2

INTRODUCTION

The goal of GitCraft is to make sure that craft revisions are not lost during
design and testing and to allow designers to easily restore older designs
even if the craft was overwritten in the editor. In other words, GitCraft gives
you the ability to undo any change to your craft provided that the version that
you want to restore was saved. Thus, GitCraft can be compared to an undo button
that works across launches and game sessions. GitCraft achieves this ability by
integrating Git distributed revision control software with KSP.

GitCraft only affects editing your ships. It does not interact with your vessel
outside of Vehicle Assembly Building or Space Plane Hangar. GitCraft integrates
with the standard editor UI. In other words, your revisions are stored by git as
you save them in the editor. To restore a revision you can activate the history
window by clicking the git button and then click on the revision that you want
to restore. GitCraft shows the part count and the game time when the ship was
saved. Since version 0.9b2, and in the version you downloaded, attaching a part
is not required to enable GitCraft, it is a "partless" plugin that works for
all ships.

GitCraft is integrated with the popular Toolbar plugin (see
http://forum.kerbalspaceprogram.com/threads/60066), but it will work even if the
Toolbar plugin is not installed.

Currently, GitCraft works on Windows, Mac OS X, and a subset of Linux
installations (i.e., x86-64 installations of Linux and corresponding 64-bit KSP
executables).

INSTALLATION

1) Merge the contents of the GameData directory with the GameData directory of
your Kerbal Space Program directory.
Thus, the directory "Exosomatic Ontologies" should appear inside the GameData
directory of your KSP. If you are upgrading from an earlier version, then
the old directory in GameData should be replaced by the directory from this
distribution. In other words, please delete the old directory and then put the
directory from this distribution in its place.
2) On Windows, copy git2-65e9dc6.dll into root KSP directory, i.e., into the
directory where the file KSP.exe is.
On Mac, copy the file libgit2-65e9dc6.dylib into the root KSP directory.
On Linux, copy the file libgit2-65e9dc6.so into the root KSP directory.
This step is necessary because GitCraft currently uses libgit2, which is a
native implementaton of git. If you are upgrading from an earlier version, then
this step can be skipped.

TECHNICAL DETAILS

GitCraft initializes a new git repository in your saved game directory. Thus,
if the name of your game is "default", then a git repository will be intialized
in <KSP_ROOT>/saves/default/.git

To delete all data stored by GitCraft, simply remove the ".git" directory.

GitCraft automatically commits the craft file when it is saved. The part count
and the timestamp are stored in the commit message.

Since version 0.9b2, the Git part is NO LONGER REQUIRED for GitCraft to work,
the plugin is "partless" and is enabled for all designs.
The Git part is still present in the "Control" section of the editor
to provide painless upgrade path for 0.9b1 users. In the next version the part
WILL BE COMPLETELY REMOVED from the plugin. The upgrade code path, which will
include a dialog requesting permission to chop the part off, will be provded in
the next release. Users are advised to complete all active missions that use
vessels with the Git part attached to them.

SOURCE CODE AND LICENSING

GitCraft uses libgit2 and libgit2sharp. GitCraft is distributed under GPLv2
license, please see GitCraft_license.txt.

libgit2 is distributed under GPLv2, please see its license in
libgit2.license.txt.

This project uses portions of Novapunch part pack, which is licensed under
Creative Commons Attribute-Sharealike 3.0 License. Please see the license for
Novapunch in novapunch2_03a_license.txt.

This project uses the git logo, which was created by Jason Long. The logo was
covered by Creative Commons Attribution 3.0 Unported license
as of 01/20/2013. Please refer to http://creativecommons.org/licenses/by/3.0/
in order to obtain the text of the license.

The buildable source code for GitCraft and all its dependencies can be obtained
from https://github.com/exmksp/GitCraft
To build a universal binary for the libgit2 binary on Mac you may need to use
the patch stored in the source directory.

KNOWN ISSUES

1. If a case-insensitive filesystem is used, then GitCraft does not work well if
the capitalization of the name of the ship differs from the capitalization of
the filename for the file in which the ship is saved. For example, if you
are using the case-insensitive version of the Mac OS Extended filesystem
(the default version), and if the name of your ship is "Rocket" and if
it is saved in the file "rocket.craft", then GitCraft won't pick up the history
correctly. Please make sure that capitalization of your files and the name of
the ship are consistent. The issue may arize if you decide to change the
capitalization of the ship's name after it was saved.

2. GitCraft does not work well with "Auto Saved Ship.craft" files generated by
KSP when you launch a ship without saving its design, unless the name of your
auto saved ship is "Auto Saved Ship".

3. GitCraft does not pick up history before the craft was saved or loaded.
The reason for this is that plugins don't have access to the contents of the
ship name widget in the editor. The plugins only have access to the ship's
name, which is updated during saving or loading. To get access to the history
of your ship, save it or, if it was already saved, load it.

Changelog

0.9b2:
 * Plugin is now partless.
 * Integration with blizzy78's toolbar.
 * Linux support.

0.9b1: First release.