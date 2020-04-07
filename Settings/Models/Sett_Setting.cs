using System;
using System.Collections.Generic;
using System.Text;

namespace Settings
{
	public class Sett_Setting : ISetting
	{
		public long Id { get; set; }
		public string? Name { get; set; }
		public long ReferenceId { get; set; }
		public string? Value { get; set; }
	}
}
