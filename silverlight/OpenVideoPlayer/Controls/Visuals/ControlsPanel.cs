using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace org.OpenVideoPlayer.Controls.Visuals {

	public class ControlsPanel : ControlBase {

		public ControlsPanel() {
			Highlight = new SolidColorBrush(Colors.Black);
			PlaybackPositionText = PlaybackDurationText = "";
		}

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();
			LayoutRoot = GetTemplateChild("layoutRoot") as StackPanel;
		}

		protected internal StackPanel LayoutRoot;

		private Brush highlight;
		public Brush Highlight {
			get { return highlight; }
			set { highlight = value; }
		}

		public readonly static DependencyProperty HighlightProperty = DependencyProperty.Register("Highlight", typeof(Brush), typeof(ControlsPanel), new PropertyMetadata(null));

		//public string PlaybackPositionText { get; set; }
		public String PlaybackPositionText {
			get { return (String)GetValue(PlaybackPositionTextProperty); }
			set { SetValue(PlaybackPositionTextProperty, value); }
		}
		public readonly static DependencyProperty PlaybackPositionTextProperty = DependencyProperty.Register("PlaybackPositionText", typeof(string), typeof(ControlsPanel), new PropertyMetadata(null));

		//public string PlaybackDurationText { get; set; }
		public String PlaybackDurationText {
			get { return (String)GetValue(PlaybackDurationTextProperty); }
			set { SetValue(PlaybackDurationTextProperty, value); }
		}
		public readonly static DependencyProperty PlaybackDurationTextProperty = DependencyProperty.Register("PlaybackDurationText", typeof(string), typeof(ControlsPanel), new PropertyMetadata(null));

		public UIElementCollection Children { get { return LayoutRoot.Children; } }
	}
}
