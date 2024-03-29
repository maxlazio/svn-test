﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using org.OpenVideoPlayer.Util;
using System.Windows.Shapes;
using System.Reflection;

namespace org.OpenVideoPlayer.Controls.Visuals {

	public class QualityGauge : ControlBase {

		public QualityGauge() {
			//DefaultStyleKey = GetType();
			////InitializeComponent();
			this.Tag = "";
		}

		//public event RoutedEventHandler ItemCheckedChanged;
		//public event RoutedEventHandler ItemClick;

		public SolidColorBrush Red = new SolidColorBrush(Color.FromArgb(255, 255, 175, 175));
		public SolidColorBrush Yellow = new SolidColorBrush(Color.FromArgb(255, 255, 255, 140));
		public SolidColorBrush White = new SolidColorBrush(Colors.White);
		public SolidColorBrush Green = new SolidColorBrush(Color.FromArgb(255, 175, 255, 175));

		internal Panel layoutRoot;
		Grid g;

		private int levels;
		public int Levels {
			get { return levels; }
			set {
				//Grid g = layoutRoot as Grid;
				
				if (levels != value || (g!=null && ((Vertical && g.RowDefinitions.Count!=value) || g.ColumnDefinitions.Count!=value ))) {
					levels = value;
					if (layoutRoot != null) {
						SetLevels();
					}
				}
			}
		}

		public static DependencyProperty LevelsProperty = DependencyProperty.Register("Levels", typeof (int), typeof(QualityGauge), null);

		public bool Vertical { get; set; }
		public static DependencyProperty VerticalProperty = DependencyProperty.Register("Vertical", typeof(bool), typeof(QualityGauge), null);

		private double thickness;
		public double Thickness {
			get { return thickness; }
			set {
				if (thickness != value) {
					thickness = value;
					if (layoutRoot != null) {
						SetLevels();
					}
				}
			}
		}

		public static DependencyProperty ThicknessProperty = DependencyProperty.Register("Thickness", typeof(double), typeof(QualityGauge), null);

		private double gridThickness;
		public double GridThickness {
			get { return gridThickness; }
			set {
				if (gridThickness != value) {
					gridThickness = value;
					if (layoutRoot != null) {
						SetLevels();
					}
				}
			}
		}

		public static DependencyProperty GridThicknessProperty = DependencyProperty.Register("GridThickness", typeof(double), typeof(QualityGauge), null);

		private double value = 0.5;

		public Double Value {
			get { return value; }
			set {
				if (value == this.value) return;
				this.value = value;
				int f = (int)(Levels * value);			
				//Grid g = ((Grid) layoutRoot);

				foreach (UIElement e in g.Children) {
					Path p = e as Path;
					if (p == null) continue;

					p.Stroke = Foreground; //(f <  new SolidColorBrush(Colors.White);
					int level = (Vertical) ? (int) p.GetValue(Grid.RowProperty) : (Levels-1-(int)p.GetValue(Grid.ColumnProperty));
					if (level == Levels - 1) continue;
					p.Visibility = (level < Levels - f) ? Visibility.Collapsed : Visibility.Visible;
				}
				if (levels == 1) {
					//g.Background = Foreground;
					if (icon != null) {
						icon.Visibility = Visibility.Visible;
					} else {
						g.Background = Foreground;
					}
				} else {
					if (icon != null) {
						icon.Visibility = Visibility.Collapsed;
					} else {
						g.Background = Background;
					}
				}
			}
		}

		public new Brush Foreground {
			get { return base.Foreground; }
			set {
				//Grid g = ((Grid) layoutRoot);
				if (g == null || Foreground == value) return;
				//foreach (Path p in g.Children) {
				//    Storyboard sb = new Storyboard() { BeginTime = TimeSpan.Zero, Duration = new Duration(TimeSpan.FromSeconds(1)) };
				//    ColorAnimation ca = new ColorAnimation() { BeginTime= TimeSpan.Zero,  Duration = new Duration(TimeSpan.FromSeconds(1)), From = ((SolidColorBrush)Foreground).Color, To = ((SolidColorBrush)value).Color };
				//    sb.Children.Add(ca);
				//    Storyboard.SetTarget(ca, p);
				//    Storyboard.SetTargetProperty(ca, new PropertyPath("(Path.Stroke).(SolidColorBrush.Color)"));
				//    sb.Begin();
				//}
				base.Foreground = value; 
				Value = this.value;

			}
		}

		private Brush highlight = new SolidColorBrush(Color.FromArgb(255, 33, 33, 33));
		public Brush Highlight {
			get { return highlight; }
			set { highlight = value; }
		}
		public static DependencyProperty HighlightProperty = DependencyProperty.Register("Highlight", typeof(Brush), typeof(QualityGauge), new PropertyMetadata(null));

		protected FrameworkElement icon;

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();
			layoutRoot = GetTemplateChild("layoutRoot") as Panel;
			icon = GetTemplateChild("icon") as FrameworkElement;
			//BindTemplate(this, GetTemplateChild);
			g = layoutRoot as Grid;
			SetLevels();

			Value = value;
		}

		private void SetLevels() {
			Grid g = ((Grid) layoutRoot);
			Color fc = Colors.White;
			Color st = Colors.White;

			g.RowDefinitions.Clear();
			g.ColumnDefinitions.Clear();
			g.Children.Clear();
			if (icon != null) g.Children.Add(icon);

			if (levels == 1) {
				if (icon != null) {
					icon.Visibility = Visibility.Visible;
				} else {
					g.Background = new SolidColorBrush(fc);
				}
			
			//g.Margin = new Thickness(3);

			} else {
				if (icon != null) {
					icon.Visibility = Visibility.Collapsed;
				} else {
					g.Background = Background;
				}

				g.Margin = new Thickness(0);
				for (int i = 0; i < Levels; i++) {
					if (Vertical) {
						g.RowDefinitions.Add(new RowDefinition {Height = new GridLength(GridThickness)});
						Path p = XamlReader.Load(string.Format(@"<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' "
						                                       + @"Margin='2,0,2,0' Width='Auto' StrokeThickness='{2}' Grid.Row='{0}' Height='{2}'  Stretch='Fill'  Stroke='{1}' Data='M 0,0L 8,0' />", i, fc, Thickness)) as Path;
						g.Children.Add(p);
					} else {
						g.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(GridThickness)});
						Path p = XamlReader.Load(string.Format(@"<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' "
						                                       + @"Margin='0,0,0,0' Width='{2}' StrokeThickness='{2}' Grid.Column='{0}' Height='Auto'  Stretch='Fill'  Stroke='{1}' Data='M 0,0L 0,8' />", i, fc, Thickness)) as Path;
						g.Children.Add(p);
					}
				}
			}
		}

		/// <summary>
		/// Binds all the protected properties of the object into the template
		/// </summary>
		//public static void BindTemplate(Control sender, org.OpenVideoPlayer.Util.ControlHelper.GetChildDlg dlg) {
		//    //use reflection to eliminate all that biolerplate binding code.
		//    //NOTE - field names must match element names in the xaml for binding to work!
		//    FieldInfo[] fields = sender.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

		//    foreach (FieldInfo fi in fields) {
		//        if ((fi.FieldType.Equals(typeof(FrameworkElement)) || fi.FieldType.IsSubclassOf(typeof(FrameworkElement))) && fi.GetValue(sender) == null) {
		//            //object o = sender.GetTemplateChild(fi.Name);
		//            object o = dlg(fi.Name);
		//            if (o != null && (o.GetType().Equals(fi.FieldType) || o.GetType().IsSubclassOf(fi.FieldType))) {
		//                fi.SetValue(sender, o);
		//            } else {
		//                Debug.WriteLine(string.Format("No template match for: {0}, {1}", fi.Name, fi.FieldType));
		//            }
		//        }
		//    }
		//}

	}
}