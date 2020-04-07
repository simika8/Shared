using System;
using System.Globalization;

namespace LX.Common.Extensions
{
	/// <summary>
	/// DateTime extensionok
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	static class DateTimeExt
	{
		/// <summary>
		/// Unix timestamp is seconds past epoch
		/// </summary>
		/// <param name="self"></param>
		/// <param name="kind"></param>
		/// <returns></returns>
#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static DateTime FromUnixDateTime(this long self, DateTimeKind kind = DateTimeKind.Unspecified)
			=> new DateTime(1970, 1, 1, 0, 0, 0, 0, kind).AddSeconds(self);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="self"></param>
		/// <param name="kind"></param>
		/// <returns></returns>
#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static long ToUnixDateTime(this DateTime self, DateTimeKind kind = DateTimeKind.Unspecified)
			=> (long)self.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, kind)).TotalSeconds;

#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static int MonthsBetween(this DateTime self, DateTime other)
			=> self.Month - other.Month + 12 * (self.Year - other.Year);

		/// <summary>
		/// Munkanapok hozzáadása dátumhoz
		/// </summary>
		/// <param name="current">Aktuális dátum</param>
		/// <param name="days">Napok száma</param>
		/// <returns></returns>
		public static DateTime AddBusinessDays(this DateTime current, int days)
		{
			int sign = Math.Sign(days);
			int unsignedDays = Math.Abs(days);

			for (int i = 0; i < unsignedDays; i++)
			{
				do
				{
					current = current.AddDays(sign);
				} while (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday);
			}

			return current;
		}

#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static DateTime? NAddSeconds(this DateTime? self, double? value) 
			=> value.HasValue ? self?.AddSeconds(value.Value) : null;

#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static DateTime? NAddMinutes(this DateTime? self, double? value) 
			=> value.HasValue ? self?.AddMinutes(value.Value) : null;

#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static DateTime BeginingOfMonth(this DateTime self)
			=> new DateTime(self.Year, self.Month, 1);

#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static DateTime EndOfMonth(this DateTime self)
			=> new DateTime(self.Year, self.Month, 1).AddMonths(1).AddMilliseconds(-1);

#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static string ToStringInvariant(this DateTime self, string format)
			=> self.ToString(format, CultureInfo.InvariantCulture);
	}
}
