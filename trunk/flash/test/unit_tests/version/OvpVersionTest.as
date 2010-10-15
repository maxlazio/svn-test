﻿package version {
	import asunit.framework.TestCase;
	import org.openvideoplayer.version.*;
	
	public class OvpVersionTest extends TestCase {
		
		// We need to change this with each release, but at least this verifies
		// the version info has been updated and the class is working correctly.
		private var currentVersion:String = "2.1.4";
		
		//--------------------------------------------------------------------------
		//
		// Construction, setup and tear down
		//
		//--------------------------------------------------------------------------
		
		public function OvpVersionTest(testMethod:String) {
			super(testMethod);
		}
		
		protected override function setUp():void {
		}
		
		protected override function tearDown():void {
		}
		
		//--------------------------------------------------------------------------
		//
		// Tests
		//
		//--------------------------------------------------------------------------
		
		public function testOvpVersion():void {
			assertEquals(currentVersion, OvpVersion.version);
		}
	}
}