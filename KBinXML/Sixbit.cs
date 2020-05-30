using System;
using System.Collections.Generic;
using System.Linq;

namespace KBinXML {

	public class Sixbit {
		private const string CharacterMap = "0123456789:ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
		private static Dictionary<char, byte> StringMap { get; }

		static Sixbit() {
			StringMap = new Dictionary<char, byte>();
			foreach (var c in CharacterMap) {
				StringMap.Add(c, (byte)c);
			}
		}

		
		public static string Decode(ByteBuffer data) {
			var length = data.GetU8();

			var bits = data.GetBytes((length * 6 + 7) / 8, false);
			var returnBytes = new byte[(length * 8 / 6) + 1];
			
			for (var i = 0; i < bits.Length * 8; i++) {
				returnBytes[i / 6] += (byte) (((bits[i / 8] << i % 8) & 0b10000000) >> 7);
				if(i % 6 != 5) returnBytes[i / 6] <<= 1;
			}

			var decodedChars = returnBytes.Select(x => CharacterMap[x]).ToArray();
			return new string(decodedChars).Substring(0, length).Trim('\0');
		}

		public static byte[] Encode(string data) {
			var chars = new byte[data.Length];
			for (var i = 0; i < data.Length; i++) {
				chars[i] = StringMap[data[i]];
			}

			var returnBytes = new byte[chars.Length * 6 / 8 + 2];
			returnBytes[0] = (byte) (data.Length & 0xFF);
			
			for (var i = 0; i < chars.Length * 6; i++) {
				returnBytes[i / 8 + 1] += (byte) (((chars[i / 6] << i % 6) & 0b100000) >> 5);
				if (i % 8 != 7) returnBytes[i / 8 + 1] <<= 1;
			}

			return returnBytes;
		}
	}

}