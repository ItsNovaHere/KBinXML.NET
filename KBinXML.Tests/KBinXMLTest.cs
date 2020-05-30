using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;
using static KBinXML.Converters;

// When trying to write performant code, resharper is a bitch.
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ForCanBeConvertedToForeach

namespace KBinXML.Tests {

	public class KBinXMLTest {
		private readonly ITestOutputHelper _testOutputHelper;
		public KBinXMLTest(ITestOutputHelper testOutputHelper) {
			_testOutputHelper = testOutputHelper;
		}

		[Fact]
		public void KBinReader_GetXML_ReturnsValidXML() {
			var sw = new Stopwatch();
			sw.Start();
			
			var kbin = new KBinReader(File.ReadAllBytes(@"test.kbin"));
			
			sw.Stop();
			
			_testOutputHelper.WriteLine(sw.Elapsed.ToString());
			_testOutputHelper.WriteLine(kbin.Document.ToString());
		}

		[Fact]
		public void KBinWriter_Document_ReturnsByteArray() {
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			
			var kbin = new KBinWriter(XDocument.Load(@"testcases.xml"));
			var outKbin = new KBinReader(kbin.Document);
			_testOutputHelper.WriteLine(outKbin.Document.ToString());
		}

		[Fact]
		public void Sixbit() {
			var test = "ABCdef0123_test";
			var encoded = KBinXML.Sixbit.Encode(test);
			var decoded = KBinXML.Sixbit.Decode(new ByteBuffer(encoded));
			
			_testOutputHelper.WriteLine(test);
			_testOutputHelper.WriteLine(decoded);
		}
		
		[Fact]
		public void Converter_IP4ToString_SpeedTest() {
			var data = new byte[] {0x00, 0x00, 0x00, 0x0E};
			var sw = new Stopwatch();
			sw.Start();

			var ret = IP4ToString(data);
			sw.Stop();
			
			Assert.True(sw.ElapsedTicks < 3000);
			Assert.Equal(data, IP4FromString(ret));
		}

		[Fact]
		public void Converter_IP4FromString_SpeedTest() {
			const string data = "0.0.0.14";
			var sw = new Stopwatch();
			sw.Start();

			var ret = IP4FromString(data);
			sw.Stop();
			
			Assert.True(sw.ElapsedTicks < 3000);
			Assert.Equal(data, IP4ToString(ret));
		}
	}

}