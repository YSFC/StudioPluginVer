using System;
using System.Text;
using System.Security.Cryptography;

namespace AssetStudio
{
    /// <summary>
    /// 归龙潮(Gui Long Chao)专用
    /// </summary>
    public class UnityCN_GLC
    {
        private const string Signature = "#$unity3dchina!@";

        private static ICryptoTransform Encryptor;

        public byte[] Index = new byte[0x10];
        public byte[] Sub = new byte[0x10];

        public UnityCN_GLC(EndianBinaryReader reader)
        {
            reader.ReadUInt32();

			var infoBytes = reader.ReadBytes(0x8);
			reader.AlignStream();

			infoBytes = infoBytes.ToUInt4Array();
			Index = Array.Empty<byte>();
			var subBytes = infoBytes.AsSpan(0, 0x10);
			for (var i = 0; i < subBytes.Length; i++)
            {
                var idx = (i % 4 * 4) + (i / 4);
                Sub[idx] = subBytes[i];
            }
        }

        public void DecryptBlock(Span<byte> bytes, int size, int index)
		{
			int count = 0;
			var offset = 0;
            while (offset < size)
			{
				if (count++ >= 0x14) break;
				offset += Decrypt(bytes.Slice(offset), index++, size - offset);
            }
        }

        private int DecryptByte(Span<byte> bytes, ref int offset, ref int index)
        {
            var b = Sub[((index >> 2) & 3) + 4] + Sub[index & 3] + Sub[((index >> 4) & 3) + 8] + Sub[((byte)index >> 6) + 12];
			bytes[offset] = byte.RotateLeft(bytes[offset], b & 7);
			b = bytes[offset];
            offset++;
            index++;
            return b;
        }

        private int Decrypt(Span<byte> bytes, int index, int remaining)
        {
            var offset = 0;

            var curByte = DecryptByte(bytes, ref offset, ref index);
            var byteHigh = curByte >> 4;
            var byteLow = curByte & 0xF;

            if (byteHigh == 0xF)
            {
                int b;
                do
                {
                    b = DecryptByte(bytes, ref offset, ref index);
                    byteHigh += b;
                } while (b == 0xFF);
            }

            offset += byteHigh;

            if (offset < remaining)
            {
                DecryptByte(bytes, ref offset, ref index);
                DecryptByte(bytes, ref offset, ref index);
                if (byteLow == 0xF)
                {
                    int b;
                    do
                    {
                        b = DecryptByte(bytes, ref offset, ref index);
                    } while (b == 0xFF);
                }
            }

            return offset;
        }

        public class Entry
        {
            public string Name { get; private set; }
            public string Key { get; private set; }

            public Entry(string name, string key)
            {
                Name = name;
                Key = key;
            }

            public bool Validate()
            {
                var bytes = Convert.FromHexString(Key);
                if (bytes.Length != 0x10)
                {
                    Logger.Warning($"[UnityCN] {this} has invalid key, size should be 16 bytes, skipping...");
                    return false;
                }

                return true;
            }

            public override string ToString() => $"{Name} ({Key})";
        }
    }
}