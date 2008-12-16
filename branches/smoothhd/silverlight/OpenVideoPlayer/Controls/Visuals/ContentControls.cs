using System.Windows;
using System.Windows.Controls;

namespace org.OpenVideoPlayer.Controls.Visuals {

	public class ContentOverrideControl : ContentControl {
		public ContentOverrideControl() {DefaultStyleKey = this.GetType();}
	}

	public class ContentButtonOptions : ContentOverrideControl {}
	public class ContentButtonPause : ContentOverrideControl {}
	public class ContentButtonPlay : ContentOverrideControl { }
	public class ContentButtonNext : ContentOverrideControl {}
	public class ContentButtonPrevious : ContentOverrideControl {}
	public class ContentButtonMute : ContentOverrideControl {}
	public class ContentButtonFullScreen : ContentOverrideControl {}
	public class ContentButtonLinkEmbed : ContentOverrideControl { }

	public class ContentButtonChapters : ContentOverrideControl { }

	public class ContentButtonPlaylist : ContentOverrideControl {
		public new Visibility Visibility {
			get { return base.Visibility; }
			set { base.Visibility = value; }
		}
	}
}
