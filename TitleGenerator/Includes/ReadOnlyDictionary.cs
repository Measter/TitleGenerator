using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Measter
{
	public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
	{
		private readonly IDictionary<TKey, TValue> m_dictionary;

		public ReadOnlyDictionary()
		{
			m_dictionary = new Dictionary<TKey, TValue>();
		}

		public ReadOnlyDictionary( IDictionary<TKey, TValue> dict )
		{
			m_dictionary = new Dictionary<TKey, TValue>( dict );
		}


		#region Implementation of IDictionary<Tkey,TValue>

		public bool ContainsKey( TKey key )
		{
			return m_dictionary.ContainsKey( key );
		}

		public void Add( TKey key, TValue value )
		{
			throw ReadOnlyException();
		}
		public bool Remove( TKey key )
		{
			throw ReadOnlyException();
		}

		public bool TryGetValue( TKey key, out TValue value )
		{
			return m_dictionary.TryGetValue( key, out value );
		}
	    
		public TValue this[TKey key]
		{
			get
			{
				return m_dictionary[key];
			}
			set
			{
				throw ReadOnlyException();
			}
		}

		public ICollection<TKey> Keys
		{
			get
			{
				return m_dictionary.Keys;
			}
		}
		public ICollection<TValue> Values
		{
			get
			{
				return m_dictionary.Values;
			}
		}

		#endregion

		#region Implementation of ICollection<KeyValuePair<Tkey,TValue>>

		public void Add( KeyValuePair<TKey, TValue> item )
		{
			throw ReadOnlyException();
		}
		public void Clear()
		{
			throw ReadOnlyException();
		}
		public bool Contains( KeyValuePair<TKey, TValue> item )
		{
			return m_dictionary.Contains( item );
		}

		public void CopyTo( KeyValuePair<TKey, TValue>[] array, int arrayIndex )
		{
			m_dictionary.CopyTo( array, arrayIndex );
		}
		public bool Remove( KeyValuePair<TKey, TValue> item )
		{
			throw ReadOnlyException();
		}

		public int Count
		{
			get
			{
				return m_dictionary.Count;
			}
		}
		public bool IsReadOnly
		{
			get
			{
				return true;
			}
		}

		#endregion

		#region Implementation of IEnumerable

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return m_dictionary.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		private static Exception ReadOnlyException()
		{
			return new NotSupportedException( "This Dictionary is read-only" );
		}
	}
}
