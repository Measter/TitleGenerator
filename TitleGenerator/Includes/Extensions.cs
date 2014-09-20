using System;
using System.Collections.Generic;
using System.Linq;

namespace TitleGenerator
{
	public static class Extensions
	{
		public static bool IsFlagSet<T>( this Enum value, Enum flag )
		{
			if( !typeof( T ).IsEnum )
				throw new ArgumentException();
			if( value.Equals( flag ) )
				return true;
			return ( (int)Enum.Parse( typeof( T ), value.ToString() ) & (int)Enum.Parse( typeof( T ), flag.ToString() ) ) != 0;
		}

		public static double GetStandardDeviation( this double[] l )
		{
			double sum = l.Aggregate<double, double>( 0, ( c, t ) => c + t );
			double mean = sum / l.Length;

			for( int i = 0; i < l.Length; i++ )
				l[i] = Math.Pow( l[i] - mean, 2 );

			sum = l.Aggregate<double, double>( 0, ( c, t ) => c + t );
			mean = sum / l.Length;

			return Math.Sqrt( mean );
		}

		public static int Clamp( this int n, int min, int max )
		{
			if( n > max )
			{
				return max;
			}
			if( n < min )
			{
				return min;
			}
			return n;
		}


		public static KeyValuePair<T1, T2> RandomItem<T1, T2>( this Dictionary<T1, T2> list, Random rand )
		{
			if( !list.Any() )
				return default( KeyValuePair<T1, T2> );

			return list.ElementAt( rand.Next( list.Count ) );
		}

		public static T RandomItem<T>( this List<T> list, Random rand )
		{
			if( !list.Any() )
				return default( T );

			return list[rand.Next( list.Count )];
		}

		public static T RandomItem<T>( this IEnumerable<T> list, Random rand )
		{
			if( !list.Any() )
				return default( T );

			var enumerable = list as List<T> ?? list.ToList();
			return enumerable.ToList()[rand.Next( enumerable.Count() )];
		}


		public static int Normal( this Random r, double average, double devation )
		{
			double u1 = r.NextDouble();
			double u2 = r.NextDouble();
			double normal = Math.Sqrt( -2.0 * Math.Log( u1 ) ) * Math.Sin( 2.0 * Math.PI * u2 );

			return (int)Math.Round( ( normal * devation ) + average );
		}
		public static int Normal( this Random r, int min, int max )
		{
			double mean = ( max - min ) / 2.0;
			double stdDev = mean / 3.0;

			int res = Normal( r, mean, stdDev ) + min;

			return res.Clamp( min, max );
		}
	}
}