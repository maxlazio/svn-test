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
using System.Collections.Generic;
using org.OpenVideoPlayer.Advertising.VPAID;

namespace org.OpenVideoPlayer {

	public interface IMastTrigger {
		string id { get; set; }
		string description { get; set; }

		List<IMastSource> Sources { get;  }
	}

	public interface IMastSource {
		string uri { get; set; }
		string altReference { get; set; }
		string format { get; set; }
		//bool allowskip { get; set; }

		List<IMastTarget> Targets { get; }
		List<IMastSource> Sources { get; }
	}

	public interface IMastTarget {
		string region { get; set; }
		string type { get; set; }
		//TriggerBehavior behavior { get; set; }

		List<IMastTarget> Targets { get; }

		object Instance { get; set; }
		//IVPAID Ad { get; set; }
	}

	public class BasicTarget : IMastTarget {
		public string region {get;set;}
		public string type {get;set;}
		public object Instance {get;set;}

		public List<IMastTarget> Targets {get { return null; }}
	}

	//[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://openvideoplayer.sf.net/mast")]
	//public enum TriggerBehavior {
	//    /// <remarks/>
	//    replace,
	//    /// <remarks/>
	//    combine,
	//    /// <remarks/>
	//    bypass,
	//    /// <remarks/>
	//    stack,
	//}
}
