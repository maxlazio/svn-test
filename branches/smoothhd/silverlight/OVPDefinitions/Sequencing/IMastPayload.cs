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

namespace org.OpenVideoPlayer.Media {
	public interface IMastPayload {
		//IMastTrigger Trigger { get; set; }
		string TriggerId { get; set; }
		IMastSource Source { get; set; }
		object Payload { get; set; }

		void Deactivate();
		void Activate();

		event EventHandler Activated;
		event EventHandler Deactivated;
	}
}
