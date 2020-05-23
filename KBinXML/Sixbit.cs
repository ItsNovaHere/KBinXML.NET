using System;
using System.Collections.Generic;
using System.Linq;

namespace KBinXML {

	public class Sixbit {
		private const string CharacterMap = "0123456789:ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
		private static readonly Dictionary<char, byte> StringMap;

		static Sixbit() {
			StringMap = new Dictionary<char, byte>();
			foreach (var c in CharacterMap) {
				StringMap.Add(c, (byte)c);
			}
		}

		
		public static string Decode(ByteBuffer data) {
			var length = data.GetU8();
			var returnBytes = new byte[length];
			var lengthBits = length * 6;
			var lengthBytes = (lengthBits + 7) / 8;
			var padding = 8 - lengthBits % 8;

			var bits = BitConverter.ToInt64(data.GetBytes(lengthBytes).Ensure(8));
			bits >>= padding != 8 ? padding : 0; 
			for (var i = 0; i < length; i++) {
				returnBytes[i] = (byte)(bits & 0b111111);
				bits >>= 6;
			}

			var decodedChars = returnBytes.Select(x => CharacterMap[x]).ToArray();
			Array.Reverse(decodedChars);
			return new string(decodedChars);
		}
	}

}