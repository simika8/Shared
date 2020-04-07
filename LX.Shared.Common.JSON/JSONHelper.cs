using Newtonsoft.Json.Converters;

namespace LX.Common.JSON
{
#if EXTERN
	public
#else
	internal
#endif
	class JSONHelper
	{
		/// <summary>
		/// Egyedi dátumformázáshoz segédosztály
		/// </summary>
		public class DateFormatConverter : IsoDateTimeConverter
		{
			public DateFormatConverter(string format) => DateTimeFormat = format;
		}
	}
}

