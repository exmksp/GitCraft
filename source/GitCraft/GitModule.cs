using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using LibGit2Sharp;

namespace GitNS
{
    public class GitModule : PartModule
    {
        public override void OnSave(ConfigNode node)
        {
            if (!HighLogic.LoadedSceneIsEditor)
                return;
            if (null == GitPartless.fetch)
                return;
            GitPartless.fetch.shipModified = true;
            node.ClearData();
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class GitPartless : MonoBehaviour
    {
        public bool shipModified = false;
        private static Texture2D gitLogo = new Texture2D(80, 34, TextureFormat.ARGB32, false);
        public static GitPartless fetch = null;

        public static ShipConstruct PartfulShipBeingEdited() {
            // This also prevents partless ships from displaying the git UI.
            if (!HighLogic.LoadedSceneIsEditor || null == EditorLogic.fetch || null == EditorLogic.fetch.ship)
                return null;
            ShipConstruct ship = EditorLogic.fetch.ship;
            if (null == ship.Parts || 0 == ship.Parts.Count)
                return null;
            else
                return ship;
        }

        // Returns the name of the ship currently being edited.
        private static string ShipName() {
            if (ShipConstruction.ShipConfig == null)
                return null;
            else
                return ShipConstruction.ShipConfig.GetValue("ship");
        }

        // Return the filename of the ship currently being edited.
        public static string ShipFilename() {
            string shipName = ShipName();
            if (null == shipName)
                return null;
            return  KSPUtil.SanitizeString(shipName, '_', true) + ".craft";
        }

        // Get the full path to the directory where the current game is saved.
        private static string CurrentGameDirectory() {
            if (null == HighLogic.SaveFolder)
                return null;
            else
                return string.Concat(new string[] { KSPUtil.ApplicationRootPath, "saves",
                    Path.DirectorySeparatorChar.ToString(), HighLogic.SaveFolder });
        }

        // Get path to a ship relative to a repository, which is in the current game directory.
        private static string RelativeShipPath() {
            string filename = ShipFilename();
            if (filename == null)
                return null;
            string subFolder = ShipConstruction.GetShipsSubfolderFor(HighLogic.LoadedScene);
            if (subFolder == null)
                return null;
            return string.Concat(new string[] { "Ships", Path.DirectorySeparatorChar.ToString(),
                subFolder, Path.DirectorySeparatorChar.ToString(), filename });
        }

        public static string FullShipPath() {
            string gamedir = CurrentGameDirectory();
            if (null == gamedir)
                return null;
            string relpath = RelativeShipPath();
            if (null == relpath)
                return null;
            return string.Concat(new string[] { gamedir, Path.DirectorySeparatorChar.ToString(), relpath });
        }

        // See if there is a .git directory in the current game directory. In good faith this assumes
        // if the directory exists, then it is fully set up and ready for action.
        private static bool HaveDotGitDirSetUp()
        {
            string gamedir = CurrentGameDirectory();
            if (null == gamedir)
                return false;
            else
                return Directory.Exists(gamedir + Path.DirectorySeparatorChar + ".git");
        }

        // Get the current repository status of the ship.
        private static FileStatus? ShipRepoStatus() {
            if (!HaveDotGitDirSetUp())
                return null;
            string relpath = RelativeShipPath();
            return ObtainRepository().Index.RetrieveStatus(relpath);
        }

        // This is what should be used to obtain an instance of Repository. The code creates
        // an empty repository, if it does not exist.
        private static Repository ObtainRepository() {
            string gamedir = CurrentGameDirectory();
            if (null == gamedir)
                return null;
            if (!HaveDotGitDirSetUp()) {
                LibGit2Sharp.Repository.Init(gamedir);
            }
            return new Repository(gamedir);
        }

        void Update()
        {
            ShipConstruct ship = PartfulShipBeingEdited();
            if (null == ship)
                return;
            List<Part> parts = ship.Parts;
            if (shipModified && File.Exists(FullShipPath())) {
                FileStatus? status = ShipRepoStatus();
                // This code may be triggered from GitModule.OnSave() that were not really about saving.
                // To filter correct triggers, we need to check that there is a ship and that it has actually changed.
                if (status != null && status.Value != FileStatus.Unaltered) {
                    // This is a bit paranoid, but it is necessary to make .craft files look as if there was
                    // never an injection of the GitModule. As of 0.23, things work just fine even without this,
                    // but who knows. It would suck if users get into trouble because of some empty MODULE
                    // sections. This doesn't really work during the launch, but I think that that should be fine.
                    // The "Auto Saved Ship.craft" files will have references to GitModule and there isn't much
                    // that I can do about them apart from making them empty or implementing some elaborate recovery
                    // strategy which is not worth it because who would share an auto saved ship anyway?

                    // Hopefully, all of this is useless, because SQUAD should be very defensive with .craft loading code 
                    // now that there are so many plugins floating around.
                    for (int ix = 0; ix < parts.Count; ++ix)
                        if (null != parts[ix] && null != parts[ix].Modules)
                        if (parts[ix].Modules.Contains("GitModule")) {
                            for (int kx = 0; kx < parts[ix].Modules.Count; ++kx)
                                if (parts[ix].Modules[kx] is GitModule) {
                                    Debug.Log("Pulling GitModule out!");
                                    //parts[ix].Modules.Remove(parts[ix].Modules[kx]);
                                    parts[ix].RemoveModule(parts[ix].Modules[kx]);
                                    break;
                                }
                        }
                    Debug.Log("re-saving and committing");
                    ShipConstruction.SaveShip(ship, ship.shipName); // get rid of the MODULE node, just in case
                    commitAsNeeded();
                }
            } 
            // Now that we are partless, we need to attach ourselves somewhere so that we
            // can intercept onSave(). We check the first part of the ship in each frame and attach
            // us there. When the ship is saved(), we remove ourselves.
            int? jx = null;
            for (int ix = 0; ix < parts.Count; ++ix)
                if (null != parts[ix] && null != parts[ix].Modules) {
                    if (parts[ix].Modules.Contains("GitModule"))
                        return; // we are already in
                    if (null == jx)
                        jx = ix;
                }
            if (null == jx)
                return;
            Debug.Log("nmod = " + parts[jx.Value].Modules.Count);
            Debug.Log("Plugging GitModule in!");
            parts[jx.Value].AddModule("GitModule");
            Debug.Log("nmod = " + parts[jx.Value].Modules.Count);
        }

        void Awake()
        {
            fetch = this;
        }

        private Signature getSignature()
        {
            return new Signature("GitCraft Plugin for KSP", "gitcraft@localhost", System.DateTimeOffset.Now);
        }

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

        public void commitAsNeeded()
        {
            shipModified = false;
            string fullpath = FullShipPath();
            if (null == fullpath)
                return;
            if (HaveDotGitDirSetUp()) {
                // commit if necessary
                string relpath = RelativeShipPath();
                FileStatus? status = ShipRepoStatus();
                print(String.Format("Repository status = {0}", status.ToString()));
                if (null != status && FileStatus.Unaltered != status.Value && FileStatus.Nonexistent != status.Value) {
                    Repository repo = ObtainRepository();
                    print(String.Format("Committing craft {0}", relpath));
                    repo.Index.Stage(relpath);
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

        void Start()
        {
            if (HighLogic.LoadedSceneIsEditor) {
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

        private List<Commit> getHistory()
        {
            return (List<Commit>)hist;
        }

        private List<Commit> reloadHistory()
        {
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
        static int? UniquePrefixLength(string[] arr)
        {
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
                            EditorLogic.LoadShipFromFile(FullShipPath());
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
            if (null == PartfulShipBeingEdited())
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
            string relPath = RelativeShipPath();
            Repository repo = ObtainRepository();
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
        public void Checkout(string id)
        {
            Repository repo = new Repository(CurrentGameDirectory());
            string relPath = RelativeShipPath();
            CheckoutOptions opts = new CheckoutOptions();
            opts.CheckoutModifiers = CheckoutModifiers.Force;
            repo.CheckoutPaths(id, new string[] { relPath }, opts);
            print("Checked out " + relPath + " from " + id);
        }
    }
}