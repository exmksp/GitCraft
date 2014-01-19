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
		private const string TOOLTIP_ENABLED_UIOFF = "Show GitCraft history";
		private const string TOOLTIP_ENABLED_UION = "Hide GitCraft history";
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

		private void updateTooltip() {
			string tooltip = null;
			if (gitButton.Enabled) {
				tooltip = GitPartless.ShowGitCraftUI ? TOOLTIP_ENABLED_UION : TOOLTIP_ENABLED_UIOFF;
			} 
			else
				tooltip = TOOLTIP_DISABLED;
			if (tooltip != gitButton.ToolTip)
				gitButton.ToolTip = tooltip;
		}

		public void enableGitButton(bool enable) {
			gitButton.Enabled = enable;
			updateTooltip();
		}

		public void Update() {
			if (GitPartless.ShowGitCraftUI && gitButton.TexturePath != TEXTURE_UION) 
				gitButton.TexturePath = TEXTURE_UION;
			if (!GitPartless.ShowGitCraftUI && gitButton.TexturePath != TEXTURE_UIOFF)
				gitButton.TexturePath = TEXTURE_UIOFF;
			updateTooltip();
		}
	}
}

