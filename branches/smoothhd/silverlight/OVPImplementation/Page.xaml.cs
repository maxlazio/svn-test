using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Browser;
using org.OpenVideoPlayer.Controls;


namespace org.OpenVideoPlayer {
	[ScriptableType]
	public partial class Page : UserControl {

		public Page(object sender, StartupEventArgs e) {
			InitializeComponent();

			//System.Windows.Resources.StreamResourceInfo streamInfo
			//    = System.Windows.Application.GetResourceStream(new Uri("OpenVideoPlayer;component/themes/generic.xaml", UriKind.Relative));
			//StreamReader sr = new StreamReader(streamInfo.Stream);
			//object o = XamlReader.Load(sr.ReadToEnd()); 

			player.OnStartup(sender, e);
			player.ApplyTemplate();

		}

		[ScriptableMember]
		public OpenVideoPlayerControl Player {
			get { return player; }
		}

	}
}
