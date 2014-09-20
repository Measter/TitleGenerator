using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Measter;
using Parsers.Title;
using TitleGenerator.Includes;

namespace TitleGenerator.Tasks.TitleGeneration
{
	class LocalisationTask : SharedTask
	{
		public LocalisationTask( Options options, Logger log )
			: base( options, log )
		{

		}

		protected override bool Execute()
		{
			lock( TaskStatus.TitleCheck )
			{
			}

			if( TaskStatus.Abort )
				return false;

			string sFile = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			sFile = Path.Combine( sFile, "localisation/kaTitleNames.csv" ).Replace( '\\', '/' );
			FileInfo nameFile = new FileInfo( sFile );

			Log( "Creating Name File" );
			SendMessage( "Creating Names" );

			if( TaskStatus.Abort )
				return false;

			if( !nameFile.Directory.Exists )
				nameFile.Directory.Create();

			StreamWriter names = new StreamWriter( nameFile.Open( FileMode.Create, FileAccess.Write ),
												   Encoding.GetEncoding( 1252 ) );

			if( TaskStatus.Abort )
				return false;
			CreateStringFromCounties( names );

			if( TaskStatus.Abort )
				return false;
			CreateStringFromDuchies( names );

			if( TaskStatus.Abort )
				return false;
			CreateStringFromKingdoms( names );

			names.Dispose();

			return true;
		}


		private void CreateStringFromKingdoms( StreamWriter names )
		{
			if( !m_options.CreateEmpires )
				return;

			Log( "Creating Kingdom Names" );
			SendMessage( "Creating Kingdom Names" );

			foreach( var pair in m_options.Data.Kingdoms )
			{
				if( TaskStatus.Abort )
					return;

				Title k = pair.Value;
				if( k.Primary || k.Capital == -1 )
					continue;

				MakeNameForTitle( names, k.TitleID, null, m_options.EmpireShortNames, TitleLevel.Empire );
			}
		}

		private void CreateStringFromDuchies( StreamWriter names )
		{
			if( !m_options.CreateKingdoms )
				return;

			Log( "Creating Duchy Names" );
			SendMessage( "Creating Duchy Names" );

			foreach( var pair in m_options.Data.Duchies )
			{
				if( TaskStatus.Abort )
					return;

				Title d = pair.Value;
				if( d.Primary || d.Capital == -1 )
					continue;

				bool cont = MakeNameForTitle( names, d.TitleID, null, m_options.KingdomShortNames, TitleLevel.Kingdom );

				if( !cont || !m_options.CreateEmpires )
					continue;
				MakeNameForTitle( names, d.TitleID, null, m_options.EmpireShortNames, TitleLevel.Empire );
			}
		}

		private void CreateStringFromCounties( StreamWriter names )
		{
			if( !m_options.CreateDuchies )
				return;

			Log( "Creating County Names" );
			SendMessage( "Creating County Names" );

			foreach( var pair in m_options.Data.Counties )
			{
				if( TaskStatus.Abort )
					return;

				#region Duchies
				Title c = pair.Value;
				if( c.Primary )
					continue;

				//Check for existing duchy.
				Title t = m_options.Data.Duchies.ToList().Find( d => d.Value.TitleID == "d_" + c.TitleID.Substring( 2 ) ).Value;
				if( t != null )
					continue;

				string noun;
				if( m_options.Data.Localisations.ContainsKey( "PROV" + c.CountyID ) )
				{
					noun = m_options.Data.Localisations["PROV" + c.CountyID];
				} else
				{
					Log( " --Couldn't find localisation string for " + c.TitleID );
					continue;
				}

				Log( " --Writing names for " + "d_" + c.TitleID.Substring( 2 ) );

				names.WriteLine( "d_" + c.TitleID.Substring( 2 ) + noun.Substring( noun.IndexOf( ';' ) ) );
				if( m_options.Data.Localisations.ContainsKey( c.TitleID + "_adj" ) )
					names.WriteLine( "d_" + m_options.Data.Localisations[c.TitleID + "_adj"].Substring( 2 ) );
				#endregion

				if( !m_options.CreateKingdoms )
					continue;
				bool cont = MakeNameForTitle( names, "PROV" + c.CountyID, c.TitleID, m_options.KingdomShortNames,
											  TitleLevel.Kingdom );

				if( !cont || !m_options.CreateEmpires )
					continue;
				MakeNameForTitle( names, "PROV" + c.CountyID, c.TitleID, m_options.EmpireShortNames, TitleLevel.Empire );
			}
		}

		private bool MakeNameForTitle( StreamWriter names, string titleID, string countyTitleID,
									   bool shortName, TitleLevel level )
		{
			ReadOnlyDictionary<string, Title> titles;
			titles = level == TitleLevel.Kingdom ? m_options.Data.Kingdoms : m_options.Data.Empires;

			string id = countyTitleID ?? titleID;
			string prefix = level == TitleLevel.Kingdom ? "k_" : "e_";
			Log( " --Writing names for " + prefix + id.Substring( 2 ) );
			Title t = titles.ToList().Find( e => e.Value.TitleID == prefix + id.Substring( 2 ) ).Value;
			if( t != null )
				return false;

			string noun = GetNoun( shortName, titleID, level, countyTitleID );
			if ( noun == null )
			{
				Log( "   --Localisation for " + titleID + " doesn't exist." );
				return false;
			}
			names.WriteLine( prefix + id.Substring( 2 ) + noun.Substring( noun.IndexOf( ';' ) ) );
			if( m_options.Data.Localisations.ContainsKey( id + "_adj" ) )
				names.WriteLine( prefix + m_options.Data.Localisations[id + "_adj"].Substring( 2 ) );

			return true;
		}

		private string GetNoun( bool shortNames, string titleID, TitleLevel titleLevel, string countyTitleID )
		{
			string value = null;

			if( shortNames )
				value = GetNewName( titleID, titleLevel, countyTitleID );
			else if( m_options.Data.Localisations.ContainsKey( titleID ) )
				value = m_options.Data.Localisations[titleID];

			return value;
		}

		private string GetNewName( string titleID, TitleLevel titleLevel, string countyTitleID )
		{
			string id = countyTitleID ?? titleID;

			Log( " --Creating short name for: " + id );

			if( !m_options.Data.Localisations.ContainsKey( id + "_adj" ) )
			{
				if( m_options.Data.Localisations.ContainsKey( titleID ) )
					return m_options.Data.Localisations[titleID];
				return null;
			}

			string noun, adj;

			noun = m_options.Data.Localisations[titleID];
			adj = m_options.Data.Localisations[id + "_adj"];
			string[] nounBits = noun.Split( ';' );
			string[] adjBits = adj.Split( ';' );

			if( titleLevel == TitleLevel.Duchy )
				nounBits[1] = adjBits[1] + " Duchy";
			else if( titleLevel == TitleLevel.Kingdom )
				nounBits[1] = adjBits[1] + " Kingdom";
			else
				nounBits[1] = adjBits[1] + " Empire";

			noun = nounBits.Aggregate( String.Empty, ( c, bit ) => c + ( bit + ";" ) );
			noun = noun.TrimEnd( ';' );
			return noun;
		}
	}
}
