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
			
			var lengthBits = length * 6;
			var lengthBytes = (lengthBits + 7) / 8;

			
			var bits = data.GetBytes(lengthBytes, false);
			var returnBytes = new byte[length * 8 / 6];
			
			for (var i = 0; i < bits.Length * 8; i++) {
				var bit = bits[i / 8];
				var bitShift = i % 8;
				var value = (byte) (((bit << bitShift) & 0b10000000) >> 7);
				returnBytes[i / 6] += value;
				if(i % 6 != 5) returnBytes[i / 6] <<= 1;
			}

			var decodedChars = returnBytes.Select(x => CharacterMap[x]).ToArray();
			return new string(decodedChars).Substring(0, length);
		}
	}

}