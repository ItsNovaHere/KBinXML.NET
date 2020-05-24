using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

				switch (nodeType) {
					case (byte) Control.Attribute: {
						var readDataAuto = ReadDataAuto(dataBuffer);
						Array.Reverse(readDataAuto);
						
						var value = encoding.GetString(readDataAuto).Trim('\0');
						node?.SetAttributeValue(name, value);
						
						break;
					}
					case (byte) Control.NodeEnd: {
						if (node?.Parent != null) {
							node = node.Parent;
						}

						break;
					}
					case (byte) Control.SectionEnd:
						nodesLeft = false;
						break;
					default:
						skip = false;
						break;
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

				string text;
				if (format.Equals(Format.Binary)) {
					node.SetAttributeValue("__size", totalCount);
					
					Array.Reverse(nodeData);
					text = "";
					for (var i = 0; i < nodeData.Length; i++) {
						text += nodeData[i].ToString("x2");
					}
				} else if (format.Equals(Format.String)) {
					Array.Reverse(nodeData);
					text = encoding.GetString(nodeData);
				} else if (isArray) {
					text = "";
					for (var i = 0; i < arrayCount; i++) {
						var dataChunk = nodeData[(i * count)..((i + 1) * count)];
						text = format.FormatToString(dataChunk) + " " + text;
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

		private static Dictionary<int, Format> Formats => new Dictionary<int, Format> {
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
			{44, (Format.Float * 4).Rename("4f")},
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

		private enum Control : byte {
			NodeStart = 1,
			Attribute = 46,
			NodeEnd = 190,
			SectionEnd = 191
		}
		
		private static void Assert(byte actual, byte expected) {
			if (actual != expected) throw new Exception();
		}

		private class Format {
			public static readonly Format S8 = new Format("s8", 1, Converters.S8ToString, Converters.S8FromString);
			public static readonly Format U8 = new Format("u8", 1, Converters.U8ToString, Converters.U8FromString);
			public static readonly Format S16 = new Format("s16", 2, Converters.S16ToString, Converters.S16FromString);
			public static readonly Format U16 = new Format("u16", 2, Converters.U16ToString, Converters.U16FromString);
			public static readonly Format S32 = new Format("s32", 4, Converters.S32ToString, Converters.S32FromString);
			public static readonly Format U32 = new Format("u32", 4, Converters.U32ToString, Converters.U32FromString);
			public static readonly Format S64 = new Format("s64", 8, Converters.S64ToString, Converters.S64FromString);
			public static readonly Format U64 = new Format("u64", 8, Converters.U64ToString, Converters.U64FromString);
			public static readonly Format Float = new Format(new[] {"float", "f"}, 4, Converters.SingleToString, Converters.SingleFromString);
			public static readonly Format Double = new Format(new[] {"double", "d"}, 8, Converters.DoubleToString, Converters.DoubleFromString);
			public static readonly Format Time = new Format("time", 4, Converters.U32ToString, Converters.U32FromString);
			public static readonly Format IP4 = new Format("ip4", 1, Converters.IP4ToString, Converters.IP4FromString, 4);
			public static readonly Format String = new Format( new []{"str", "string"}, 0, null!, null!, -1); // Theoretically these should never be called. Key word: Theoretically.
			public static readonly Format Binary = new Format(new[]{"bin", "binary"}, 0, null!, null!, -1); // See above.
			public static readonly Format Bool = new Format(new[] {"bool", "b"}, 1, Converters.BoolToString, Converters.BoolFromString);
			
			
			internal delegate byte[] FromString(string data);

			internal new delegate string ToString(byte[] data);

			private readonly int _count;
			private readonly FromString _fromString;

			private readonly List<string> _names;
			private readonly int _size;
			private readonly ToString _toString;

			private Format(IEnumerable<string> names, int size, ToString toString, FromString fromString, int count = 1) {
				_names = new List<string>(names);
				_size = size;
				_toString = toString;
				_fromString = x => {
					var ret = fromString(x);
					if(BitConverter.IsLittleEndian) Array.Reverse(ret);
					return ret;
				};
				_count = count;
			}

			private Format(string name, int size, ToString toString, FromString fromString, int count = 1) : this(new[] {name}, size, toString, fromString, count) { }
			public string Name => _names[0];
			public int Count => _count;
			public int Size => _size;
			public ToString FormatToString => _toString;
			public FromString FormatFromString => _fromString;
			
			public Format WithAlias(string alias) {
				_names.Add(alias);
				return this;
			}

			public Format Rename(string name) {
				_names.Clear();
				_names.Add(name);
				return this;
			}

			public static Format operator *(Format a, int b) {
				var names = a._names.Select(x => $"{b}{x}");
				var size = a._size;

				var toString = new ToString(data => {
					var ret = "";
					foreach (var dataChunk in data.Chunked(a._size)) {
						ret = a._toString(dataChunk) + " " + ret;
					}

					return ret.Trim();
				});
				var fromString = new FromString(data => {
					var returnArray = new List<byte>();
					foreach (var dataPart in data.Split(" ")) returnArray.AddRange(a._fromString(dataPart));

					return returnArray.ToArray();
				});

				return new Format(names, size, toString, fromString, b * a._count);
			}

			//Comparisons are based off naming.
			public override bool Equals(object obj) {
				if (!(obj is Format format)) return false;

				return format._names == _names;
			}

			public override int GetHashCode() {
				return Name.GetHashCode();
			}
		}

		public static class Converters {
			public static string IP4ToString(byte[] data) {
				Array.Reverse(data);
				var ret = "";
				for (var i = 0; i < data.Length; i++) {
					ret += data[i] + ".";
				}

				ret = ret.Substring(0, ret.Length - 1);
				return ret;
			}

			public static byte[] IP4FromString(string data) {
				var ret = new byte[4];
				var input = data.Split(".");
				for (var i = 0; i < ret.Length; i++) {
					ret[i] = byte.Parse(input[i]);
				}

				return ret;
			}

			public static string SingleToString(byte[] data) {
				return MathF.Round(BitConverter.ToSingle(data), 6).ToString(CultureInfo.InvariantCulture);
			}

			public static byte[] SingleFromString(string data) {
				return BitConverter.GetBytes(float.Parse(data));
			}

			public static string DoubleToString(byte[] data) {
				return Math.Round(BitConverter.ToDouble(data), 6).ToString(CultureInfo.InvariantCulture);
			}
			
			public static byte[] DoubleFromString(string data) {
				return BitConverter.GetBytes(double.Parse(data));
			}

			public static string BoolToString(byte[] data) {
				return data[0] == 0 ? "0" : "1";
			}

			public static byte[] BoolFromString(string data) {
				return new[] {(byte) (data[0] == '0' ? 0 : 1)};
			}
			
			public static string U8ToString(byte[] data) {
				return data[0].ToString();
			}

			public static string S8ToString(byte[] data) {
				return ((sbyte) data[0]).ToString();
			}

			public static string U16ToString(byte[] data) {
				return BitConverter.ToUInt16(data).ToString();
			}

			public static string S16ToString(byte[] data) {
				return BitConverter.ToInt16(data).ToString();
			}

			public static string U32ToString(byte[] data) {
				return BitConverter.ToUInt32(data).ToString();
			}

			public static string S32ToString(byte[] data) {
				return BitConverter.ToInt32(data).ToString();
			}

			public static string U64ToString(byte[] data) {
				return BitConverter.ToUInt64(data).ToString();
			}

			public static string S64ToString(byte[] data) {
				return BitConverter.ToInt64(data).ToString();
			}

			public static byte[] U8FromString(string data) {
				return new[] {byte.Parse(data)};
			}

			public static byte[] S8FromString(string data) {
				return new[] {(byte) sbyte.Parse(data)};
			}

			public static byte[] S16FromString(string data) {
				return BitConverter.GetBytes(short.Parse(data));
			}

			public static byte[] U16FromString(string data) {
				return BitConverter.GetBytes(ushort.Parse(data));
			}

			public static byte[] S32FromString(string data) {
				return BitConverter.GetBytes(int.Parse(data));
			}

			public static byte[] U32FromString(string data) {
				return BitConverter.GetBytes(uint.Parse(data));
			}

			public static byte[] S64FromString(string data) {
				return BitConverter.GetBytes(long.Parse(data));
			}

			public static byte[] U64FromString(string data) {
				return BitConverter.GetBytes(ulong.Parse(data));
			}
		}
	}

}