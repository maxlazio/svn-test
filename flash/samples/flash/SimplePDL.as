﻿package {
	import flash.net.NetConnection;
	import flash.display.MovieClip;
	import flash.media.Video;
	import flash.events.*;
	
	import org.openvideoplayer.net.*;
	import org.openvideoplayer.events.*;
	
	public class SimplePDL extends MovieClip {
		private var _nc:OvpConnection;
		private var _ns:OvpNetStream;
		private var _filename:String = new String("http://products.edgeboss.net/download/products/content/demo/video/oomt/elephants_dream_700k.flv");

		// Constructor
		public function SimplePDL() {
			// Create the connection object and add the necessary event listeners
			_nc = new OvpConnection()
			_nc.addEventListener(OvpEvent.ERROR, errorHandler);
			_nc.addEventListener(NetStatusEvent.NET_STATUS, netStatusHandler);
			_nc.connect(null);
		}
		
		// This method is called from the netStatusHandler below when we receive a good connection		
		private function connectedHandler():void {
			trace("Successfully connected to: " + _nc.netConnection.uri);

			// Instantiate an OvpNetStream object
			_ns = new OvpNetStream(_nc);
			
			// Add the necessary listeners
			_ns.addEventListener(NetStatusEvent.NET_STATUS, streamStatusHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_PLAYSTATUS, streamPlayStatusHandler);
			_ns.addEventListener(OvpEvent.NETSTREAM_METADATA, metadataHandler);
			_ns.addEventListener(OvpEvent.STREAM_LENGTH, streamLengthHandler); 

			// Give the video symbol on stage our net stream object
			_video.attachNetStream(_ns);
   			_ns.play(_filename);
		}		
			
		// Handles all OvpEvent.ERROR events
		private function errorHandler(e:OvpEvent):void {
			trace("Error #" + e.data.errorNumber+": " + e.data.errorDescription, "ERROR");
		}
		
		// Handles the stream length event fired by the OvpNetStream class when "duration" is found in the metadata for the FLV file
		private function streamLengthHandler(e:OvpEvent):void {
			trace("Stream length is " + e.data.streamLength);
		}
		
		// Handles NetStatusEvent.NET_STATUS events fired by the OvpConnection class
		private function netStatusHandler(e:NetStatusEvent):void {
			trace(e.info.code);
			switch (e.info.code) {
				case "NetConnection.Connect.Rejected":
					trace("Rejected by server. Reason is "+e.info.description);
					break;
				case "NetConnection.Connect.Success":
					connectedHandler();
					break;
			}
		}
		
		// Handles the NetStatusEvent.NET_STATUS events fired by the OvpNetStream class			
		private function streamStatusHandler(e:NetStatusEvent):void {
			trace("streamStatusHandler() - event.info.code="+e.info.code);
		}

		// Handles the OvpEvent.NETSTREAM_PLAYSTATUS events fired by the OvpNetStream class
		private function streamPlayStatusHandler(e:OvpEvent):void {				
			trace(e.data.code);
		}
			
		// Handles the OvpEvent.NETSTREAM_METADATA events fired by the OvpNetStream class	
		private function metadataHandler(e:OvpEvent):void {
			for (var propName:String in e.data) {
				trace("metadata: "+propName+" = "+e.data[propName]);
			}
		}
	}
}

