using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Threading;

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
		private FrameworkElement Download;

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
			Download = GetTemplateChild("Download") as FrameworkElement;

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
			if (horizontalThumb != null) {
				horizontalThumb.MouseMove += new MouseEventHandler(horizontalThumb_MouseMove);
			}
			MouseMove += new MouseEventHandler(ScrubberBar_MouseMove);
			this.SizeChanged += new SizeChangedEventHandler(ScrubberBar_SizeChanged);
		}

		void ScrubberBar_SizeChanged(object sender, SizeChangedEventArgs e) {
			if (Download != null && downloadPercent > 0) Download.Width = ActualWidth * downloadPercent;
		}

		public TimeSpan Throttle = TimeSpan.Zero;
		private System.Threading.Timer t;// = new System.Threading.Timer();

		DateTime lastMove = DateTime.MinValue;
		private readonly object tLock = new object();
		MouseEventArgs lastArgs = null;

		void ScrubberBar_MouseMove(object sender, MouseEventArgs e) {
			try {
				lastArgs = e;

				if (Throttle == TimeSpan.Zero) {
					TimerTick(lastArgs);
					return;
				}

				if (t == null) {
					t = new System.Threading.Timer(TimerTick, lastArgs, Throttle, TimeSpan.FromMilliseconds(-1));
				} else {
					if (DateTime.Now - lastMove > Throttle) {
						TimerTick(lastArgs);
						t.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
					} else {
						t.Change(Throttle, TimeSpan.FromMilliseconds(-1));
					}
				}
				
			} catch (Exception ex) {
				Console.WriteLine("Mousemove issue " + ex);
			}
		}

		private double downloadPercent;

		public double DownloadPercent {
			get { return downloadPercent; }
			set { 
				if(value > 1) value = 1;
				if(value < 0) value = 0;
				if(value!=downloadPercent) {
					downloadPercent = value;
					if (Download != null) Download.Width = ActualWidth * downloadPercent;
				}
			}
		}

		void TimerTick(object o) {
			if (!Monitor.TryEnter(tLock)) return;
			try {
				MouseEventArgs e = o as MouseEventArgs;
				if (MouseOver != null) {
					Point pt = e.GetPosition(this);
					double time = GetTimeValue(pt);
					MouseOver(this, new ScrubberBarValueChangeArgs(time, e, (mouseIsDown))); // && throttle)));
				//	Debug.WriteLine("Move : " + mouseIsDown);
				}
			} catch (Exception ex) {
				Console.WriteLine("Mousemove issue " + ex);
			} finally {
				Monitor.Exit(tLock);
				t.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
			}
		}

		void horizontalThumb_MouseMove(object sender, MouseEventArgs e) {
		}

		void TrackMouseMove(object sender, MouseEventArgs e) {
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
