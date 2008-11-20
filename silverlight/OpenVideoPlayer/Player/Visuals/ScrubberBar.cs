using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace org.OpenVideoPlayer.Player.Visuals {
	public class ScrubberBar : Slider {

		private FrameworkElement horizontalThumb;
		private FrameworkElement leftTrack;
		private FrameworkElement rightTrack;
		private FrameworkElement upTrack;
		private FrameworkElement downTrack;

		public event EventHandler<ScrubberBarValueChangeArgs> ValueChangeRequest;
		public event EventHandler<ScrubberBarValueChangeArgs> MouseOver;

		protected bool mouseIsDown;

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();

			horizontalThumb = GetTemplateChild("HorizontalThumb") as FrameworkElement;
			leftTrack = GetTemplateChild("LeftTrack") as FrameworkElement;
			rightTrack = GetTemplateChild("RightTrack") as FrameworkElement;
			upTrack = GetTemplateChild("UpTrack") as FrameworkElement;
			downTrack = GetTemplateChild("DownTrack") as FrameworkElement;

			if (leftTrack != null) {
				leftTrack.MouseLeftButtonDown += OnMoveThumbToMouseHorizontal;
			}
			if (rightTrack != null) {
				rightTrack.MouseLeftButtonDown += OnMoveThumbToMouseHorizontal;
			}
			if (upTrack != null) {
				upTrack.MouseLeftButtonDown += OnMoveThumbToMouseVertical;
			}
			if (downTrack != null) {
				downTrack.MouseLeftButtonDown += OnMoveThumbToMouseVertical;
			}
		}

		private void OnMoveThumbToMouseHorizontal(object sender, MouseButtonEventArgs args) {
			if (ValueChangeRequest != null) {
				Point pt = args.GetPosition(this);
				double time = GetTimeValue(pt);
				ValueChangeRequest(this, new ScrubberBarValueChangeArgs(time, args));
			}
		}

		private void OnMoveThumbToMouseVertical(object sender, MouseButtonEventArgs args) {
			if (ValueChangeRequest != null) {
				Point pt = args.GetPosition(this);
				double time = GetTimeValue(pt);
				ValueChangeRequest(this, new ScrubberBarValueChangeArgs(time, args));
			}
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
			base.OnMouseLeftButtonDown(e);
			mouseIsDown = true;
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
			base.OnMouseLeftButtonUp(e);
			mouseIsDown = false;
		}

		protected override void OnMouseLeave(MouseEventArgs e) {
			base.OnMouseLeave(e);
			mouseIsDown = false;
		}

		protected override void OnMouseMove(MouseEventArgs args) {
			try {
				if (MouseOver != null) {
					Point pt = args.GetPosition(this);
					double time = GetTimeValue(pt);
					MouseOver(this, new ScrubberBarValueChangeArgs(time, args, mouseIsDown));
				}
			}
			catch (Exception ex) {
				Console.WriteLine("Mousemove issue " + ex);
			}
		}

		private double GetTimeValue(Point mousePosition) {
			return (((mousePosition.X - (horizontalThumb.ActualWidth/2))/(ActualWidth - horizontalThumb.ActualWidth)*(Maximum - Minimum)) + Minimum);
		}
	}
}
