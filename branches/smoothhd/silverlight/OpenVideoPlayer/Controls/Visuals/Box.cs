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
	public class Box : ControlBase{

		public override void OnApplyTemplate() {
			base.BindFields = false;
			base.OnApplyTemplate();
			text = GetTemplateChild("text") as TextBlock;
			close = GetTemplateChild("close") as Border;
			if (text != null) text.Text = GetValue(Box.TextProperty) as string;

			close.MouseLeftButtonUp += OnCloseMouseLeftButtonUp;
		}

		internal Border close;
		internal TextBlock text;
		public Panel LayoutRoot { get { return GetTemplateChild("grid") as Panel; } }

		public string Text { 
			get {
				return (text != null) ? text.Text : GetValue(Box.TextProperty) as string;
			}
			set { 
				if(text!=null) text.Text = value;
				SetValue(Box.TextProperty, value);
			}
		}
		public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(Box), new PropertyMetadata(null));

		internal void OnCloseMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			this.Visibility = Visibility.Collapsed;
		}

	}
}
