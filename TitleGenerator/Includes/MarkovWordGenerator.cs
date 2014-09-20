using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Parsers;

namespace Measter
{
	public class MarkovWordGenerator
	{
		const int MAX_TRIES = 200;


		private Dictionary<string, Chain> m_chains;
		private Dictionary<int, int> m_sampleLengths;
		private int m_totalSampleSize;
		private List<string> m_used;
		private Random m_rand;
		private int m_order;
		private char m_nullChar;
		private readonly Dictionary<Tuple<int, int>, bool> m_filled = new Dictionary<Tuple<int, int>, bool>();

		public MarkovWordGenerator( int order )
		{
			m_order = order < 1 ? 3 : order;
			m_rand = null;

			m_chains = new Dictionary<string, Chain>();
			m_sampleLengths = new Dictionary<int, int>();
			m_used = new List<string>();
			m_nullChar = (char)0;
			m_totalSampleSize = 0;
		}

		public MarkovWordGenerator( BinaryReader br )
		{
			m_rand = null;
			m_chains = new Dictionary<string, Chain>();
			m_sampleLengths = new Dictionary<int, int>();
			m_used = new List<string>();

			ReadRawData( br );
		}

		public void SampleWords( IEnumerable<string> samples )
		{
			foreach( string s in samples )
				SampleWord( s );
		}

		public void SampleWord( string s )
		{
			string nulledWord, token;
			Chain entry;

			AddSampleLength( s.Length );
			nulledWord = String.Concat( new string( m_nullChar, 3 ), s.ToUpper(), m_nullChar );

			for( int letter = 0; letter < nulledWord.Length - m_order; letter++ )
			{
				token = nulledWord.Substring( letter, m_order );
				if( m_chains.ContainsKey( token ) )
					entry = m_chains[token];
				else
				{
					entry = new Chain( this );
					m_chains[token] = entry;
				}
				entry.AddCharacter( nulledWord[letter + m_order] );
			}
		}

		public void ResetUsedList()
		{
			m_used.Clear();
		}

		public string NextName( int minLength, int maxLength )
		{
			if( minLength < 1 )
				minLength = 1;
			minLength += m_order;
			maxLength += m_order;

			Tuple<int, int> sizeTuple = new Tuple<int, int>(minLength, maxLength);

			if ( !m_filled.ContainsKey( sizeTuple ) )
				m_filled[sizeTuple] = false;

			string word;

			if( !m_filled[sizeTuple] )
			{
				string token;
				int tries = 0;

				do
				{
					if( maxLength == -1 + m_order )
						maxLength = GetSampledSize();

					word = new string( m_nullChar, m_order );
					while( word.Length < maxLength )
					{
						token = word.Substring( word.Length - m_order, m_order );
						char c = m_chains[token].GetLetter();
						if( c != m_nullChar )
							word += c;
						else
							break;
					}

					tries++;
				} while( ( m_used.Contains( word ) || word.Length < minLength ) && tries < MAX_TRIES );

				// Fall back on a previous word, and clear previous words.
				m_filled[sizeTuple] = tries == MAX_TRIES;

				m_used.Add( word );
			} else
			{
				word = m_used.Where( s => s.Length >= minLength && s.Length < maxLength ).RandomItem( m_rand );
			}

			return ToTitleCase( word );
		}

		public void SetRandom( Random rand )
		{
			m_rand = rand;
		}


		private string ToTitleCase( string word )
		{
			char[] parts = word.ToCharArray( m_order, word.Length - m_order );
			for( int i = 1; i < parts.Length; i++ )
			{
				if( parts[i - 1] == ' ' )
					continue;
				parts[i] = Char.ToLower( parts[i] );
			}
			return new string( parts );
		}

		private int GetSampledSize()
		{
			int index = m_rand.Next( m_totalSampleSize );
			int num = 0;

			foreach( var pair in m_sampleLengths )
			{
				if( pair.Value > index )
				{
					num = pair.Key;
					break;
				}
				index -= pair.Value;
			}

			return num;
		}

		private void AddSampleLength( int len )
		{
			if( !m_sampleLengths.ContainsKey( len ) )
				m_sampleLengths.Add( len, 0 );
			m_sampleLengths[len]++;
			m_totalSampleSize++;
		}



		public void DumpRawData( BinaryWriter bw )
		{
			bw.Write( m_order );
			bw.Write( m_nullChar );

			bw.Write( m_totalSampleSize );
			bw.Write( m_sampleLengths.Count );
			foreach( var pair in m_sampleLengths )
			{
				bw.Write( pair.Key );
				bw.Write( pair.Value );
			}

			bw.Write( m_chains.Count );
			foreach( var chain in m_chains )
			{
				bw.Write( chain.Key );
				chain.Value.DumpData( bw );
			}
		}

		private void ReadRawData( BinaryReader br )
		{
			m_order = br.ReadInt32();
			m_nullChar = br.ReadChar();

			m_totalSampleSize = br.ReadInt32();

			int size = br.ReadInt32();
			for( int i = 0; i < size; i++ )
				m_sampleLengths.Add( br.ReadInt32(), br.ReadInt32() );

			size = br.ReadInt32();
			for( int i = 0; i < size; i++ )
			{
				m_chains.Add( br.ReadString(), Chain.ReadRawData( br, this ) );
			}
		}


		internal class Chain
		{
			private Dictionary<char, int> m_letters;
			private int m_totalSize;
			private MarkovWordGenerator m_parentGen;

			public Chain( MarkovWordGenerator gen )
			{
				m_letters = new Dictionary<char, int>();
				m_totalSize = 0;
				m_parentGen = gen;
			}

			public void AddCharacter( char letter )
			{
				if( !m_letters.ContainsKey( letter ) )
					m_letters.Add( letter, 0 );
				m_letters[letter]++;
				m_totalSize++;
			}

			public char GetLetter()
			{
				int index = m_parentGen.m_rand.Next( m_totalSize );
				char letter = (char)0;

				foreach( var pair in m_letters )
				{
					if( pair.Value > index )
					{
						letter = pair.Key;
						break;
					}
					index -= pair.Value;
				}

				return letter;
			}

			public void DumpData( BinaryWriter bw )
			{
				bw.Write( m_totalSize );
				bw.Write( m_letters.Count );
				foreach( var pair in m_letters )
				{
					bw.Write( pair.Key );
					bw.Write( pair.Value );
				}
			}

			public static Chain ReadRawData( BinaryReader br, MarkovWordGenerator gen )
			{
				Chain ch = new Chain( gen );

				ch.m_totalSize = br.ReadInt32();
				int size = br.ReadInt32();
				for( int i = 0; i < size; i++ )
					ch.m_letters.Add( br.ReadChar(), br.ReadInt32() );

				return ch;
			}
		}
	}
}
