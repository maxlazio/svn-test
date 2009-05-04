using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Browser;
using org.OpenVideoPlayer.Controls;
using System.Windows.Shapes;
using System.Windows.Media;


namespace OVPImplementation {
	[ScriptableType]
	public partial class SupportPlayer : UserControl {

		public SupportPlayer(object sender, StartupEventArgs e) {
			InitializeComponent();

			player.OnStartup(sender, e);
			player.ApplyTemplate();

			foreach (FrameworkElement fe in Player.ControlBox.Children) {
				if (fe is Button && (fe.Name.Contains("Chapters") || fe.Name.Contains("Playlist") || fe.Name.Contains("Next") || fe.Name.Contains("Previous"))) {
					fe.Visibility = Visibility.Collapsed;
					fe.Margin = new Thickness(0);
					fe.Width = 0.0;
				}
			}
			foreach (FrameworkElement fe in Player.Children) {
				Rectangle h = fe as Rectangle;
				if (h != null && h.Name == "highlightBorder") {
					h.StrokeThickness = 0;
					h.Fill = new SolidColorBrush(Colors.Transparent);
				}
			}
			Player.MediaElement.Margin = new Thickness(0);
		}

		[ScriptableMember]
		public OpenVideoPlayerControl Player {
			get { return player; }
		}

	}
}
