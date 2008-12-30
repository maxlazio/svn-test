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
	public class ContentLinkEmbedBox : ControlBase {
		internal TextBox linkText;
		public TextBox LinkText {
			get { return linkText; }
			set { linkText = value; }
		}

		internal TextBox embedText;
		public TextBox EmbedText {
			get { return embedText; }
			set { embedText = value; }
		}

		public override void OnApplyTemplate() {
			BindFields = false;
			base.OnApplyTemplate();
			linkText = GetTemplateChild("linkText") as TextBox;
			embedText = GetTemplateChild("embedText") as TextBox;
			linkText.GotFocus += OnLinkText_GotFocus;
			embedText.GotFocus += OnEmbedText_GotFocus;
		}


		internal void OnEmbedText_GotFocus(object sender, RoutedEventArgs e) {
			embedText.SelectAll();
		}

		internal void OnLinkText_GotFocus(object sender, RoutedEventArgs e) {
			linkText.SelectAll();
		}
	}
}
