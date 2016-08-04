using System.Runtime.InteropServices;

namespace Yeppp
{

	/// <summary>Vector mathematical functions</summary>
	public partial class Math
	{


		/// <summary>Computes sine on double precision (64-bit) floating-point elements.</summary>
		/// <param name="xArray">Input array.</param>
		/// <param name="xOffset">Offset of the first element in xArray.</param>
		/// <param name="yArray">Output array.</param>
		/// <param name="yOffset">Offset of the first element in yArray.</param>
		/// <param name="length">The length of the subarrays to be used in computation.</param>
		/// <exception cref="System.NullReferenceException">If xArray or yArray is null.</exception>
		/// <exception cref="System.DataMisalignedException">If xArray or yArray is not naturally aligned.</exception>
		/// <exception cref="System.ArgumentException">If length is negative.</exception>
		/// <exception cref="System.IndexOutOfRangeException">If xOffset is negative, xOffset + length exceeds the length of xArray, yOffset is negative, yOffset + length exceeds the length of yArray, or length is negative.</exception>
		public static unsafe void Sin_V64f_V64f(double[] xArray, int xOffset, double[] yArray, int yOffset, int length)
		{
			if (xOffset < 0)
				throw new System.IndexOutOfRangeException();

			if (xOffset + length > xArray.Length)
				throw new System.IndexOutOfRangeException();

			if (yOffset < 0)
				throw new System.IndexOutOfRangeException();

			if (yOffset + length > yArray.Length)
				throw new System.IndexOutOfRangeException();

			if (length < 0)
				throw new System.ArgumentException();

			fixed (double* x = &xArray[xOffset])
			{
				fixed (double* y = &yArray[yOffset])
				{
					Sin_V64f_V64f(x, y, length);
				}
			}
		}


		/// <summary>Computes sine on an array of double precision (64-bit) floating-point elements.</summary>
		/// <param name="x">Pointer to the array of elements on which sine will be computed.</param>
		/// <param name="y">Pointer the array where the computed sines will be stored.</param>
		/// <param name="length">Length of the arrays specified by x and y.</param>
		/// <exception cref="System.NullReferenceException">If x or y is null.</exception>
		/// <exception cref="System.DataMisalignedException">If x or y is not naturally aligned.</exception>
		/// <exception cref="System.ArgumentException">If length is negative.</exception>
		public static unsafe void Sin_V64f_V64f(double* x, double* y, int length)
		{
			if (length < 0)
				throw new System.ArgumentException();

			Status status = yepMath_Sin_V64f_V64f(x, y, new System.UIntPtr(unchecked((uint) length)));
			if (status != Status.Ok)
				throw Library.GetException(status);
		}


		[DllImport("yeppp", ExactSpelling=true, CallingConvention=CallingConvention.Cdecl, EntryPoint="yepMath_Sin_V64f_V64f")]
		internal static unsafe extern Status yepMath_Sin_V64f_V64f(double* x, double* y, System.UIntPtr length);

	}

}