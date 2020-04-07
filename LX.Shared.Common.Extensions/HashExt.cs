using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LX.Common.Extensions
{
	/// <summary>
	/// Hash extensionok
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	static class HashExt
	{
		/// <summary>
		/// Stringből MD5 hash számítása
		/// </summary>
		/// <param name="self">Bemeneti string</param>
		/// <returns>MD5 hash kisbetűs hex string formában</returns>
		public static string GetMd5Hash(this string self)
			=> self.GetHash<MD5CryptoServiceProvider>();

		/// <summary>
		/// Byte tömbből hash számítása
		/// </summary>
		/// <typeparam name="THash">Hash algoritmus</typeparam>
		/// <param name="self">Bemeneti byte tömb</param>
		/// <returns>Hash érték byte tömbben</returns>
		public static byte[] GetHash<THash>(this byte[] self) where THash : HashAlgorithm, new()
		{
			using var cryptoServiceProvider = new THash();
			return cryptoServiceProvider.ComputeHash(self);
		}

		/// <summary>
		/// Stringből hash számítása
		/// </summary>
		/// <typeparam name="THash">Hash algoritmus</typeparam>
		/// <param name="self">Bemeneti string</param>
		/// <param name="enc">Karakterkódolás (UTF-8 alapértelmezetten)</param>
		/// <returns>Hash kisbetűs hex string formában</returns>
		public static string GetHash<THash>(this string self, Encoding enc = null) where THash : HashAlgorithm, new()
		{
			byte[] data = GetHash<THash>((enc ?? Encoding.UTF8).GetBytes(self));

			return data.Aggregate(
				new StringBuilder(data.Length * 2),
				(sb, current) => sb.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", current)
			).ToString();
		}
	}
}
