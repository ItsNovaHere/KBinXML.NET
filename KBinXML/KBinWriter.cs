using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace KBinXML {

	public class KBinWriter {
		private readonly bool _compressed;
		private readonly Encoding _encoding;
		private readonly ByteBuffer _nodeBuffer;
		private readonly ByteBuffer _dataBuffer;
		internal static Encoding[] Encodings => KBinReader.Encodings;
		internal static Dictionary<int, Format> Formats = KBinReader.Formats;
		public byte[] Document;

		public KBinWriter(XDocument document, Encoding encoding = default, bool compressed = true) {
			_compressed = compressed;
			_encoding = encoding ?? Encodings[0];
			
			var header = new ByteBuffer();
			header.AppendU8(0xA0);
			header.AppendU8((byte) (_compressed ? 0x42 : 0x45));

			var encodingIndex = Array.FindIndex(Encodings, x => x.CodePage == _encoding.CodePage) << 5;
			header.AppendU8(encodingIndex);
			header.AppendU8(encodingIndex ^ 0xFF);
			
			_nodeBuffer = new ByteBuffer();
			_dataBuffer = new ByteBuffer();

			WriteNode(document.Root);
			
			_nodeBuffer.AppendU8((byte)KBinReader.Control.SectionEnd | 64);
			_nodeBuffer.RealignWrite();
			header.AppendU32((uint) _nodeBuffer.Length);
			_nodeBuffer.AppendU32((uint) _dataBuffer.Length);
			Document = header.Data.Concat(_nodeBuffer.Data.Concat(_dataBuffer.Data)).ToArray();
		}

		private int _byteOffset = 0;
		private int _wordOffset = 0;
		
		private void WriteDataAligned(byte[] data, int size, int count) {
			var totalSize = size * count;
			if (totalSize == 1) {
				if (_byteOffset % 4 == 0) {
					_byteOffset = _dataBuffer.Offset;
					_dataBuffer.AppendU32(0);
				}

				_dataBuffer.Set(data, _byteOffset);
				_byteOffset++;
			} else if(totalSize == 2) {
				if (_wordOffset % 4 == 0) {
					_wordOffset = _dataBuffer.Offset;
					_dataBuffer.AppendU32(0);
				}
				
				_dataBuffer.Set(data, _wordOffset);
				_wordOffset += 2;
			} else {
				_dataBuffer.AppendBytes(data);
				_dataBuffer.RealignWrite();
			}
		}

		private void WriteDataAuto(byte[] data) {
			_dataBuffer.AppendS32(data.Length);
			_dataBuffer.AppendBytes(data);
			_dataBuffer.RealignWrite();
		}
		
		private void WriteString(string text) {
			var data = _encoding.GetBytes(text);
			Array.Resize(ref data, data.Length + 1);
			WriteDataAuto(data);
		}

		private void WriteNode(XElement element) {
			var nodeType = element.Attribute("__type")?.Value;
			if (nodeType == null) {
				if (element.GetValue().Length > 0) {
					nodeType = "str";
				} else {
					nodeType = "void";
				}
			}

			var (nodeId, format) = Formats.First(x => x.Value.HasName(nodeType));
			var isArray = 0;
			var countValue = element.Attribute("__count")?.Value;
			if (countValue != null) {
				var count = int.Parse(countValue);
				isArray = 0b01000000;
			}

			_nodeBuffer.AppendU8(nodeId | isArray);

			var name = element.Name.LocalName;
			WriteNodeName(name);

			if (nodeType != "void") {
				var value = element.GetValue();
				byte[] data;

				if (format.Name == "bin") {
					data = new byte[value.Length / 2];
					for (var i = 0; i < data.Length; i++) {
						data[i] = Convert.ToByte(value[(i * 2)..(i * 2 + 2)], 16);
					}
				} else if (format.Name == "str") {
					data = _encoding.GetBytes(value);
					Array.Resize(ref data, data.Length + 1);
				} else {
					data = value.Split(" ").Aggregate(new byte[0], (b, s) => b.Concat(format.FormatFromString(s)).ToArray());
				}

				if (isArray > 0 || format.Count == -1) {
					_dataBuffer.AppendU32((uint) (data.Length * format.Size));
					_dataBuffer.AppendBytes(data);
					_dataBuffer.RealignWrite();
				} else {
					WriteDataAligned(data, format.Size, format.Count);
				}
			}

			foreach (var a in element.Attributes().OrderBy(x => x.Name.LocalName)) {
				if (!a.Name.LocalName.StartsWith("__")) {
					WriteString(a.Value);
					_nodeBuffer.AppendU8((byte) KBinReader.Control.Attribute);
					WriteNodeName(a.Name.LocalName);
				}
			}

			foreach (var c in element.Elements()) {
				WriteNode(c);
			}

			_nodeBuffer.AppendU8((byte) KBinReader.Control.NodeEnd | 64);
		}

		private void WriteNodeName(string name) {
			if (_compressed) {
				_nodeBuffer.AppendBytes(Sixbit.Encode(name));
			} else {
				var encoded = _encoding.GetBytes(name);
				_nodeBuffer.AppendU8((encoded.Length - 1) | 64);
				_nodeBuffer.AppendBytes(encoded);
			}
		}
	}

}