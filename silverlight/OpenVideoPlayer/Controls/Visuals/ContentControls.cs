using System.Windows;
using System.Windows.Controls;
using org.OpenVideoPlayer.Media;
using System.Collections;
using org.OpenVideoPlayer.Util;
using System.Windows.Media;
using System;
using System.Diagnostics;
using System.Text;

namespace org.OpenVideoPlayer.Controls.Visuals {

	public class ContentOverrideControl : ContentControl {
		public ContentOverrideControl() { DefaultStyleKey = this.GetType(); }
	}

	public class ContentButtonOptions : ContentOverrideControl { }
	public class ContentButtonPause : ContentOverrideControl { }
	public class ContentButtonPlay : ContentOverrideControl { }
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

	public class CustomToolTip : ControlBase {
		internal TextBlock textBox;

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();
			textBox = GetTemplateChild("textBox") as TextBlock;
		}

		public string Text {
			get { return textBox.Text; }
			set { textBox.Text = value; }
		}
	}
}
