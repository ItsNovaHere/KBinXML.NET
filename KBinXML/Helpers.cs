using System;
using System.Linq;
using System.Xml.Linq;

namespace KBinXML {

	public static class Helpers {
		public static T[][] Chunked<T>(this T[] data, int size) {
			var ret = new T[data.Length / size][];
			
			for (var i = 0; i < ret.Length; i++) {
				ret[i] = data[(i * size)..((i + 1) * size)];
			}

			return ret;
		}

		public static T[] Ensure<T>(this T[] data, int size, T value = default) {
			if (data.Length >= size) return data;
			Array.Resize(ref data, size);
				
			for (var i = data.Length; i < size; i++) {
				data[i] = value;
			}

			return data;
		}

		public static string GetValue(this XElement element) {
			return element.Nodes().OfType<XText>().Aggregate("", (current, text) => current + text.Value);
		}
	}

}