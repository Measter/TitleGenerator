using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Measter;
using Parsers.Title;

namespace TitleGenerator.Tasks.TitleGeneration
{
	class NationTableTask : SharedTask
	{
		public NationTableTask( Options options, Logger log ) : base( options, log )
		{
			
		}

		protected override bool Execute()
		{
			lock ( TaskStatus.TitleCheck )
			{
			}

			if ( TaskStatus.Abort )
				return false;

			if ( m_options.Data.NationTable.Count == 0 )
				return true; // If no table entries are loaded, it probably doesn't exist.

			string sFile = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			sFile = Path.Combine( sFile, "common/eu4_converter/nation_table.csv" ).Replace( '\\', '/' );
			FileInfo nationFile = new FileInfo( sFile );

			Log( "Creating Nation Converter Table" );
			SendMessage( "Creating Nation Converter Table" );

			if ( TaskStatus.Abort )
				return false;

			if ( !nationFile.Directory.Exists )
				nationFile.Directory.Create();

			StreamWriter nations = new StreamWriter( nationFile.Open( FileMode.Create, FileAccess.Write ), Encoding.GetEncoding( 1252 ) );

			OutputOriginal( nations );

			if( TaskStatus.Abort )
				return false;
			CreateTableFromCounties( nations );

			if( TaskStatus.Abort )
				return false;
			CreateTableFromDuchies( nations );

			if( TaskStatus.Abort )
				return false;
			CreateTableFromKingdoms( nations );

			nations.Dispose();

			return true;
		}

		private void OutputOriginal( StreamWriter nations )
		{
			nations.WriteLine( "# Original Contents" );

			foreach ( var pair in m_options.Data.NationTable )
				nations.WriteLine( pair.Value );

			nations.WriteLine();
			nations.WriteLine();
		}

		private void CreateTableFromKingdoms( StreamWriter nations )
		{
			if( !m_options.CreateEmpires )
				return;

			Log( "Creating Kingdom Table" );
			SendMessage( "Creating Kingdom Table" );
			
			nations.WriteLine( "# Generated From Kingdoms" );

			foreach( var pair in m_options.Data.Kingdoms )
			{
				if( TaskStatus.Abort )
					return;

				Title c = pair.Value;
				if( c.Primary || c.Capital == -1 )
					continue;

				CreateTableEntry( nations, c.TitleID, TitleLevel.Empire );
			}

			nations.WriteLine();
			nations.WriteLine();
		}

		private void CreateTableFromDuchies( StreamWriter nations )
		{
			if( !m_options.CreateKingdoms )
				return;

			Log( "Creating Duchy Table" );
			SendMessage( "Creating Duchy Table" );

			nations.WriteLine( "# Generated From Duchies" );

			foreach( var pair in m_options.Data.Duchies )
			{
				if( TaskStatus.Abort )
					return;

				Title c = pair.Value;
				if( c.Primary || c.Capital == -1 )
					continue;

				bool cont = CreateTableEntry( nations, c.TitleID, TitleLevel.Kingdom );

				if( !cont || !m_options.CreateEmpires )
					continue;
				CreateTableEntry( nations, c.TitleID, TitleLevel.Empire );
			}

			nations.WriteLine();
			nations.WriteLine();
		}

		private void CreateTableFromCounties( StreamWriter nations )
		{
			if ( !m_options.CreateDuchies )
				return;

			Log( "Creating County Table" );
			SendMessage( "Creating County Table" );

			nations.WriteLine( "# Generated From Counties" );

			foreach ( var pair in m_options.Data.Counties )
			{
				if ( TaskStatus.Abort )
					return;

				Title c = pair.Value;
				if ( c.Primary )
					continue;

				bool cont = CreateTableEntry( nations, c.TitleID, TitleLevel.Duchy );

				if ( !cont || !m_options.CreateKingdoms )
					continue;
				cont = CreateTableEntry( nations, c.TitleID, TitleLevel.Kingdom );

				if (!cont || !m_options.CreateEmpires )
					continue;
				CreateTableEntry( nations, c.TitleID, TitleLevel.Empire );
			}

			nations.WriteLine();
			nations.WriteLine();
		}

		private bool CreateTableEntry( StreamWriter nations, string titleID, TitleLevel level )
		{
			ReadOnlyDictionary<string, Title> titles;
			string prefix;

			switch ( level )
			{
				case TitleLevel.Duchy:
					titles = m_options.Data.Duchies;
					prefix = "d_";
					break;
				case TitleLevel.Kingdom:
					titles = m_options.Data.Kingdoms;
					prefix = "k_";
					break;
				default:
					titles = m_options.Data.Empires;
					prefix = "e_";
					break;
			}

			string convert;
			if( !m_options.Data.NationTable.TryGetValue( titleID, out convert ) )
				return false;

			Title t = titles.ToList().Find( e => e.Value.TitleID == prefix + titleID.Substring( 2 ) ).Value;
			if ( t != null )
				return false;

			nations.WriteLine( prefix + convert.Substring( 2 ) );

			return true;
		}
	}
}
