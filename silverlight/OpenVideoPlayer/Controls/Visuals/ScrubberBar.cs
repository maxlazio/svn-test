using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace org.OpenVideoPlayer.Controls.Visuals {
	public class ScrubberBar : Slider {

		public ScrubberBar() {
			DefaultStyleKey = GetType();
		}

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
				leftTrack.MouseMove += new MouseEventHandler(TrackMouseMove);
			}
			if (rightTrack != null) {
				rightTrack.MouseLeftButtonDown += OnMoveThumbToMouseHorizontal;
				rightTrack.MouseMove += new MouseEventHandler(TrackMouseMove);
			}
			if (upTrack != null) {
				upTrack.MouseLeftButtonDown += OnMoveThumbToMouseVertical;
			}
			if (downTrack != null) {
				downTrack.MouseLeftButtonDown += OnMoveThumbToMouseVertical;
			}
		}

		void TrackMouseMove(object sender, MouseEventArgs e) {
			try {
				if (MouseOver != null) {
					Point pt = e.GetPosition(this);
					double time = GetTimeValue(pt);
					MouseOver(this, new ScrubberBarValueChangeArgs(time, e, mouseIsDown));
				}
			} catch (Exception ex) {
				Console.WriteLine("Mousemove issue " + ex);
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

		private double GetTimeValue(Point mousePosition) {
			return (((mousePosition.X - (horizontalThumb.ActualWidth/2))/(ActualWidth - horizontalThumb.ActualWidth)*(Maximum - Minimum)) + Minimum);
		}
	}

	public class ScrubberBarValueChangeArgs : EventArgs {
		public ScrubberBarValueChangeArgs(Double dblValue, MouseEventArgs args)// Point mousePosition)
			: base() {
			Value = dblValue;
			MouseArgs = args;
		}

		public ScrubberBarValueChangeArgs(Double dblValue, MouseEventArgs args, bool pressed)// Point mousePosition)
			: base() {
			Value = dblValue;
			MouseArgs = args;
			MousePressed = pressed;
		}

		public Double Value { get; private set; }
		public MouseEventArgs MouseArgs { get; private set; }
		public bool MousePressed { get; private set; }
	}
}
