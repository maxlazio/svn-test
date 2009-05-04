using System.Windows;
using System.Windows.Controls;
using org.OpenVideoPlayer.Media;
using System.Collections;
using org.OpenVideoPlayer.Util;
using System.Windows.Media;
using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace org.OpenVideoPlayer.Controls.Visuals {
	/// <summary>
	/// A base class used for the content classes for our buttons, etc.  We use different classes for each one, 
	/// because we can override styles per type as needed, but if we set them by name they can't be overridden
	/// </summary>
	public class ContentOverrideControl : ContentControl {
		public ContentOverrideControl() { DefaultStyleKey = this.GetType(); }
	}

	public class ContentButtonOptions : ContentOverrideControl { }
	public class ContentButtonPause : ContentOverrideControl { }
	public class ContentButtonPlay : ContentOverrideControl {
		//public ContentButtonPlay() { this.b}
	}
	public class ContentButtonNext : ContentOverrideControl { }
	public class ContentButtonPrevious : ContentOverrideControl { }
	public class ContentButtonMute : ContentOverrideControl { }
	public class ContentButtonFullScreen : ContentOverrideControl { }
	public class ContentButtonLinkEmbed : ContentOverrideControl { }
	public class ContentButtonChapters : ContentOverrideControl { }

	public class ContentButtonPlaylist : ContentOverrideControl {
		public new Visibility Visibility {
			get { return base.Visibility; }
			set { base.Visibility = value; }
		}
	}


	public class PlayerOptionsMenu : Menu {

	}
}
