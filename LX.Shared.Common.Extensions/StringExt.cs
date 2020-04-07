using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LX.Common.Extensions
{
	/// <summary>
	/// String extensionok
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	static class StringExt
	{
		/// <summary>
		/// 3.5-ös .Net-ből hiányzó fícsör pótlása
		/// </summary>
		/// <returns></returns>
#if NETFX_40
#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static bool IsNullOrWhitespace(this string self)
			=> string.IsNullOrWhiteSpace(self);
#else
		public static bool IsNullOrWhitespace(this string self)
			=> string.IsNullOrEmpty(self) || self.Trim().Length == 0;
#endif

		/// <summary>
		/// .Equals(Ordinal(IgnoreCase) + Trim)
		/// </summary>
		/// <param name="self">Ezt</param>
		/// <param name="other">Ehhez hasonlítja</param>
		/// <param name="ignoreCase">kis/NAGYbetű mindegy</param>
		/// <returns>Egyezik-e a két string</returns>
#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static bool NEquals(this string self, string other, bool ignoreCase = true)
			=> self.Trim().Equals(other.Trim(), ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

		/// <summary>
		/// Substring biztonságos verziója
		/// </summary>
		/// <param name="self"></param>
		/// <param name="startIndex"></param>
		/// <param name="length"></param>
		/// <returns></returns>
#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static string Copy(this string self, int startIndex, int length = int.MaxValue)
			=> startIndex < self?.Length ? self.Substring(startIndex, Math.Min(length, self.Length - startIndex)) : string.Empty;

#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static string RightStr(this string self, int length)
			=> self.Copy(self.Length - length, length);

#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static string LeftStr(this string self, int length)
			=> self.Copy(0, length);

		/// <summary>
		/// Stringből delphi kompatibilis byte tömböt gyárt, üres string esetén 1 db szóközzel helyettesíti azt
		/// </summary>
		/// <param name="self">Bemeneti string</param>
		/// <returns>byte tömb ansi kódolással</returns>
		public static byte[] Str2Bytes(this string self)
			=> Encoding.Default.GetBytes(string.IsNullOrEmpty(self) ? " " : self);

		/// <summary>
		/// A beadott stringet Enummá konvertálja
		/// </summary>
		/// <typeparam name="T">Enum típus</typeparam>
		/// <param name="self">Bemeneti string</param>
		/// <returns></returns>
		public static T ToEnum<T>(this string self)
			=> (T)Enum.Parse(typeof(T), self, true);

#if NETFX_40
		/// <summary>
		/// A beadott stringet Enummá konvertálja
		/// </summary>
		/// <typeparam name="T">Enum típus</typeparam>
		/// <param name="self">Bemeneti string</param>
		/// <param name="def">Alapértelmetett érték, ha nem lehet konvertálni</param>
		/// <returns></returns>
		public static T ToEnum<T>(this string self, T def) where T : struct, Enum
			=> Enum.TryParse<T>(self, out var res) ? res : def;
#endif

		/// <summary>
		/// Rövidítés a ToTitleCase eljáráshoz
		/// </summary>
		/// <param name="self">Bemeneti szöveg</param>
		/// <param name="lowerFirst">Kisbetűsítés elsőként</param>
		/// <returns>Kimeneti szöveg</returns>
#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static string ToTitleCase(this string self, bool lowerFirst = false)
			=> CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
				lowerFirst
					? self.ToLowerInvariant()
					: self);

		/// <summary>
		/// Ékezetek eltávolítása szövegből
		/// </summary>
		/// <param name="self">Szöveg</param>
		/// <returns>Ékezetmentes szöveg</returns>
		public static string RemoveDiacritics(this string self)
		{
			if (self.IsNullOrWhitespace())
			{
				return self;
			}

			self = self.Normalize(NormalizationForm.FormD);
			char[] chars = self.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
			return new string(chars).Normalize(NormalizationForm.FormC);
		}

		public static string IncludeTrailingPathDelimiter(this string self, char? delimiter = null)
			=> self.EndsWith((delimiter ?? Path.DirectorySeparatorChar).ToString())
				? self
				: string.Concat(self, delimiter ?? Path.DirectorySeparatorChar);

		public static string UnixCombine(this string self, params string[] paths)
		{
			static string IncludeTrailingSlash(string path) => path.IncludeTrailingPathDelimiter('/');
			var masterBuilder = new StringBuilder(IncludeTrailingSlash(self));

			foreach (string path in paths)
			{
				masterBuilder.Append(IncludeTrailingSlash(path));
			}

			return masterBuilder.ToString();
		}

		public static bool IsMatches(this string self, string pattern)
			=> Regex.IsMatch(self, pattern, RegexOptions.IgnoreCase);
	}
}
