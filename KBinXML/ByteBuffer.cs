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

		public ByteBuffer() : this(new byte[0]) {
			
		}

		public byte[] Data => _data;
		
		public int Offset {
			get => _offset;
			set => _offset = value;
		}

		public int Length => _data.Length;

		public byte[] GetBytes(int count, bool reverse = true) {
			var data = _data[_offset..(_offset += count)];
			
			if (BitConverter.IsLittleEndian && reverse) {
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
				AppendU8(0);
			}
		}

		public void Set(byte[] data, int offset) {
			var dataEnd = data.Length + offset;
			if (dataEnd > _data.Length) {
				Array.Resize(ref _data, dataEnd);
			}
			
			
		}

		private byte GetByte() {
			return GetBytes(1)[0];
		}

		private byte PeekByte() {
			return PeekBytes(1)[0];
		}

		public void AppendBytes(byte[] data) {
			Array.Resize(ref _data, _offset + data.Length);
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

		public void AppendU8(in byte data) => AppendBytes(new[] {data});
		// This allows for cleaner code when dealing with bit math.
		public void AppendU8(in int data) => AppendBytes(new[] {(byte) data}); 
		public void AppendS8(in sbyte data) => AppendBytes(new[] {(byte) data});
		public void AppendU16(in ushort data) => AppendBytes(BitConverter.GetBytes(data));
		public void AppendU32(in uint data) => AppendBytes(BitConverter.GetBytes(data));
		public void AppendS32(in int data) => AppendBytes(BitConverter.GetBytes(data));
	}

}