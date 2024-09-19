using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AssetStudio
{
	public static class IKA9ntUtils
	{
		//AF的
		private const string pwd = "y9JUY4yttVeCBvZVdXsRMDLuL8H7vNyh";

		public static void AliceFictionDecrypt(Span<byte> data, string filename)
		{
			byte[] iv = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			filename = Path.GetFileName(filename);

			var unity = Encoding.UTF8.GetBytes("Unity");
			

			byte[] sign = new byte[5];
			var salt = Encoding.UTF8.GetBytes(filename);

			data.Slice(0, 5).CopyTo(sign);
			if (Enumerable.SequenceEqual(sign, unity))
			{
				return;
			}

			try
			{
				RijndaelManaged aesManaged = new RijndaelManaged();
				aesManaged.IV = iv;
				aesManaged.Mode = CipherMode.ECB;
				aesManaged.Padding = PaddingMode.None;
				PasswordDeriveBytes pdb = new PasswordDeriveBytes(pwd, salt, "SHA1", 0x64);
				aesManaged.Key = pdb.GetBytes(16);
				var decryptor = aesManaged.CreateDecryptor();
				using (MemoryStream ms = new MemoryStream(data.ToArray()))
				{
					using (MemoryStream dataOutput = new MemoryStream())
					{
						using (SeekableAesStream cs = new SeekableAesStream(ms, pwd, salt))
						{
							cs.CopyTo(dataOutput);
						}

						dataOutput.Position = 0;
						dataOutput.Read(sign, 0, 5);
						if (!Enumerable.SequenceEqual(sign, unity))
						{
							Console.WriteLine($"Dec error：{filename}");
						}

						dataOutput.Position = 0;
						dataOutput.ToArray().CopyTo(data);
					}
				}
				Console.WriteLine($"OK：{filename}");

			}
			catch (Exception e)
			{
				Console.WriteLine($"Error：{filename}");
			}
			finally
			{

			}
		}


		public class SeekableAesStream : Stream
		{
			readonly Stream _baseStream;
			readonly RijndaelManaged _aes;
			readonly ICryptoTransform _encryptor;

			public bool AutoDisposeBaseStream { get; set; } = true;

			public SeekableAesStream(Stream baseStream, string password, byte[] salt, int keySize = 128)
			{
				PasswordDeriveBytes pdb = new PasswordDeriveBytes(password, salt, "SHA1", 0x64);
				_baseStream = baseStream;

				_aes = new RijndaelManaged
				{
					KeySize = keySize,
					Mode = CipherMode.ECB,
					Padding = PaddingMode.None
				};
				_aes.Key = pdb.GetBytes(_aes.KeySize / 8);

				_aes.IV = new byte[16];
				_encryptor = _aes.CreateEncryptor(_aes.Key, _aes.IV);

			}

			void Cipher(byte[] buffer, int offset, int count, long streamPos)
			{
				// find block number
				var blockSizeInByte = _aes.BlockSize / 8;
				var blockNumber = (streamPos / blockSizeInByte) + 1;
				var keyPos = streamPos % blockSizeInByte;

				// buffer
				var outBuffer = new byte[blockSizeInByte];
				var nonce = new byte[blockSizeInByte];
				var init = false;

				for (var i = offset; i < count; i++)
				{
					// encrypt the nonce to form next xro buffer(unique key)
					if (!init || (keyPos % blockSizeInByte) == 0)
					{
						BitConverter.GetBytes(blockNumber).CopyTo(nonce, 0);
						_encryptor.TransformBlock(nonce, 0, nonce.Length, outBuffer, 0);
						if (init) keyPos = 0;
						init = true;
						blockNumber++;
					}
					buffer[i] ^= outBuffer[keyPos];
					keyPos++;
				}
			}

			public override bool CanRead => _baseStream.CanRead;
			public override bool CanSeek => _baseStream.CanSeek;
			public override bool CanWrite => _baseStream.CanWrite;
			public override long Length => _baseStream.Length;
			public override long Position
			{
				get => _baseStream.Position;
				set => _baseStream.Position = value;
			}
			public override void Flush() => _baseStream.Flush();
			public override void SetLength(long value) => _baseStream.SetLength(value);
			public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);

			public override int Read(byte[] buffer, int offset, int count)
			{
				var streamPos = Position;
				var ret = _baseStream.Read(buffer, offset, count);
				Cipher(buffer, offset, count, streamPos);
				return ret;
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				Cipher(buffer, offset, count, Position);
				_baseStream.Write(buffer, offset, count);
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					_encryptor?.Dispose();
					_aes?.Dispose();
					if (AutoDisposeBaseStream)
					{
						_baseStream?.Dispose();
					}
				}
				base.Dispose(disposing);
			}
		}

	}
}
