// == ThumbnailImage
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace org.OpenVideoPlayer.Controls.Visuals {
	public class Thumbnail : ContentControl {
		// Using a DependencyProperty as the backing store for SourceUrl.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(string), typeof(Thumbnail), new PropertyMetadata(new PropertyChangedCallback(Prop)));

		public String Source {
			get { return (String)GetValue(SourceProperty); }
			set { SetValue(SourceProperty, value); }
		}

		protected static void Prop(DependencyObject imageObject, DependencyPropertyChangedEventArgs eventArgs) {
			Thumbnail i = imageObject as Thumbnail;
			if (i != null && eventArgs.NewValue != null && eventArgs.NewValue is string) {
				try {
					Image image = new Image();
					BitmapImage bi = new BitmapImage(new Uri((string)eventArgs.NewValue));
					image.Source = bi;
					i.Content = image;
				} catch (UriFormatException) {
					// leave thumbnail blank if image can't be loaded
					Debug.WriteLine("Fixed Image: can't load image:" + eventArgs.NewValue.ToString());
				}
			}
		}
	}
}
