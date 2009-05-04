﻿package {
	import asunit.framework.TestSuite;
	import rss.AkamaiMediaRSSTest;
	import utilities.DateUtilTest;
	import net.*;
	
	public class AllTests extends TestSuite {
		public function AllTests() {
			super();
			
			// OVP connection tests
			addTest(new OvpConnectionProgTest("testInstantiated"));
			addTest(new OvpConnectionProgTest("testConnect"));
			
			addTest(new OvpConnectionStreamTest("testInstantiated"));
			addTest(new OvpConnectionStreamTest("testConnect"));
			
			addTest(new OvpConnectionBandwidthTest("testInstantiated"));
			addTest(new OvpConnectionBandwidthTest("testBandwidth"));
			
			addTest(new OvpConnectionStreamLengthTest("testInstantiated"));
			addTest(new OvpConnectionStreamLengthTest("testStreamLength"));
			
			// Akamai connection tests
			addTest(new AkamaiConnectionProgTest("testConnect"));
			addTest(new AkamaiConnectionStreamTest("testConnect"));
			
			// utility tests
			addTest(new DateUtilTest("testInstantiated"));
			addTest(new DateUtilTest("testCompareTodayWithTimestampEarlier"));
			addTest(new DateUtilTest("testCompareTodayWithTimestampLater"));
			addTest(new DateUtilTest("testCompareWithTimestampEqual"));
			addTest(new DateUtilTest("testCompareTodayWithTimestampEarlier2"));
			addTest(new DateUtilTest("testCompareTodayWithTimestampLater2"));
			addTest(new DateUtilTest("testCompareWithTimestampEqual2"));
			addTest(new DateUtilTest("testCompareTodayWithTimestampEarlier3"));
			addTest(new DateUtilTest("testCompareTodayWithTimestampLater3"));
			addTest(new DateUtilTest("testCompareWithTimestampEqual3"));
			
			// rss tests
			addTest(new AkamaiMediaRSSTest("testInstantiated"));
			addTest(new AkamaiMediaRSSTest("testFilterTextSimple"));
			addTest(new AkamaiMediaRSSTest("testFilterTextSimpleWithWhiteSpace"));
			addTest(new AkamaiMediaRSSTest("testFilterTextTitleAny"));
			addTest(new AkamaiMediaRSSTest("testFilterTextTitleAll"));			
			addTest(new AkamaiMediaRSSTest("testFilterTextDescriptionAny"));
			addTest(new AkamaiMediaRSSTest("testFilterTextDescriptionAny3"));
			addTest(new AkamaiMediaRSSTest("testFilterTextDescriptionAll"));
			addTest(new AkamaiMediaRSSTest("testFilterTextDescriptionAllWhiteSpace"));
			addTest(new AkamaiMediaRSSTest("testFilterDateSimpleLEQNow"));
			addTest(new AkamaiMediaRSSTest("testFilterDateSimpleGEQNow"));
			addTest(new AkamaiMediaRSSTest("testfilterDateSameDayNoTimestampLEQ"));
			addTest(new AkamaiMediaRSSTest("testfilterDateSameDayNoTimestampGEQ"));
			addTest(new AkamaiMediaRSSTest("testfilterTextAndDateSameDayNoTimestampLEQ"));
			addTest(new AkamaiMediaRSSTest("testfilterTextAndDateSameDayNoTimestampLEQ2"));
			addTest(new AkamaiMediaRSSTest("testfilterTextAndDateSameDayNoTimestampGEQ"));
			addTest(new AkamaiMediaRSSTest("testfilterTextAndDateSameDayNoTimestampGEQ2"));
			addTest(new AkamaiMediaRSSTest("testfilterTextAndDateSameDayNoTimestampGEQ3"));
			addTest(new AkamaiMediaRSSTest("testfilterTextAndDateNowGEQ"));

		}
	}
}