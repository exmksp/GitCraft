/* Integration of GitCraft with blizzy78's toolbar.
 */
using System;
using UnityEngine;
using Toolbar;
using GitCraft;

namespace GitCraft {
	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class ToolbarIntegration : MonoBehaviour, IToolbarIntegration {
		private IButton gitButton;
		private const string TOOLTIP_ENABLED = "Toggle GitCraft history";
		private const string TOOLTIP_DISABLED = "GitCraft needs a ship to operate on.";
		private const string TEXTURE_UIOFF = "Exosomatic Ontologies/GitCraft/ToolbarIcons/git_logo_orange";
		private const string TEXTURE_UION  = "Exosomatic Ontologies/GitCraft/ToolbarIcons/git_logo_black";
		internal ToolbarIntegration() {
			Debug.Log("Instantiating ToolbarIntegration for the Git");
			gitButton = ToolbarManager.Instance.add("GitCraft", "gitButton");
			gitButton.TexturePath = TEXTURE_UIOFF;
			gitButton.OnClick += (e) => {
				GitPartless.ShowGitCraftUI = !GitPartless.ShowGitCraftUI;
				if (GitPartless.ShowGitCraftUI && null != GitPartless.fetch)
					GitPartless.fetch.reloadHistory();
			};
			gitButton.Enabled = false;
			gitButton.ToolTip = TOOLTIP_DISABLED;
			GitPartless.ToolbarIntegration = this;
		}

		internal void OnDestroy() {
			gitButton.Destroy();
		}

		public bool isGitButtonEnabled() {
			return gitButton.Enabled;
		}

		public void enableGitButton(bool enable) {
			gitButton.Enabled = enable;
			gitButton.ToolTip = enable ? TOOLTIP_ENABLED:TOOLTIP_DISABLED;
		}

		public void Update() {
			if (GitPartless.ShowGitCraftUI && gitButton.TexturePath != TEXTURE_UION)
				gitButton.TexturePath = TEXTURE_UION;
			if (!GitPartless.ShowGitCraftUI && gitButton.TexturePath != TEXTURE_UIOFF)
				gitButton.TexturePath = TEXTURE_UIOFF;
		}
	}
}

