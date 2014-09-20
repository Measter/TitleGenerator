using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Measter;

namespace MarkovChainDump
{
	class Program
	{
		static void Main( string[] args )
		{
			List<char> charSet = LoadCharSet();
			if ( charSet == null )
				return;

			if ( !Directory.Exists( "database" ) )
				Directory.CreateDirectory( "database" );
			if ( !Directory.Exists( "output" ) )
				Directory.CreateDirectory( "output" );

			DirectoryInfo db = new DirectoryInfo( "database" );

			db.GetFiles().AsParallel().ForAll( f => ProcessFile( charSet, f ) );
			//foreach ( FileInfo fi in db.GetFiles("*.txt") )
			//{
			//	ProcessFile( charSet, fi );
			//}
			
			Console.WriteLine("Done");

			Console.Read();
		}

		private static void ProcessFile( List<char> charSet, FileInfo inFile )
		{
			MarkovWordGenerator gen = new MarkovWordGenerator( 3 );

			Console.WriteLine( "Processing file: {0}", inFile.Name );

			int counter = 0;
			List<string> skippedWords = new List<string>();
			using ( StreamReader sr = new StreamReader( inFile.FullName, Encoding.UTF8 ) )
			{
				bool skipWord;
				while ( !sr.EndOfStream )
				{
					++counter;
					if ( counter%50000 == 0 )
						Console.WriteLine( counter );

					skipWord = false;
					string line = sr.ReadLine();
					for ( int i = 0; i < line.Length; i++ )
					{
						char c = line[i];
						if ( !charSet.Contains( c ) )
						{
							skippedWords.Add( line );
							skipWord = true;
						}
					}

					if ( !skipWord )
						gen.SampleWord( line );
				}
			}

			FileInfo outFile = new FileInfo( Path.Combine( "output", Path.ChangeExtension( inFile.Name, ".bin" ) ) );
			using ( FileStream fs = outFile.Open( FileMode.Create, FileAccess.Write, FileShare.None ) )
			using ( BinaryWriter bw = new BinaryWriter( fs ) )
				gen.DumpRawData( bw );

			outFile = new FileInfo( Path.Combine( "output", inFile.Name + " - Skipped.txt" ) );
			using( FileStream fs = outFile.Open( FileMode.Create, FileAccess.Write, FileShare.None ) )
			using( StreamWriter sw = new StreamWriter( fs, Encoding.UTF8 ) )
				foreach ( string skippedWord in skippedWords )
					sw.WriteLine( skippedWord );
		}

		private static List<char> LoadCharSet()
		{
			FileInfo charFile = new FileInfo( "charset.bin" );

			if( !charFile.Exists )
			{
				Console.WriteLine( "Charset.bin file not found." );
				return null;
			}
			List<char> charSet = new List<char>();
			using ( FileStream fs = charFile.Open( FileMode.Open ) )
			using ( BinaryReader br = new BinaryReader( fs, Encoding.GetEncoding( 1252 ) ) )
			{
				Byte size = br.ReadByte();
				for ( int i = 0; i < size; i++ )
				{
					Char c = br.ReadChar();
					charSet.Add( c );
				}
			}
			return charSet;
		}
	}
}
