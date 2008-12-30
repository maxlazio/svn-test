using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;


namespace OVPImplementation {
	public partial class Page : UserControl {

		public Page(object sender, StartupEventArgs e) {
			InitializeComponent();

			//System.Windows.Resources.StreamResourceInfo streamInfo
			//    = System.Windows.Application.GetResourceStream(new Uri("OpenVideoPlayer;component/themes/generic.xaml", UriKind.Relative));
			//StreamReader sr = new StreamReader(streamInfo.Stream);
			//object o = XamlReader.Load(sr.ReadToEnd()); 

			Player.OnStartup(sender, e);
		}
	}
}
