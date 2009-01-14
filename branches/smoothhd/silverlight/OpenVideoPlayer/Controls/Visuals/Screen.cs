using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace org.OpenVideoPlayer.Controls.Visuals {
	[ContentProperty("Child")]
	[TemplatePart(Name = Screen.ChildBorder, Type = typeof(Border))]
	public class Screen : Control {
		/// <summary>
		/// ChildBorder property string.
		/// </summary>
		private const string ChildBorder = "ChildBorder";

		private Border border;
		private Size sizeOriginal;

		private double scalingFactor = 1.0;
		private Size sizeActuallyNeeded;

		public Screen() {
			DefaultStyleKey = typeof(Screen);
		}

		public static readonly DependencyProperty ChildProperty = DependencyProperty.Register("Child", typeof(UIElement), typeof(Screen), new PropertyMetadata(new PropertyChangedCallback(Screen.OnChildPropertyChanged)));

		public UIElement Child {
			get { return GetValue(ChildProperty) as UIElement; }
			set { SetValue(ChildProperty, value); }
		}

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();
			border = GetTemplateChild(ChildBorder) as Border;
			sizeOriginal = new Size(0, 0);
			OnChildChanged();
		}

		private static void OnChildPropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs args) {
			(dobj as Screen).OnChildChanged();
		}

		private void OnChildChanged() {
			if (border != null) {
				border.Child = Child;
			}
		}

		protected override Size MeasureOverride(Size availableSize) {
		//	Debug.WriteLine("MeasureOverride(availableSize=" + availableSize.ToString() + " )");
			if (border != null) {
				if (sizeOriginal.Width == 0) // Make this measurement only once per instance
                {
					border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
					sizeOriginal = border.DesiredSize;
				}
				Size desiredSizeMax = sizeOriginal;

				border.Measure(availableSize);
				Size desiredSizeFit = border.DesiredSize;

				Double ScalingFactorX = 1.0;
				if (availableSize.Width < desiredSizeFit.Width) {
					ScalingFactorX = availableSize.Width / desiredSizeFit.Width;
				} else if (availableSize.Width < desiredSizeMax.Width) {
					ScalingFactorX = availableSize.Width / desiredSizeMax.Width;
				}

				Double ScalingFactorY = 1.0;
				if (availableSize.Height < desiredSizeFit.Height) {
					ScalingFactorY = availableSize.Height / desiredSizeFit.Height;
				} else if (availableSize.Height < desiredSizeMax.Height) {
					ScalingFactorY = availableSize.Height / desiredSizeMax.Height;
				}

				scalingFactor = Math.Min(ScalingFactorX, ScalingFactorY);

			//	Debug.WriteLine("ScalingFactorX:" + ScalingFactorX.ToString(CultureInfo.CurrentCulture));
			//	Debug.WriteLine("ScalingFactorY:" + ScalingFactorY.ToString(CultureInfo.CurrentCulture));
			//	Debug.WriteLine("scalingFactor:" + scalingFactor.ToString(CultureInfo.CurrentCulture));

				if (Double.IsPositiveInfinity(availableSize.Width) || Double.IsPositiveInfinity(availableSize.Height)) {
					sizeActuallyNeeded = new Size(desiredSizeFit.Width * scalingFactor, desiredSizeFit.Height * scalingFactor);
				} else {
					sizeActuallyNeeded = new Size(availableSize.Width / scalingFactor, availableSize.Height / scalingFactor);
				}
			}
		//	Debug.WriteLine("MeasureOverride(desiredSize=" + sizeActuallyNeeded.ToString() + " )");
			return sizeActuallyNeeded;
		}
		
		protected override Size ArrangeOverride(Size finalSize) {
		//	Debug.WriteLine("ArrangeOverride(finalSize=" + finalSize.ToString() + " )");

			if (border != null) {
				ScaleTransform st = new ScaleTransform();
				st.ScaleX = scalingFactor;
				st.ScaleY = scalingFactor;
				st.CenterX = 0;
				st.CenterY = 0;

				border.RenderTransform = st;

				border.Arrange(new Rect(0, 0, sizeActuallyNeeded.Width, sizeActuallyNeeded.Height));

			//	Debug.WriteLine("ArrangeOverride(scalingFactor=" + scalingFactor.ToString(CultureInfo.CurrentUICulture) + " )");

			}
			return finalSize;
		}
	}
}
