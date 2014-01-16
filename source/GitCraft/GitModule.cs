using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using LibGit2Sharp;

namespace GitNS
{
    public class GitPart : Part
    {
        public GitModule gitMod = null;

        protected override void onEditorUpdate()
        {
            base.onEditorUpdate();
            if (this.gitMod != null && this.gitMod.needsCommit)
                this.gitMod.commitAsNeeded(); // this is supposed to intercept the next frame after save
        }
    }

    public class GitModule : PartModule
    {
        private static Texture2D gitLogo = new Texture2D(80, 34, TextureFormat.ARGB32, false);
        public static string DirSep = System.IO.Path.DirectorySeparatorChar.ToString();

        private static string ShipName()
        {
            if (ShipConstruction.ShipConfig == null)
                return null;
            else
                return ShipConstruction.ShipConfig.GetValue("ship");
        }
        // Get the full path to the directory where the saved games are.
        private static string getSaveFolder()
        {
            if (null == HighLogic.SaveFolder)
                return null;
            else
                return string.Concat(new string[] {
                    KSPUtil.ApplicationRootPath, "saves", DirSep,
                    HighLogic.SaveFolder
                });
        }
        // Get full path to a ship.
        private static string fullShipPath(string shipFileName)
        {
            if (shipFileName == null)
                return null;
            string saveFolder = getSaveFolder();
            if (saveFolder == null)
                return null;
            string relPath = RelShipPath(shipFileName);
            if (relPath == null)
                return null;
            return string.Concat(new string[] { saveFolder, DirSep, relPath });
        }
        // Get path to a ship relative to a repository.
        private static string RelShipPath(string shipFileName)
        {
            if (shipFileName == null)
                return null;
            string subFolder = ShipConstruction.GetShipsSubfolderFor(HighLogic.LoadedScene);
            if (subFolder == null)
                return null;
            return string.Concat(new string[] { "Ships", DirSep, subFolder,
                DirSep, shipFileName
            });
        }
        // See if we have the git repository set up.
        private static bool haveGitRepo()
        {
            string saveFolder = getSaveFolder();
            if (null == saveFolder)
                return false;
            else
                return Directory.Exists(saveFolder + System.IO.Path.DirectorySeparatorChar + ".git");
        }

        private Signature getSignature()
        {
            return new Signature("GitCraft Plugin for KSP", "gitcraft@localhost", System.DateTimeOffset.Now);
        }

        public bool needsCommit = false;


        // Timestamp for the craft in game time, earth units.
        public static string commitTS()
        {
            double ts = Planetarium.GetUniversalTime();
            const long secondsPerMinute = 60;
            const long minutesPerHour = 60;
            const long hoursPerDay = 24;
            long sec = Convert.ToInt64(ts);
            long min = sec / 60;
            long hour = min / 60;
            long day = hour / 24;
            sec = sec % 60;
            min = min % 60;
            hour = hour % 24;
            day += 1;
            return ("day " + day.ToString()
                + ", " + hour.ToString("00")
                + ":" + min.ToString("00")
                + ":" + sec.ToString("00"));
        }

        public int? numParts()
        {
            if (null != ShipConstruction.ShipConfig)
                return ShipConstruction.ShipConfig.CountNodes;
            else
                return null;
        }

        public static string ShipFileName()
        {
            return  KSPUtil.SanitizeString(ShipName(), '_', true) + ".craft";
        }

        public void commitAsNeeded()
        {
            needsCommit = false;
            if (!isEnabled)
                return;
            print("Git part OnLoad called.");
            string shipFileName = ShipFileName();
            print("Ship type: " + ShipConstruction.ShipType);
            string fullPath = fullShipPath(shipFileName);
            print("Full ship path: " + (fullPath == null ? "<null>" : fullPath));
            if (null == fullPath)
                return;
            if (!haveGitRepo() && null != getSaveFolder()) {
                print("Initializng the repository...");
                LibGit2Sharp.Repository.Init(getSaveFolder());
                print("Done.");
            }
            if (haveGitRepo()) {
                // commit if necessary
                string relPath = RelShipPath(shipFileName);
                print(String.Format("Relative path = {0}", relPath));
                Repository repo = new Repository(getSaveFolder());
                FileStatus status = repo.Index.RetrieveStatus(relPath);
                print(String.Format("Repository status = {0}", status.ToString()));
                if (FileStatus.Unaltered != status && FileStatus.Nonexistent != status) {
                    print(String.Format("Committing craft {0}", relPath));
                    repo.Index.Stage(relPath);
                    string msg = ""; // + relPath + "\", ";
                    int? np = numParts();
                    if (np != null)
                        msg += np.ToString() + " parts, " + commitTS();
                    repo.Commit(msg, getSignature(), getSignature());
                    print("Commit successful.");
                    reloadHistory();
                }
            }
        }

        public void loadIcons()
        {
            byte[] data = KSP.IO.File.ReadAllBytes<GitModule>("git_logo.png");
            print("Loaded " + data.Length + " bytes from the Git logo.");
            gitLogo.LoadImage(data);
        }

        protected Rect btnwinPos;
        protected Rect gitwinPos;
        protected Vector2 scrollPos;
        protected bool startedUI = false;
        protected bool gitOn = false;
        const int BTNWIN_ID = 1;
        const int GITWIN_ID = 2;

        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor) {
                print("Git part started in the editor.");
                if (!startedUI) {
                    loadIcons();
                    if (btnwinPos.x == 0 && btnwinPos.y == 0)
                        btnwinPos = new Rect(Screen.width - 103, 37, 86, 40);
                    if (gitwinPos.x == 0 && gitwinPos.y == 0)
                        gitwinPos = new Rect(Screen.width / 2 - 250,
                            Screen.height / 2 - 250, 400, 300); 
                    RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI));
                    startedUI = true;
                }
            }
        }

        // Something in KSP makes using opaque type necessary here.
        private object hist = null;
        private List<Commit> getHistory() {
            return (List<Commit>)hist;
        }

        private List<Commit> reloadHistory() {
            List<Commit> h = LoadHistory();
            print("History size: " + h.Count.ToString());
            hist = h;
            return h;
        }

        // Get the length of a unique prefix of a collection of strings.
        // This is intended for shortening the length of commit ids to display
        // in the UI.
        // null is returned if there is no unique prefix or if the unique prefix 
        // has length 0 or if the input array is empty.  
        static int? UniquePrefixLength(string[] arr) {
            if (arr.Length == 0)
                return null; // degenerate case
            HashSet<string> pref = new HashSet<string>();
            int rv;
            for (rv = 0; rv <= arr[0].Length; ++rv) {
                pref.Clear();
                for (int i = 0; i < arr.Length; ++i)
                    pref.Add(arr[i].Substring(0, rv));
                if (pref.Count == arr.Length)
                    return rv;
            }
            return null;
        }

        private void WindowGUI(int windowID)
        {
            if (BTNWIN_ID == windowID) {
                if (GUILayout.Button(gitLogo)) {
                    gitOn = !gitOn;
                    if (gitOn) {
                        reloadHistory();
                    }
                }
                GUI.DragWindow();
            }
            if (GITWIN_ID == windowID) {
                GUILayout.BeginVertical();
                List<Commit> hist = getHistory();
                if (null == hist)
                    GUILayout.Label("No history at all?");
                else {
                    scrollPos = GUILayout.BeginScrollView(scrollPos, GUI.skin.scrollView);
                    string[] idarr = new string[hist.Count];
                    for (int ix = 0; ix < hist.Count; ++ix)
                        idarr[ix] = hist[ix].Sha;
                    int prefL = 10;
                    {
                        int? preflen = UniquePrefixLength(idarr);
                        if (null != preflen && preflen > prefL)
                            prefL = preflen.Value;
                    }
                    GUILayout.BeginVertical();
                    for (int ix = 0; ix < hist.Count; ++ix) {
                        GUILayout.BeginHorizontal();
                        GUI.skin.button.stretchWidth = true;
                        if (GUILayout.Button(idarr[ix].Substring(0, prefL))) {
                            Checkout(idarr[ix]);
                            EditorLogic.LoadShipFromFile(fullShipPath(ShipFileName()));
                        }
                        GUILayout.Label(hist[ix].Message);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndScrollView();
                }
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reload")) {
                    reloadHistory();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUI.DragWindow();
            }
        }

        public void drawGUI()
        {
            if (!part.isAttached || null == part.parent)
                return;
                
            GUIStyle winst = new GUIStyle();
            winst.border.left = winst.border.top = winst.border.right = winst.border.bottom = 0;
            winst.padding.left = winst.padding.top = winst.padding.bottom = winst.padding.right = 5;
            winst.contentOffset = new Vector2(0, 0);
            GUI.skin = HighLogic.Skin;
            GUIStyle old_winst = GUI.skin.window;
            GUI.skin.window = winst;
            btnwinPos = GUILayout.Window(BTNWIN_ID, btnwinPos, WindowGUI, GUIContent.none);
            GUI.skin.window = old_winst;

            if (gitOn)
                gitwinPos = GUILayout.Window(GITWIN_ID, gitwinPos, WindowGUI, "History", GUILayout.MinWidth(400));
        }

        public override void OnAwake()
        {
            // Bind to the part so that we can catch the save. Maybe there is a more
            // elegant way to do this -- essentially I need to defer the commit to
            // the earliest time when the save has finished so that git can pick up
            // the data from disk. I don't think that the render loop is the right way to do this,
            // (what if it runs in a different thread?)

            // For now, playing the ball between OnAwake(), OnSave(), and part.OnEditorUpdate() seems to work well.
            GitPart gpart = (GitPart)part;
            if (null != gpart)
                gpart.gitMod = this; 
        }

        public override void OnLoad(ConfigNode node)
        {
            // Committing here would force a commit of a vessel file on disk
            // as the git part is being attached to it in the editor, which
            // means that we are committing stuff without the git part
            // potentially. So, we only update the history here.
        }

        public override void OnSave(ConfigNode node)
        {
            if (!HighLogic.LoadedSceneIsEditor || !isEnabled)
                return;
            needsCommit = true;
        }


        // Adopted and modified the code from
        // http://stackoverflow.com/questions/13122138/what-is-the-libgit2sharp-equivalent-of-git-log-path
        // <summary>
        /// Loads the history for a file
        /// </summary>
        /// <param name="filePath">Path to file</param>
        /// <returns>List of version history</returns>
        public List<Commit> LoadHistory()
        {
            List<Commit> list = new List<Commit>();
            HashSet<string> dedup = new HashSet<string>();
            string saveFolder = getSaveFolder();
            string relPath = RelShipPath(ShipFileName());
            if (null == saveFolder || null == relPath)
                return list;
            Repository repo = new Repository(saveFolder);
            CommitFilter cf = new CommitFilter();
            // get commits in reverse topological order so that parents go first
            // this works together with deduplicator to obtain something like git-log
            cf.SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse;
            foreach (Commit commit in repo.Commits.QueryBy(cf)) {
                //if (this.TreeContainsPath(commit.Tree, relPath))
                TreeEntry te = commit[relPath];
                if (te != null) {
                    if (dedup.Contains(te.Target.Sha))
                        continue;
                    dedup.Add(te.Target.Sha);
                    list.Add(commit);
                }
            }
            list.Reverse(); // so that we get forward topological order
            return list;
        }

        // Checkout the current ship from a specified commit. 
        public void Checkout(string id) {
            Repository repo = new Repository(getSaveFolder());
            string relPath = RelShipPath(ShipFileName());
            repo.CheckoutPaths(id, new string[] { relPath });
            print("Checked out " + relPath + " from " + id);
        }
    }
}