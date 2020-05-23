using System;
using System.Linq;

namespace KBinXML {

	public class ByteBuffer {
		private byte[] _data;
		private int _offset;
		
		public ByteBuffer(byte[] data, int offset = 0) {
			_data = data;
			_offset = offset;
		}

		public int Offset {
			get => _offset;
			set => _offset = value;
		}

		public byte[] GetBytes(int count) {
			var data = _data[_offset..(_offset += count)];
			
			if (BitConverter.IsLittleEndian) {
				Array.Reverse(data);
			}

			return data;
		}

		public byte[] PeekBytes(int count) {
			var data = _data[_offset..(_offset + count)];

			if (BitConverter.IsLittleEndian) {
				Array.Reverse(data);
			}

			return data;
		}

		public void RealignRead(int size = 4) {
			while (_offset % size != 0) {
				_offset++;
			}
		}

		public void RealignWrite(int size = 4) {
			while (_offset % size != 0) {
				AppendBytes(new byte[]{0});
			}
		}

		private byte GetByte() {
			return GetBytes(1)[0];
		}

		private byte PeekByte() {
			return PeekBytes(1)[0];
		}

		public void AppendBytes(byte[] data) {
			Array.Resize(ref _data, _data.Length + data.Length);
			data.CopyTo(_data, _offset);
			_offset += data.Length;
		}

		public byte PeekU8() => PeekByte();
		
		public byte GetU8() => GetByte();
		public sbyte GetS8() => (sbyte) GetByte();
		public ushort GetU16() => BitConverter.ToUInt16(GetBytes(sizeof(ushort)));
		public short GetS16() => BitConverter.ToInt16(GetBytes(sizeof(short)));
		public uint GetU32() => BitConverter.ToUInt32(GetBytes(sizeof(uint)));
		public int GetS32() => BitConverter.ToInt32(GetBytes(sizeof(int)));
		public ulong GetU64() => BitConverter.ToUInt64(GetBytes(sizeof(ulong)));
		public long GetS64() => BitConverter.ToInt64(GetBytes(sizeof(long)));
	}

}