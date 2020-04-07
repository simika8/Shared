namespace System.ComponentModel.DataAnnotations
{
	public sealed class DecimalFractionDigitsAttribute : Attribute
	{
		public int Digits { get; }

		public DecimalFractionDigitsAttribute(int digits)
			=> Digits = digits;
	}
}
