using System;
using System.Globalization;
using System.Linq;

namespace KBinXML {

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
			
			Array.Reverse(ret);
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