using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using LX.Common.Extensions;

namespace LX.Common
{
#if EXTERN
	public
#else
	internal
#endif
	static class AesCrypto
	{
		private enum TransformerType { Encript, Decript, }

		private static byte[] PerformCryptography(ICryptoTransform cryptoTransform, byte[] data)
		{
			byte[] res;
			var ms = new MemoryStream();
			using (var cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write))
			{
				cryptoStream.Write(data, 0, data.Length);
				cryptoStream.FlushFinalBlock();
				res = ms.ToArray();
			}

			return res;
		}

		private static ICryptoTransform GetCryptoTransformer(TransformerType transformerType, byte[] encryptionKey)
		{
			using (var aes = Aes.Create())
			{
				aes.BlockSize = 128;
				aes.KeySize = 256;
				aes.IV = new byte[16];
				aes.Key = encryptionKey.GetHash<SHA256Managed>();

				switch (transformerType)
				{
					case TransformerType.Encript:
						return aes.CreateEncryptor();
					case TransformerType.Decript:
						return aes.CreateDecryptor();
					default:
						throw new ArgumentOutOfRangeException(nameof(transformerType), transformerType, null);
				}
			}
		}

		public static byte[] AesDecrypt(byte[] encryptedBytes, byte[] encryptionKey)
		{
			using (var decryptor = GetCryptoTransformer(TransformerType.Decript, encryptionKey))
			{
				return PerformCryptography(decryptor, encryptedBytes);
			}
		}

		/// <summary>
		/// BASE64 byte tömb Aes dekódolása
		/// </summary>
		/// <param name="encryptedText">BASE64 kódolt, titkosított byte tömb</param>
		/// <param name="encryptionKey">Titkosítókulcs</param>
		/// <param name="encoding">Karakterkódolás</param>
		/// <returns>Titkosítatlan szöveg</returns>
		public static string AesDecryptString(string encryptedText, string encryptionKey, Encoding encoding = null)
		{
			byte[] decrypted = AesDecrypt(Convert.FromBase64String(encryptedText), (encoding ?? Encoding.UTF8).GetBytes(encryptionKey));
			return (encoding ?? Encoding.UTF8).GetString(decrypted);
		}

		public static byte[] AesEncrypt(byte[] data, byte[] encryptionKey)
		{
			using (var encryptor = GetCryptoTransformer(TransformerType.Encript, encryptionKey))
			{
				return PerformCryptography(encryptor, data);
			}
		}

		/// <summary>
		/// Szöveg Aes titkosítása BASE64 byte tömbbe
		/// </summary>
		/// <param name="text">Titkosítandó szöveg</param>
		/// <param name="encryptionKey">Titkosítókulcs</param>
		/// <param name="encoding">Karakterkódolás</param>
		/// <returns>BASE64 kódolt, titkosított byte tömb</returns>
		public static string AesEncryptString(string text, string encryptionKey, Encoding encoding = null)
		{
			byte[] encryptedData = AesEncrypt((encoding ?? Encoding.UTF8).GetBytes(text), (encoding ?? Encoding.UTF8).GetBytes(encryptionKey));
			return Convert.ToBase64String(encryptedData);
		}
	}
}
