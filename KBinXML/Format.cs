using System;
using System.Collections.Generic;
using System.Linq;

namespace KBinXML {

	internal class Format {
		public static readonly Format Void = new Format("void", default, default, default);
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

		public bool HasName(string name) {
			return _names.Contains(name);
		}
		
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
				var bytes = data.Chunked(a._size);
				for (var i = 0; i < bytes.Length; i++) {
					var dataChunk = bytes[i];
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

}