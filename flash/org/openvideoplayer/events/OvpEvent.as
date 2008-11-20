// OvpEvent.as
//
// Copyright (c) 2008, the Open Video Player authors. All rights reserved.
//
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions are 
// met:
//
//    * Redistributions of source code must retain the above copyright 
//		notice, this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above 
//		copyright notice, this list of conditions and the following 
//		disclaimer in the documentation and/or other materials provided 
//		with the distribution.
//    * Neither the name of the openvideoplayer.org nor the names of its 
//		contributors may be used to endorse or promote products derived 
//		from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR 
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//

package org.openvideoplayer.events
{
	import flash.events.Event;

	/**
	 * The OvpEvent class is dispatched for all events in the Open Video Player code base.
	 * 
	 * The data property contains varying information depending on the specific event. For example, the <code>OvpEvent.BANDWIDTH</code> event
	 * provides bandwith and latency values via the data property:
	 * <listing>
	 * private function bandwidthHandler(e:OvpEvent):void {
	 * &#xA0;&#xA0;&#xA0;&#xA0;trace("Bandwidth measured at " + e.data.bandwidth+ " kbps and latency is " + e.data.latency + " ms.");
	 * }
	 * </listing>
	 */

	public class OvpEvent extends Event
	{
		/** 
		 * The OvpEvent.ERROR constant defines the value of an error event's
		 * <code>type</code> property, which indicates that the class
		 * has encountered a run-time error.
		 * 
		 * @see OvpError
		 */
  		public static const ERROR:String = "error";
  		
  		/**
  		 * The OvpEvent.TIMEOUT constant defines the value of an error event's
  		 * <code>type</code> property, which indicates a timeout has occurred.
  		 */
		public static const TIMEOUT:String = "timeout";

		/** 
		 * The OvpEvent.BANDWIDTH constant defines the value of the OvpEvent's
		 * <code>type</code> property, which indicates the class
		 * has completed a bandwidth estimate. The <code>bandwidth</code> and <code>latency</code>
		 * values can be accessed via the data property, for example <code>event.data.bandwidth</code>
		 * and <code>event.data.latency</code>
		 * 
		 * @see org.openvideoplayer.net.OvpConnection#detectBandwidth
		 */
  		public static const BANDWIDTH:String = "bandwidth";
  			 			
  		/** 
		 * The OvpEvent.END_OF_STREAM constant defines the value of the OvpEvent's
		 * <code>type</code> property, which indicates that the end of the stream has been reached. Note that determination
		 * of this end is based upon an analysis of the NetStream events  (specifically NetStream.Play.Stop
		 * followed by NetStream.Buffer.Empty). This method is used, and this event provided, in order for the class
		 * to be compatible with FCS 1.7x servers, which do not issue the NetStream.onPlayStatus.Complete event, as well as
		 * for progressive delivery of streams. For streaming delivery from FMS 2.5 or higher, the OvpEvent.COMPLETE
		 * should be used. This event is a more robust indicator that end of stream has been reached.
		 * 
		 */
  		public static const END_OF_STREAM:String = "end";
  			
  		/** 
		 * The OvpEvent.COMPLETE constant defines the value of the OvpEvent's
		 * <code>type</code> property, which indicates that the end of the stream has been reached. This event is dispatched
		 * whenever the <code>onPlayStatus</code> callback function of the underlying NetStream issues a <code>NetStream.Play.Complete</code> code.
		 * 
		 */
  		public static const COMPLETE:String = "complete";
  			
  		/** 
		 * The OvpEvent.STREAM_LENGTH constant defines the value of the OvpEvent's
		 * <code>type</code> property, which indicates the class
		 * has completed a stream length request. The <code>streamLength</code> value
		 * can be accessed via the data property, for example <code>event.data.streamLength</code>.
		 * 
		 * @see org.openvideoplayer.net.OvpConnection#getStreamLength
		 * @see org.openvideoplayer.net.OvpConnection#streamLengthAsTimeCode
		 * 
		 */
  		public static const STREAM_LENGTH:String = "streamlength";
  			  			
  		/** 
		 * The OvpEvent.PROGRESS constant defines the value of the OvpEvent's
		 * <code>type</code> property, which indicates the class is actively playing a stream. This event is fired
		 * every <code>progressInterval</code> millseconds from when <code>play</code> is first called, to when
		 * <code>close()</code> is called. This event is designed to be an update interval to drive time and position displays
		 * which vary with the progress of the stream. 

		 * 
		 * @see org.openvideoplayer.net.OvpNetStream#progressInterval
		 * @see org.openvideoplayer.net.OvpNetStream#close
		 */
  		public static const PROGRESS:String = "progress";
			
		/** 
		 * The OvpEvent.LOADED constant defines the value of the OvpEvent's
		 * <code>type</code> property, which indicates the class has successfully loaded external data, such
		 * as a MediaRSS feed. This event is fired before that data is parsed.
		 * 
		 */
  		public static const LOADED:String = "loaded";
  		
		/** 
		 * The OvpEvent.PARSED constant defines the value of the OvpEvent's
		 * <code>type</code> property, which indicates the class has successfully
		 * parsed some data, such as a Media RSS feed. 
		 * 
		 */
  		public static const PARSED:String = "parsed";
  		
  		/** 
		 * The OvpEvent.MP3_ID3 constant defines the value of an OvpEvent's
		 * <code>type</code> property, dispatched when the the class receives information about
		 * ID3 data embedded in an MP3 file. To trigger this event, first make a request to the
		 * <code>getMp3Id3Info</code> method.
		 * 
		 * @see org.openvideoplayer.net.OvpNetStream#getMp3Id3Info
		 */
		public static const MP3_ID3:String = "id3";
		 		
  		/** 
		 * The OvpEvent.NETSTREAM_METADATA constant defines the value of an OvpEvent's
		 * <code>type</code> property, dispatched when the OvpNetStream class receives descriptive
		 * information embedded in the FLV file being played. This event is triggered after a call to
		 * the <code>NetStream.play()</code> method, but before the video playhead has advanced.
		 * In many cases, the duration value embedded in FLV metadata approximates the actual duration,
		 * but is not exact. In other words, it does not always match the value of the NetStream.time property
		 * when the playhead is at the end of the video stream. 
		 */		 		
		public static const NETSTREAM_METADATA:String = "metadata";

  		/** 
		 * The OvpEvent.NETSTREAM_CUEPOINT constant defines the value of an OvpEvent's
		 * <code>type</code> property, dispatched when an embedded cue point is reached while playing an FLV file.
		 * You can use this handler to trigger actions in your code when the video reaches a specific cue point,
		 * which lets you synchronize other actions in your application with video playback events. 
		 * The following types of cue points can be embedded in an FLV file:
		 * <ul><li>A navigation cue point specifies a keyframe within the FLV file, and the cue point's time
		 * property corresponds to that exact keyframe. Navigation cue points are often used as bookmarks or entry
		 * points to let users navigate through the video file. </li>
		 * <li>An event cue point is specified by time, whether or not that time corresponds to a specific keyframe.
		 * An event cue point usually represents a time in the video when something happens that could be used to
		 * trigger other application events.</li></ul>.
		 */		
		public static const NETSTREAM_CUEPOINT:String = "cuepoint";
		
  		/** 
		 * The OvpEvent.NETSTREAM_IMAGEDATA constant defines the value of an OvpEvent's
		 * <code>type</code> property, dispatched when the OvpNetStream class receives an image embedded
		 * in the H.264 file being played. The onImageData method is a callback like
		 * onMetaData that sends image data as a byte array through an AMF0 data channel. The image data
		 * can be in JPEG, PNG, or GIF formats. As the information is a byte array, this functionality is
		 * only supported for ActionScript 3.0 client SWFs. The <code>info</code> property of the event will
		 * hold the image data object and the imageData.data object holds the actual byte array.
		 */		
		public static const NETSTREAM_IMAGEDATA:String = "imagedata";
		
  		/** 
		 * The OvpEvent.NETSTREAM_TEXTDATA constant defines the value of an OvpEvent's
		 * <code>type</code> property, dispatched when the OvpNetStream class receives text data embedded
		 * in the H.264 file being played. The onTextData method is a callback like onMetaData that sends text
		 * data through an AMF0 data channel. The text data is always in UTF-8 format and can contain additional
		 * information about formatting based on the 3GP timed text specification. This functionality is fully
		 * supported in ActionScript 2.0 and 3.0 because it does not use a byte array.The <code>info</code> property of the event will
		 * hold the text data object and the textData.text object holds the actual text.
		 */		
		public static const NETSTREAM_TEXTDATA:String = "textdata";
		
  		/** 
		 * The OvpEvent.NETSTREAM_PLAYSTATUS constant defines the value of an OvpEvent's
		 * <code>type</code> property, dispatched when a NetStream object has completely played a stream,
		 * or when it switches to a different stream in a server-side playlist.
		 * This handler returns information objects that provide information in addition to what's returned
		 * by the netStatus event. You can use this handler to trigger actions in your code when a NetStream
		 * object has switched from one stream to another stream in a playlist (as indicated by the information
		 * object NetStream.Play.Switch) or when a NetStream object has played to the end (as indicated by the
		 * information object NetStream.Play.Complete). 
		 */
		public static const NETSTREAM_PLAYSTATUS:String = "playstatus";
		 
		private var _data:Object;
		
		/**
		 * Returns the data for the event, which can differ based on the event type.
		 */
		public function get data():Object { return _data; }
		
		/**
		 * Constructor
		 */
		public function OvpEvent(type:String, data:Object=null)
		{
			_data = data;
			super(type);
		}
		
       /** 
        * Overrides the inherited clone() method.
        */
        override public function clone():Event {
            return new OvpEvent(type, data);
        }		
	}
}
