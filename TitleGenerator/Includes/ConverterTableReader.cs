using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Parsers;

namespace TitleGenerator
{
	public class ConverterTableReader : ReaderBase
	{
		public Dictionary<string, string> Nations;

		public ConverterTableReader()
		{
			Nations = new Dictionary<string, string>();
			Errors = new List<string>();
		}

		public override void Parse( string filename )
		{
			FileInfo f = new FileInfo( filename );

			if ( f.Name != "nation_table.csv" )
				return;

			if( !f.Exists )
			{
				Errors.Add( string.Format( "File not found: {0}", filename ) );
				return;
			}

			string input;
			StreamReader sr = new StreamReader( f.Open( FileMode.Open, FileAccess.Read ), Encoding.GetEncoding( 1252 ) );

			while( ( input = sr.ReadLine() ) != null )
			{
				input = input.Trim();
				if ( input.StartsWith( "#" ) || input.StartsWith( ";" ) || input == string.Empty )
					continue;

				Nations[input.Split( ';' )[0]] = input;
			}

			sr.Dispose();
		}
		public override void ParseFolder( string folder )
		{
			throw new NotImplementedException();
		}
	}
}
