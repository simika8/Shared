namespace LX.Common.Extensions
{
	/// <summary>
	/// Tömb extensionok
	/// </summary>
#if EXTERN
	public
#else
	internal
#endif
	static class ArrayExt
	{
#if NETFX_46
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static T[] Empty<T>() => System.Array.Empty<T>();
#else
		private static class EmptyArray<T>
		{
			public static readonly T[] Value = new T[0];
		}

#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static T[] Empty<T>() => EmptyArray<T>.Value;
#endif

#if NETFX_45
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		public static T[] AsArray<T>(T item) => new[] { item };
	}
}
