using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KBinXML {

	public class Sixbit {
		private const string CharacterMap = "0123456789:ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
		private static Dictionary<char, byte> StringMap { get; }

		static Sixbit() {
			StringMap = new Dictionary<char, byte>();
			for (var index = 0; index < CharacterMap.Length; index++) {
				StringMap.Add(CharacterMap[index], (byte) index);
			}
		}
		
		public static string Decode(ByteBuffer data) {
			var length = data.GetU8();

			var bits = data.GetBytes((length * 6 + 7) / 8, false);
			var returnBytes = new byte[(length * 8 + 7) / 6];

			for (var i = 0; i < bits.Length; i++) {
				switch (i % 3) {
					case 0:
						returnBytes[i + i / 3] += (byte) ((bits[i] & 0b11111100) >> 2);
						returnBytes[i + 1 + i / 3] += (byte) ((bits[i] & 0b00000011) << 4);
						break;
					case 1:
						returnBytes[i + i / 3] += (byte) ((bits[i] & 0b11110000) >> 4);
						returnBytes[i + 1 + i / 3] += (byte) ((bits[i] & 0b00001111) << 2);
						break;
					case 2:
						returnBytes[i + i / 3] += (byte) ((bits[i] & 0b11000000) >> 6);
						returnBytes[i + 1 + i / 3] += (byte) (bits[i] & 0b00111111);
						break;
				}
			}

			var decodedChars = returnBytes.Select(x => CharacterMap[x]).ToArray();
			return new string(decodedChars).Substring(0, length).Trim('\0');
		}

		public static byte[] Encode(string data) {
			var chars = new byte[data.Length];
			for (var i = 0; i < data.Length; i++) {
				chars[i] = StringMap[data[i]];
			}

			var returnBytes = new byte[(data.Length * 6 + 5) / 8 + 2];
			returnBytes[0] = (byte) data.Length;
			for (var i = 1; i < returnBytes.Length; i++) {
				try {
					switch ((i - 1) % 3) {
						case 0:
							returnBytes[i] += (byte) ((chars[i - 1 + (i - 1) / 3] & 0b00111111) << 2);
							returnBytes[i] += (byte) ((chars[i + (i - 1) / 3] & 0b00110000) >> 4);
							break;
						case 1:
							returnBytes[i] += (byte) ((chars[i - 1 + (i - 1) / 3] & 0b00001111) << 4);
							returnBytes[i] += (byte) ((chars[i + (i - 1) / 3] & 0b00111100) >> 2);
							break;
						case 2:
							returnBytes[i] += (byte) ((chars[i - 1 + (i - 1) / 3] & 0b00000011) << 6);
							returnBytes[i] += (byte) (chars[i + (i - 1) / 3] & 0b00111111);
							break;
					}
				} catch (IndexOutOfRangeException) {
					// ignore
				}
			}
			
			// There has to be a better way of trimming null bytes, but alas I am a shit C# dev.
			while(returnBytes[^1] == 0x00) {
				Array.Resize(ref returnBytes, returnBytes.Length - 1);
			}
			
			return returnBytes;
		}
	}

}