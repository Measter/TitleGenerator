using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Measter;
using Parsers.Options;
using Parsers.Title;
using TitleGenerator.Includes;

namespace TitleGenerator.Tasks.TitleGeneration
{
	class CreateTitleTask : SharedTask
	{
		public CreateTitleTask( Options options, Logger log )
			: base( options, log )
		{

		}

		protected override bool Execute()
		{
			Log( "Creating Titles" );
			SendMessage( "Creating Titles" );

			string path = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			path = Path.Combine( path, "common/landed_titles/KATitles.txt" ).Replace( '\\', '/' );
			FileInfo titleFile = new FileInfo( path );

			if( !titleFile.Directory.Exists )
				titleFile.Directory.Create();

			List<string> skippedList = new List<string>();

			Log( "Opening Title File" );
			StreamWriter titles = new StreamWriter( titleFile.Open( FileMode.Create, FileAccess.Write ), Encoding.GetEncoding( 1252 ) );

			if( TaskStatus.Abort )
				return false;
			CreateTitleFromCounties( titles, skippedList );

			if( TaskStatus.Abort )
				return false;
			CreateTitleFromDuchies( titles, skippedList );

			if( TaskStatus.Abort )
				return false;
			CreateTitleFromKingdoms( titles, skippedList );

			titles.Dispose();

			Log( "The Following Titles Were Skipped:" );

			foreach( string s in skippedList )
				Log( " --" + s );

			return true;
		}

		private void CreateTitleFromKingdoms( StreamWriter titles, List<string> skippedList )
		{
			if( !m_options.CreateEmpires )
				return;

			Log( "Creating Empire Titles" );
			SendMessage( "Creating Empires" );

			Title t;
			foreach( var pair in m_options.Data.Kingdoms )
			{
				if( TaskStatus.Abort )
					return;

				t = pair.Value;
				if( IsFilteredTitle( skippedList, t ) )
					continue;

				if( TitleExists( skippedList, t, TitleLevel.Empire ) )
					continue;
				titles.WriteLine( CreateTitleString( "e_", t ) );
			}
		}

		private void CreateTitleFromDuchies( StreamWriter titles, List<string> skippedList )
		{
			if( !m_options.CreateKingdoms )
				return;

			Log( "Creating Kingdom Titles" );
			SendMessage( "Creating Kingdoms" );

			Title t;
			foreach( var pair in m_options.Data.Duchies )
			{
				if( TaskStatus.Abort )
					return;

				t = pair.Value;
				if( IsFilteredTitle( skippedList, t ) )
					continue;

				if( TitleExists( skippedList, t, TitleLevel.Kingdom ) )
					continue;
				titles.WriteLine( CreateTitleString( "k_", t ) );

				if( !m_options.CreateEmpires || TitleExists( skippedList, t, TitleLevel.Empire ) )
					continue;
				titles.WriteLine( CreateTitleString( "e_", t ) );
			}
		}

		private void CreateTitleFromCounties( StreamWriter titles, List<string> skippedList )
		{
			if( !m_options.CreateDuchies )
				return;

			Log( "Creating Duchy Titles" );
			SendMessage( "Creating Duchies" );

			Title t;
			foreach( var pair in m_options.Data.Counties )
			{
				if( TaskStatus.Abort )
					return;

				t = pair.Value;
				if( IsFilteredTitle( skippedList, t ) )
					continue;

				if( TitleExists( skippedList, t, TitleLevel.Duchy ) )
					continue;
				titles.WriteLine( CreateTitleString( "d_", t ) );

				if( !m_options.CreateKingdoms || TitleExists( skippedList, t, TitleLevel.Kingdom ) )
					continue;
				titles.WriteLine( CreateTitleString( "k_", t ) );

				if( !m_options.CreateEmpires || TitleExists( skippedList, t, TitleLevel.Empire ) )
					continue;
				titles.WriteLine( CreateTitleString( "e_", t ) );
			}
		}


		private static bool IsFilteredTitle( List<string> skippedList, Title c )
		{
			if( c.Primary )
			{
				skippedList.Add( string.Format( "{0}: Is Primary.", c.TitleID ) );
				return true;
			}

			if( c.Capital == -1 )
			{
				skippedList.Add( string.Format( "{0}: No Capital.", c.TitleID ) );
				return true;
			}

			if( c.Landless )
			{
				skippedList.Add( string.Format( "{0}: Is Landless.", c.TitleID ) );
				return true;
			}
			return false;
		}

		private bool TitleExists( List<string> skippedList, Title c, TitleLevel level )
		{
			ReadOnlyDictionary<string, Title> titles;
			string prefix;

			if( level == TitleLevel.Duchy )
			{
				prefix = "d_";
				titles = m_options.Data.Duchies;
			} else if( level == TitleLevel.Kingdom )
			{
				prefix = "k_";
				titles = m_options.Data.Kingdoms;
			} else
			{
				prefix = "e_";
				titles = m_options.Data.Empires;
			}

			Title t = titles.ToList().Find( d => d.Value.TitleID == prefix + c.TitleID.Substring( 2 ) ).Value;
			if( t != null )
			{
				skippedList.Add( string.Format( "{0}: {1} already exists.", c.TitleID, level ) );
				return true;
			}
			return false;
		}

		private string CreateTitleString( string prefix, Title t )
		{
			Log( "Creating Title String for " + t.TitleID );

			StringBuilder sb = new StringBuilder();

			sb.AppendLine( prefix + t.TitleID.Substring( 2 ) + " = {" );

			#region Colours and Capital
			Log( " --Colours and Capital" );

			sb.Append( "\tcolor = { " );

			Color newColour = GetNewTitleColour( prefix, t );

			sb.AppendFormat( "{0} {1} {2}", newColour.R, newColour.G, newColour.B );
			sb.AppendLine( " }" );

			if( t.TwoColours )
			{
				sb.Append( "\tcolor2 = { " );
				sb.AppendFormat( "{0} {1} {2}", t.Colour2.R, t.Colour2.G, t.Colour2.B );
				sb.AppendLine( " }" );
			}

			if( prefix != "d_" )
			{
				if( t.Capital != -1 )
					sb.AppendLine( "\tcapital = " + t.Capital );
			} else
			{
				sb.AppendLine( "\tcapital = " + t.CountyID );
			}
			#endregion

			#region Short Names
			if( prefix == "e_" && m_options.EmpireShortNames )
				sb.AppendLine( "\tshort_name = yes" );

			if( prefix == "k_" && m_options.KingdomShortNames )
				sb.AppendLine( "\tshort_name = yes" );
			#endregion

			#region Misc Options
			if( t.Dignity != -1 )
				sb.AppendLine( "\tdignity = " + t.Dignity );
			if( t.CreationRequiresCapital == false )
				sb.AppendLine( "\tcreation_requires_capital = no" );
			if( t.PurpleBornHeirs )
				sb.AppendLine( "\tpurple_born_heirs = yes" );
			if( t.DuchyRevokation )
				sb.AppendLine( "\tduchy_revokation = yes" );

			if( t.CharTitle != null )
				sb.AppendLine( "\ttitle = " + t.CharTitle );
			if( t.FoA != null )
				sb.AppendLine( "\tfoa = " + t.FoA );
			if( t.CharTitleFemale != null )
				sb.AppendLine( "\ttitle_female = " + t.CharTitleFemale );
			if( t.TitlePrefix != null )
				sb.AppendLine( "\ttitle_prefix = " + t.TitlePrefix );
			#endregion

			//Culture localisation strings, crusade weights
			foreach( Option opt in t.MiscOptions )
			{
				if( m_options.Data.ContainsCulture( opt.GetIDString ) )
				{
					if ( opt.Type == OptionType.String || opt.Type == OptionType.ID )
						sb.AppendLine( "\t" + opt.GetIDString + " = " + opt.GetValueString );
				} else if( m_options.Data.Religions.ContainsKey( opt.GetIDString ) )
				{
					if( opt.Type == OptionType.Integer )
						sb.AppendLine( "\t" + opt.GetIDString + " = " + opt.GetValueString );
				}
			}

			#region Pagan Coat of Arms
			if( t.PaganCoA != null )
			{
				sb.Append( Option.Output( t.PaganCoA, '\t', 1 ) );
			}
			#endregion

			string scriptTemp = string.Empty;
			int capital = t.Capital;

			bool hasValidCultureReligion;
			if( t.Culture == null || t.Religion == null )
				hasValidCultureReligion = false;
			else if( m_options.Data.ContainsCulture( t.Culture ) && m_options.Data.Religions.ContainsKey( t.Religion ) )
				hasValidCultureReligion = true;
			else
				hasValidCultureReligion = false;

			#region Gain Effects
			Log( " --Writing Gain Effects" );

			if( prefix == "d_" )
			{
				scriptTemp = m_options.DuchyEffectsScript;
				capital = t.CountyID;
			}
			if( prefix == "k_" )
				scriptTemp = m_options.KingdomEffectsScript;
			if( prefix == "e_" )
				scriptTemp = m_options.EmpireEffectsScript;

			if( hasValidCultureReligion && !String.IsNullOrEmpty( scriptTemp ) )
			{
				scriptTemp = ParseScript( scriptTemp,
										  prefix + t.TitleID.Substring( 2 ),
										  t.Culture,
										  m_options.Data.GetCulture(t.Culture).Group.Name,
										  t.Religion,
										  m_options.Data.Religions[t.Religion].Group.Name,
										  capital,
										  "c" + t.TitleID.Substring( 1 ),
										  "d" + t.TitleID.Substring( 1 ),
										  "k" + t.TitleID.Substring( 1 ),
										  "e" + t.TitleID.Substring( 1 ),
										  m_options.CountyLimit,
										  m_options.DuchyLimit,
										  m_options.KingdomLimit );


				sb.AppendLine( "\tgain_effect = {" );
				sb.AppendLine( scriptTemp );
				sb.AppendLine( "\t}" ); //Close gain_effect 
			}
			#endregion

			#region Allows
			Log( " --Writing Allows" );

			if( prefix == "d_" )
				scriptTemp = m_options.DuchyAllowsScript;
			if( prefix == "k_" )
				scriptTemp = m_options.KingdomAllowsScript;
			if( prefix == "e_" )
				scriptTemp = m_options.EmpireAllowsScript;

			if( hasValidCultureReligion && !String.IsNullOrEmpty( scriptTemp ) )
			{
				scriptTemp = ParseScript( scriptTemp,
										  prefix + t.TitleID.Substring( 2 ),
										  t.Culture,
										  m_options.Data.GetCulture(t.Culture).Group.Name,
										  t.Religion,
										  m_options.Data.Religions[t.Religion].Group.Name,
										  capital,
										  "c" + t.TitleID.Substring( 1 ),
										  "d" + t.TitleID.Substring( 1 ),
										  "k" + t.TitleID.Substring( 1 ),
										  "e" + t.TitleID.Substring( 1 ),
										  m_options.CountyLimit,
										  m_options.DuchyLimit,
										  m_options.KingdomLimit );

				sb.AppendLine( "\tallow = {" );
				sb.AppendLine( scriptTemp );
				sb.AppendLine( "\t}" ); //Close allow  
			} else
			{
				sb.AppendLine( "\tallow = {" );
				sb.AppendLine( prefix + t.TitleID.Substring( 2 ) + "= { is_titular = no } " );
				sb.AppendLine( "\t}" ); //Close allow
			}
			#endregion

			sb.AppendLine( "}" ); //Close title.

			return sb.ToString();
		}

		private string ParseScript( string script, string genTitle, string culture,
									string culGroup, string religion, string relGroup,
									int capital, string county, string duchy, string kingdom,
									string empire, int countyLimit, int duchyLimit, int kingdomLimit )
		{
			string output = script;

			output = output.Replace( "__title__", genTitle );
			output = output.Replace( "__culture__", culture );
			output = output.Replace( "__culGroup__", culGroup );
			output = output.Replace( "__religion__", religion );
			output = output.Replace( "__relGroup__", relGroup );
			output = output.Replace( "__capital__", capital.ToString() );
			output = output.Replace( "__county__", county );
			output = output.Replace( "__duchy__", duchy );
			output = output.Replace( "__kingdom__", kingdom );
			output = output.Replace( "__empire__", empire );
			output = output.Replace( "__reqCount__", countyLimit.ToString() );
			output = output.Replace( "__reqDuchy__", duchyLimit.ToString() );
			output = output.Replace( "__reqKing__", kingdomLimit.ToString() );

			return output;
		}



		private Color GetNewTitleColour( string prefix, Title title )
		{
			if ( m_options.RandomTitleColour )
				return NewTitleColourRandom();

			return NewTitleColourFromPrevious( prefix, title );
		}

		private Color NewTitleColourRandom()
		{
			float hue = (float)m_options.Random.NextDouble();
			float sat = m_options.Random.Normal( 20, 75 )/100f;
			float val = m_options.Random.Normal( 20, 100 )/100f;

			return HSVtoRGB( hue, sat, val );
		}

		private Color NewTitleColourFromPrevious( string prefix, Title title )
		{
			float hue, sat, val;

			RGBtoHSV( title.Colour1, out hue, out sat, out val );

			int levDif = GetTitleLevelDifference( prefix, title.TitleID.Substring( 0, 2 ) );

			if ( title.TitleID[2]%2 == 0 )
				hue += m_options.Random.Next( 7*levDif )/100f;
			else
				hue -= m_options.Random.Next( 7*levDif )/100f;

			if ( hue > 1 )
				hue -= 1;
			if ( hue < 0 )
				hue += 1;

			return HSVtoRGB( hue, sat, val );
		}

		private int GetTitleLevelDifference( string prefix, string substring )
		{
			int baseTitle = 1, newTitle = 2;

			if( substring == "d_" )
				baseTitle = 2;
			else if( substring == "k_" )
				baseTitle = 3;

			if( prefix == "k_" )
				newTitle = 3;
			else if( prefix == "e_" )
				newTitle = 4;

			return newTitle - baseTitle;
		}
	}
}
