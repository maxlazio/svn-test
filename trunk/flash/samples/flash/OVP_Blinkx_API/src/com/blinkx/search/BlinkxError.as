package com.blinkx.search
{
	public class BlinkxError 
	{
		public static const API_ERROR:uint 			= 1;
		
		protected var _num:uint;
		protected var _desc:String;
		
		public function BlinkxError(errorCode:uint,description:String)
		{
			_num = errorCode;
			_desc = description;
			
		}
		
		/**
		 * The error number for the error dispatched. This should be one of the public constants defined in this class.
		 */
		public function get errorNumber():uint { return _num; }
		
		/**
		 * The error description for the error dispatched.
		 */
		public function get errorDescription():String { return _desc; }
		
	}
}