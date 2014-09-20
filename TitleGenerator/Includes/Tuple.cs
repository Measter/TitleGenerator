using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Measter
{
	public struct Tuple<TFirst, TSecond> : IEquatable<Tuple<TFirst, TSecond>>
	{
		public TFirst First { get; private set; }
		public TSecond Second { get; private set; }

		public Tuple( TFirst first, TSecond second )
			: this()
		{
			First = first;
			Second = second;
		}

		#region Equality members

		public bool Equals( Tuple<TFirst, TSecond> other )
		{
			return EqualityComparer<TFirst>.Default.Equals( First, other.First ) && EqualityComparer<TSecond>.Default.Equals( Second, other.Second );
		}

		public override bool Equals( object obj )
		{
			if ( ReferenceEquals( null, obj ) ) return false;
			return obj is Tuple<TFirst, TSecond> && Equals( (Tuple<TFirst, TSecond>)obj );
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ( EqualityComparer<TFirst>.Default.GetHashCode( First )*397 ) ^ EqualityComparer<TSecond>.Default.GetHashCode( Second );
			}
		}

		public static bool operator ==( Tuple<TFirst, TSecond> left, Tuple<TFirst, TSecond> right )
		{
			return left.Equals( right );
		}

		public static bool operator !=( Tuple<TFirst, TSecond> left, Tuple<TFirst, TSecond> right )
		{
			return !left.Equals( right );
		}

		#endregion

		private sealed class FirstSecondEqualityComparer : IEqualityComparer<Tuple<TFirst, TSecond>>
		{
			public bool Equals( Tuple<TFirst, TSecond> x, Tuple<TFirst, TSecond> y )
			{
				return EqualityComparer<TFirst>.Default.Equals( x.First, y.First ) && EqualityComparer<TSecond>.Default.Equals( x.Second, y.Second );
			}

			public int GetHashCode( Tuple<TFirst, TSecond> obj )
			{
				unchecked
				{
					return ( EqualityComparer<TFirst>.Default.GetHashCode( obj.First )*397 ) ^ EqualityComparer<TSecond>.Default.GetHashCode( obj.Second );
				}
			}
		}

		private static readonly IEqualityComparer<Tuple<TFirst, TSecond>> FirstSecondComparerInstance = new FirstSecondEqualityComparer();

		public static IEqualityComparer<Tuple<TFirst, TSecond>> FirstSecondComparer
		{
			get { return FirstSecondComparerInstance; }
		}

		public override string ToString()
		{
			return string.Format( "First: {0}, Second: {1}", First, Second );
		}
	}
}
