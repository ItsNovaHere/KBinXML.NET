using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

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
			
			var kbin = new KBinReader(File.ReadAllBytes(@"testcases_out.kbin"));
			
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
			const string test = "ABCdef0123_test";
			var encoded = KBinXML.Sixbit.Encode(test);
			var decoded = KBinXML.Sixbit.Decode(new ByteBuffer(encoded));
			
			_testOutputHelper.WriteLine(test);
			_testOutputHelper.WriteLine(decoded);
			
			Assert.Equal(test, decoded);
		}
	}

}