GitCraft version 0.9b1

INTRODUCTION

The goal of GitCraft is to make sure that craft revisions are not lost during design and testing and to allow to easily
restore older designs even if the craft was overwritten in the editor. In other words, GitCraft gives you the ability to
undo any change to your craft provided that the version that you want to restore was saved. Thus, GitCraft can be compared
to an undo button that works across launches and game sessions.

GitCraft achieves this ability by integrating Git distributed revision control software with KSP.

GitCraft only affects editing your ships. It does not interact with your vessel outside of Vehicle Assembly Building
or Space Plane Hangar (you need to attach the Git part to your ship to enable GitCraft, but the part does not do anything during flight).

GitCraft integrates with the standard editor UI. In other words, your revisions are stored by git as you save them in the editor.
To restore revisions you can activate the history window by clicking the git button and then click on the revision that
you want to restore. GitCraft provides the part count and the game time when the ship was saved.

Currently, GitCraft works only on Windows and Mac OS X.

INSTALLATION

1) Merge the contents of the GameData directory with the GameData directory of your Kerbal Space Program directory.
Thus, the directory "Exosomatic Ontologies" should appear inside the GameData directory of your KSP.
2) On Windows, copy git2-65e9dc6.dll into root KSP directory, i.e., into the directory where the file KSP.exe is.
On Mac, copy the file libgit2-65e9dc6.dylib into the root KSP directory.
This step is necessary because GitCraft currently uses libgit2, which is a native implementaton of git. 

To use GitCraft on your ship, simply attach the "Git" part that can be found under the "Control" section of the editor 
to any part of your ship. The git button should appear in the top right corner of the editor once the part was attached.

TECHNICAL DETAILS

GitCraft initializes a new git repository in your saved game directory. Thus, if the name of your game is "default",
then a git repository will be intialized in <KSP_ROOT>/saves/default/.git

To delete all data stored by GitCraft, simply remove the ".git" directory.

GitCraft automatically commits the craft file when it is saved. The part count and the timestamp are stored in the commit message.

SOURCE CODE AND LICENSING

GitCraft uses libgit2 and libgit2sharp. GitCraft is distributed under GPLv2 license, please see GitCraft_license.txt.
libgit2 is distributed under GPLv2, please see its license in libgit2.license.txt.

This project uses portions of Novapunch part pack, which is licensed under Creative Commons Attribute-Sharealike 3.0 License.
Please see the license for Novapunch in novapunch2_03a_license.txt.

This project uses the git logo, which was created by Jason Long. The logo was covered by Creative Commons Attribution 3.0 Unported license 
as of 01/15/2013. Please refert to http://creativecommons.org/licenses/by/3.0/ in order to obtain the text of the license.

The buildable source code for GitCraft and all its dependencies can be obtained from https://github.com/exmksp/GitCraft
To build a universal binary for the libgit2 binary on Mac you may need to use the patch stored in the source directory.

