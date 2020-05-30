using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ForCanBeConvertedToForeach

namespace KBinXML {

	public class KBinReader {
		public XDocument Document { get; }
		
		static KBinReader() {
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}

		public KBinReader(byte[] data) {
			var document = new XDocument();
			var node = document.Root;
			
			var nodeBuffer = new ByteBuffer(data);
			Assert(nodeBuffer.GetU8(), 0xA0);
			var compressedValue = nodeBuffer.GetU8();
			if (compressedValue != 0x42 && compressedValue != 0x45) {
				throw new Exception();
			}

			var compressed = compressedValue == 0x42;

			var encodingIndex = nodeBuffer.GetU8();
			var encodingCheck = nodeBuffer.GetU8();
			Assert(encodingCheck, (byte) (encodingIndex ^ 0xFF));

			var encoding = Encodings[encodingIndex >> 5];
			var nodeEnd = (int) nodeBuffer.GetU32() + 8;

			var dataBuffer = new ByteBuffer(data, nodeEnd);
			var dataSize = dataBuffer.GetU32();

			var dataByteBuffer = new ByteBuffer(data, nodeEnd);
			var dataWordBuffer = new ByteBuffer(data, nodeEnd);

			document.Declaration = new XDeclaration("1.0", encoding.WebName, "");

			var nodesLeft = true;
			while (nodesLeft) {
				while (nodeBuffer.PeekU8() == 0) nodeBuffer.GetU8();

				var nodeType = nodeBuffer.GetU8();
				var isArray = nodeType >> 6 == 1;
				nodeType &= 0b10111111;

				Formats.TryGetValue(nodeType, out var format);

				var name = "";
				if (nodeType != (byte) Control.NodeEnd && nodeType != (byte) Control.SectionEnd) {
					if (compressed) {
						name = Sixbit.Decode(nodeBuffer);
					} else {
						var length = (nodeBuffer.GetU8() & 0b10111111) + 1;
						name = encoding.GetString(nodeBuffer.GetBytes(length)).Trim('\0');
					}
				}
				
				var skip = true;

				if (nodeType == (byte) Control.Attribute) {
					var readDataAuto = ReadDataAuto(dataBuffer);
					Array.Reverse(readDataAuto);

					var value = encoding.GetString(readDataAuto).Trim('\0');
					node?.SetAttributeValue(name, value);
				} else if (nodeType == (byte) Control.NodeEnd) {
					if (node?.Parent != null) {
						node = node.Parent;
					}
				} else if (nodeType == (byte) Control.SectionEnd) {
					nodesLeft = false;
				} else {
					skip = false;
				}

				if (skip) continue;

				var child = new XElement(name);
				node?.Add(child);
				node = child;

				if (nodeType == (byte) Control.NodeStart) continue;

				node.SetAttributeValue("__type", format.Name);

				var count = format.Count;
				var arrayCount = 1;
				
				if (count == -1) {
					count = (int) dataBuffer.GetU32();
					isArray = true;
				} else if(isArray) {
					arrayCount = (int) (dataBuffer.GetU32() / (format.Size * count));
					node.SetAttributeValue("__count", arrayCount);
				}

				var totalCount = arrayCount * count;

				byte[] nodeData;
				if (isArray) {
					nodeData = dataBuffer.GetBytes(totalCount);
					dataBuffer.RealignRead();
				} else {
					nodeData = ReadDataAligned(dataBuffer, dataByteBuffer, dataWordBuffer, format.Size, format.Count);
				}

				var text = "";
				if (format.Equals(Format.Binary)) {
					node.SetAttributeValue("__size", totalCount);
					
					Array.Reverse(nodeData);
					for (var i = 0; i < nodeData.Length; i++) {
						text += nodeData[i].ToString("x2");
					}
				} else if (format.Equals(Format.String)) {
					Array.Reverse(nodeData);
					text = encoding.GetString(nodeData);
				} else if (isArray) {
					for (var i = 0; i < arrayCount; i++) {
						text = format.FormatToString(nodeData[(i * count)..((i + 1) * count)]) + " " + text;
					}
				} else {
					text = format.FormatToString(nodeData);
				}

				node.Value = text.Trim('\0').Trim();
			}
			
			document.Add(node);
			Document = document;
		}

		private static byte[] ReadDataAligned(ByteBuffer data, ByteBuffer dataByte, ByteBuffer dataWord, int size, int count) {
			if (dataByte.Offset % 4 == 0) dataByte.Offset = data.Offset;
			if (dataWord.Offset % 4 == 0) dataWord.Offset = data.Offset;
			
			var totalSize = size * count;
			byte[] ret;
			
			switch (totalSize) {
				case 1:
					ret = dataByte.GetBytes(totalSize);
					break;
				case 2:
					ret = dataWord.GetBytes(totalSize);
					break;
				default:
					ret = data.GetBytes(totalSize);
					data.RealignRead();
					break;
			}

			var trailing = Math.Max(dataByte.Offset, dataWord.Offset);
			if (data.Offset < trailing) {
				data.Offset = trailing;
				data.RealignRead();
			}

			return ret;
		}

		private static byte[] ReadDataAuto(ByteBuffer data) {
			var size = data.GetS32();
			var ret =  data.GetBytes(size);
			data.RealignRead();
			return ret;
		}
		
		public static Encoding[] Encodings => new[] {
			Encoding.GetEncoding(932), //SHIFT-JIS
			Encoding.GetEncoding(20127), //ASCII
			Encoding.GetEncoding(28591), //ISO-8859-1
			Encoding.GetEncoding(51932), //EUC-JP
			Encoding.GetEncoding(932), //SHIFT-JIS - Again, because thanks konmai!
			Encoding.GetEncoding(65001) //UTF-8
		};

		internal static Dictionary<int, Format> Formats => new Dictionary<int, Format> {
			{1, Format.Void},
			{2, Format.S8},
			{3, Format.U8},
			{4, Format.S16},
			{5, Format.U16},
			{6, Format.S32},
			{7, Format.U32},
			{8, Format.S64},
			{9, Format.U64},
			{10, Format.Binary},
			{11, Format.String},
			{12, Format.IP4},
			{14, Format.Float},
			{15, Format.Double},
			{16, Format.S8 * 2},
			{17, Format.U8 * 2},
			{18, Format.S16 * 2},
			{19, Format.U16 * 2},
			{20, Format.S32 * 2},
			{21, Format.U32 * 2},
			{22, (Format.S64 * 2).WithAlias("vs64")},
			{23, (Format.U64 * 2).WithAlias("vu64")},
			{24, (Format.Float * 2).Rename("2f")},
			{25, (Format.Double * 2).Rename("2d").WithAlias("vd")},
			{26, Format.S8 * 3},
			{27, Format.U8 * 3},
			{28, Format.S16 * 3},
			{29, Format.U16 * 3},
			{30, Format.S32 * 3},
			{31, Format.U32 * 3},
			{32, Format.S64 * 3},
			{33, Format.U64 * 3},
			{34, (Format.Float * 3).Rename("3f")},
			{35, (Format.Double * 3).Rename("3d")},
			{36, Format.S8 * 4},
			{37, Format.U8 * 4},
			{38, Format.S16 * 4},
			{39, Format.U16 * 4},
			{40, (Format.S32 * 4).WithAlias("vs32")},
			{41, (Format.U32 * 4).WithAlias("vu32")},
			{42, Format.S64 * 4},
			{43, Format.U64 * 4},
			{44, (Format.Float * 4).Rename("4f").WithAlias("vf")},
			{45, (Format.Double * 4).Rename("4d")},
			{48, (Format.S8 * 16).WithAlias("vs8")},
			{49, (Format.U8 * 16).WithAlias("vu8")},
			{50, (Format.S16 * 8).WithAlias("vs16")},
			{51, (Format.U16 * 8).WithAlias("vu16")},
			{52, Format.Bool},
			{53, (Format.Bool * 2).Rename("2b")},
			{54, (Format.Bool * 3).Rename("3b")},
			{55, (Format.Bool * 4).Rename("4b")},
			{56, (Format.Bool * 16).Rename("vb")},
		};

		internal enum Control : byte {
			NodeStart = 1,
			Attribute = 46,
			NodeEnd = 190,
			SectionEnd = 191
		}
		
		private static void Assert(byte actual, byte expected) {
			if (actual != expected) throw new Exception();
		}
	}

}