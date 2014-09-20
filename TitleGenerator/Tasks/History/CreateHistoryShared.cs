using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Parsers.Character;
using Parsers.Culture;
using Parsers.Dynasty;
using Parsers.Options;
using Parsers.Province;
using Parsers.Religion;
using Parsers.Title;
using TitleGenerator.HistoryRules;
using TitleGenerator.Includes;

namespace TitleGenerator.Tasks.History
{
	class CreateHistoryShared : SharedTask
	{
		private readonly EventOptionDateComparer m_eventDateComp = new EventOptionDateComparer();

		public CreateHistoryShared( Options options, Logger log )
			: base( options, log )
		{

		}

		protected override bool Execute()
		{
			string path = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			path = Path.Combine( path, "history/titles" ).Replace( '\\', '/' );
			if( !Directory.Exists( path ) )
				Directory.CreateDirectory( path );

			path = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			path = Path.Combine( path, "history/provinces" ).Replace( '\\', '/' );
			if( !Directory.Exists( path ) )
				Directory.CreateDirectory( path );

			path = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			path = Path.Combine( path, "history/characters/KAChars.txt" ).Replace( '\\', '/' );
			FileInfo charFile = new FileInfo( path );
			if( !charFile.Directory.Exists )
				charFile.Directory.Create();

			StreamWriter charWriter = new StreamWriter( charFile.Open( FileMode.Create, FileAccess.Write ),
														Encoding.GetEncoding( 1252 ) );

			bool ret = CreateHistory( charWriter );
			if( ret )
			{
				ret = CreateRulesetCharacters( charWriter );
			}
			charWriter.Close();

			SaveProvinceHistory();
			return ret;
		}

		protected virtual bool CreateHistory( StreamWriter charWriter )
		{
			return true;
		}


		protected bool CreateRulesetCharacters( StreamWriter charWriter )
		{
			Log( "Generating Ruleset Characters" );
			Dictionary<int, Dynasty> dyns = new Dictionary<int, Dynasty>( m_options.Data.Dynasties );
			List<Character> chars;

			foreach( CharacterRule cRule in m_options.RuleSet.CharRules )
			{
				if( !m_options.Data.ContainsCulture( cRule.Culture ) )
				{
					m_log.Log( String.Format( "Culture not found for ruleset character: {0}", cRule.Culture ), Logger.LogType.Error );
					continue;
				}
				if( !m_options.Data.Religions.ContainsKey( cRule.Religion ) )
				{
					m_log.Log( String.Format( "Religion not found for ruleset character: {0}", cRule.Religion ), Logger.LogType.Error );
					continue;
				}

				Culture cul = m_options.Data.GetCulture( cRule.Culture );
				Religion rel = m_options.Data.Religions[cRule.Religion];

				CharacterOption co = new CharacterOption();
				co.Culture = cul;
				co.SpecifiedCulture = true;
				co.Religion = rel;
				co.SpecifiedReligion = true;
				co.Gender = cRule.IsFemale ? RuleSet.Gender.Female : RuleSet.Gender.Male;
				if( cRule.ID != -1 )
					co.ID = cRule.ID;

				if( cRule.Dynasty != -1 && m_options.Data.Dynasties.ContainsKey( cRule.Dynasty ) )
				{
					co.Dynasty = m_options.Data.Dynasties[cRule.Dynasty];
					co.SpecifiedDynasty = true;
				}

				chars = CreateRandomCharacter( co, dyns, true );
				Log( string.Format( " --Name: {0} ID: {1}", chars[0].Name, chars[0].ID ) );

				int i;
				i = cRule.WriteCharacter ? 0 : 1;
				for( ; i < chars.Count; i++ )
					WriteCharacter( charWriter, chars[i] );

				//Output owned titles.
				foreach( string t in cRule.Titles )
				{
					Log( string.Format( " --Giving Title: {0}", t ) );

					Title tit;
					if( !m_options.Data.Counties.TryGetValue( t, out tit ) )
						if( !m_options.Data.Duchies.TryGetValue( t, out tit ) )
							if( !m_options.Data.Kingdoms.TryGetValue( t, out tit ) )
								m_options.Data.Empires.TryGetValue( t, out tit );

					string path = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
					path = Path.Combine( path, "history/titles/" + t + ".txt" ).Replace( '\\', '/' );

					WriteTitleOwner( new FileInfo( path ), chars[0] );
					MakeDeJureLiege( new FileInfo( path ), true, "", false, null );
				}
			}

			return true;
		}


		#region Output

		private void SaveProvinceHistory()
		{
			FileInfo curProv;
			Province prov;

			Log( "Saving province history" );

			foreach( var pair in m_options.Data.Provinces )
			{
				prov = pair.Value;
				if( m_options.RuleSet.IgnoredTitles.Contains( prov.Title ) )
					continue;

				curProv = new FileInfo( prov.Filename );

				string titlePath = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
				titlePath = Path.Combine( titlePath, "history/provinces/" ).Replace( '\\', '/' );
				titlePath = Path.Combine( titlePath, curProv.Name );

				using( StreamWriter sw = new StreamWriter( titlePath, false, Encoding.GetEncoding( 1252 ) ) )
				{
					sw.WriteLine( "title = " + prov.Title );
					sw.WriteLine();

					sw.WriteLine( "max_settlements = " + prov.MaxSettlements );
					foreach( Settlement s in prov.Settlements )
						sw.WriteLine( s.Title + " = " + s.Type );

					sw.WriteLine();
					sw.WriteLine( "culture = " + prov.Culture );
					sw.WriteLine( "religion = " + prov.Religion );
				}
			}
		}

		protected void WriteCharacter( StreamWriter sw, Character c )
		{
			sw.Write( c.ID );
			sw.WriteLine( " = {" );

			sw.WriteLine( "\tname = \"" + c.Name + "\"" );
			sw.WriteLine( "\tdynasty = \"" + c.Dynasty + "\"" );
			sw.WriteLine( "\tculture = \"" + c.Culture + "\"" );
			sw.WriteLine( "\treligion = \"" + c.Religion + "\"" );

			sw.WriteLine( "\tfemale = " + ( c.IsFemale ? "yes" : "no" ) );

			c.Events.Sort( m_eventDateComp );

			foreach( EventOption og in c.Events )
			{
				sw.Write( "\t" + og.GetIDString );
				sw.WriteLine( " = {" );

				foreach( Option o in og.SubOptions )
				{
					sw.Write( "\t\t" + o.GetIDString );
					sw.Write( " = " );
					sw.WriteLine( o.GetValueString );
				}

				sw.WriteLine( "\t" + "}" );
			}

			sw.WriteLine( "}" );
		}

		protected void WriteTitleOwner( FileInfo titleFile, Character curChars )
		{
			StreamWriter titleWriter = new StreamWriter( titleFile.Open( FileMode.Create, FileAccess.Write ),
														 Encoding.GetEncoding( 1252 ) );

			titleWriter.WriteLine( curChars.Events[0].GetIDString + " = {" );

			titleWriter.WriteLine( "\tholder = " + curChars.ID.ToString() );

			if( curChars.CustomFlags.ContainsKey( "laws" ) )
			{
				LawSet laws = (LawSet)curChars.CustomFlags["laws"];

				titleWriter.WriteLine( "\tlaw = " + laws.Succession );
				titleWriter.WriteLine( "\tlaw = " + laws.Gender );
				titleWriter.WriteLine( "\tlaw = " + laws.CityLevy );
				titleWriter.WriteLine( "\tlaw = " + laws.CityTax );
				if( laws.isMuslim )
				{
					titleWriter.WriteLine( "\tlaw = " + laws.IqtaLevy );
					titleWriter.WriteLine( "\tlaw = " + laws.IqtaTax );
				} else
				{
					titleWriter.WriteLine( "\tlaw = " + laws.FeudalLevy );
					titleWriter.WriteLine( "\tlaw = " + laws.FeudalTax );
					titleWriter.WriteLine( "\tlaw = " + laws.ChurchLevy );
					titleWriter.WriteLine( "\tlaw = " + laws.ChurchTax );
				}

				if( titleFile.Name.StartsWith( "k" ) )
					titleWriter.WriteLine( "\tlaw = " + laws.CrownAuthority );
			}

			titleWriter.WriteLine( "}" );

			titleWriter.Close();
		}

		protected void MakeDeJureLiege( FileInfo titleFile, bool setLiege, string liege, bool setDeJureLiege, string deJureLiege )
		{
			if( !setLiege && !setDeJureLiege )
				return;

			using( FileStream stream = titleFile.Open( FileMode.Append, FileAccess.Write ) )
			using( StreamWriter sw = new StreamWriter( stream, Encoding.GetEncoding( 1252 ) ) )
			{
				sw.WriteLine( ( m_options.StartDate - 2 ) + ".1.1 = {" );
				if( setDeJureLiege )
					sw.WriteLine( "\tde_jure_liege=\"" + deJureLiege + "\"" );
				if( setLiege )
					sw.WriteLine( "\tliege=\"" + ( String.IsNullOrEmpty( liege ) ? "0" : liege ) + "\"" );
				sw.WriteLine( "}" );
			}
		}
		#endregion


		#region Character Creation
		protected void MakeCharactersForTitles( StreamWriter charWriter, Dictionary<int, Dynasty> availDynasties, List<Title> titles,
								  bool setLiege, string liege, bool setDeJureLiege, string deJureLiege, Character presetChar, Character liegeChar )
		{
			List<Character> curChars = null;
			FileInfo titleFile;
			string titlePath = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );

			foreach( var title in titles )
			{
				if( TaskStatus.Abort )
					return;

				if( title.IsTitular )
					continue;

				if( title.Landless )
					continue;

				if( title.Culture == null || title.Religion == null )
					continue;

				Log( "Title " + title.TitleID );

				if( !m_options.Data.ContainsCulture( title.Culture )
					 || !m_options.Data.Religions.ContainsKey( title.Religion ) )
				{
					Log( " --Unable to find culture or religion" );
					continue;
				}

				if( m_options.RuleSet.IgnoredTitles.Contains( title.TitleID ) )
				{
					Log( " --Title in Ignore List" );
					continue;
				}

				if( presetChar == null )
				{
					Log( " --Creating Character" );

					CharacterOption charOptions = new CharacterOption();
					if( liegeChar == null || m_options.Random.Next( 0, 100 ) > m_options.RuleSet.LiegeCultureChance )
						charOptions.Culture = m_options.Data.GetCulture( title.Culture );
					else
						charOptions.Culture = m_options.Data.GetCulture( liegeChar.Culture );

					charOptions.SpecifiedCulture = true;

					if( liegeChar == null || m_options.Random.Next( 0, 100 ) > m_options.RuleSet.LiegeReligionChance )
						charOptions.Religion = m_options.Data.Religions[title.Religion];
					else
						charOptions.Religion = m_options.Data.Religions[liegeChar.Religion];

					charOptions.SpecifiedReligion = true;
					charOptions.IsSpouse = false;
					charOptions.Gender = RuleSet.Gender.Random;

					curChars = CreateRandomCharacter( charOptions, availDynasties, true );
					foreach( Character c in curChars )
						WriteCharacter( charWriter, c );
				} else
				{
					curChars = new List<Character>();
					curChars.Add( presetChar );
				}

				Log( " --Giving Title" );
				titleFile = new FileInfo( Path.Combine( titlePath, "history/titles/" + title.TitleID + ".txt" ).Replace( '\\', '/' ) );
				WriteTitleOwner( titleFile, curChars[0] );
				MakeDeJureLiege( titleFile, setLiege, liege, setDeJureLiege, deJureLiege );


				if( title.TitleID.StartsWith( "c_" ) )
					continue;

				//Randomise title order.
				List<Title> subTitles = title.SubTitles.Values.ToList().OrderBy( a => m_options.Random.Next() ).ToList();

				int reserved = 2;
				if( title.TitleID.StartsWith( "d_" ) )
					reserved = m_options.Random.Next( 3, 5 );
				Log( " --Giving First " + reserved + " Sub-Titles" );
				Title subTitle;
				for( int i = 0; i < reserved && i < subTitles.Count; i++ )
				{
					subTitle = subTitles[i];
					if( m_options.RuleSet.IgnoredTitles.Contains( subTitle.TitleID ) )
						continue;
					titleFile =
						new FileInfo( Path.Combine( titlePath, "history/titles/" + subTitle.TitleID + ".txt" ).Replace( '\\', '/' ) );
					if( subTitle.TitleID.StartsWith( "c_" ) )
					{
						WriteTitleOwner( titleFile, curChars[0] );
						MakeDeJureLiege( titleFile, true, title.TitleID, false, null );
					} else
					{
						List<Title> tList = new List<Title>();
						tList.Add( subTitle );
						MakeCharactersForTitles( charWriter, availDynasties, tList, true, title.TitleID, true, title.TitleID, curChars[0], null );
					}
				}
				if( subTitles.Count > reserved )
				{
					Log( " --Making Vassals for Remaining Sub-Titles" );
					List<Title> subTitleList = new List<Title>();
					for( int i = reserved; i < subTitles.Count; i++ )
						subTitleList.Add( subTitles[i] );
					MakeCharactersForTitles( charWriter, availDynasties, subTitleList, true, title.TitleID, true, title.TitleID, null, curChars[0] );
				}
			}
		}



		protected List<Character> CreateRandomCharacter( CharacterOption charOptions, Dictionary<int, Dynasty> availDynasties, bool outputLaws )
		{
			List<Character> retList = new List<Character>();

			#region This Character
			#region Culture, Religion, Dynasty
			Culture curCul;
			if( charOptions.SpecifiedCulture )
				curCul = charOptions.Culture;
			else
				curCul = charOptions.CultureList.RandomItem( m_options.Random );

			Religion curRel;
			if( charOptions.SpecifiedReligion )
				curRel = charOptions.Religion;
			else
				curRel = charOptions.ReligionList.RandomItem( m_options.Random );

			Dynasty dyn;

			if( charOptions.SpecifiedDynasty )
			{
				dyn = charOptions.Dynasty;
			} else
			{
				dyn = m_options.Data.GetDynastyByCulture( availDynasties, curCul, m_options.Random );
				while( dyn == null )
				{
					if( availDynasties.Count == 0 )
						availDynasties = new Dictionary<int, Dynasty>( m_options.Data.Dynasties );

					dyn = m_options.Data.GetDynastyByCulture( availDynasties,
															  m_options.Data.GetRandomCulture( m_options.Random ).Value,
															  m_options.Random );
				}
				availDynasties.Remove( dyn.ID );
			}

			#endregion

			Character curChar = new Character();
			retList.Add( curChar );


			int age;
			if( !charOptions.IsSpouse )
				age = m_options.Random.Normal( 17, 40 );
			else
				age = m_options.Random.Normal( charOptions.PartnerAge, 3.0 ).Clamp( 17, 99 );

			curChar.CustomFlags["age"] = age;

			int birthYear = m_options.StartDate - 1 - age;
			curChar.Events.Add( GetBirthDeathEvent( birthYear, "birth" ) );

			int deathYear = m_options.StartDate + m_options.Random.Normal( 5, 40 );
			curChar.Events.Add( GetBirthDeathEvent( deathYear, "death" ) );

			curChar.ID = charOptions.ID != 0 ? charOptions.ID : m_options.CharID++;
			curChar.Culture = curCul.Name;
			curChar.Religion = curRel.Name;
			curChar.Dynasty = dyn.ID;

			// Check rule set restrictions.
			if( charOptions.Gender == RuleSet.Gender.Random )
				charOptions.Gender = m_options.RuleSet.GetGender( curCul, curRel );

			if( charOptions.Gender == RuleSet.Gender.Random )
				curChar.IsFemale = ( m_options.Random.Next( 101 ) <= m_options.RuleSet.FemaleRulerChance );
			else
				curChar.IsFemale = charOptions.Gender == RuleSet.Gender.Female;

			curChar.Name = curCul.GetRandomName( curChar.IsFemale, m_options.Random );
			#endregion

			#region Spouse

			if( !charOptions.IsSpouse && age > 20 && m_options.Random.Next( 0, 101 ) < m_options.RuleSet.RulerSpouseChance )
			{
				CharacterOption spouseOptions = charOptions;

				spouseOptions.Gender = curChar.IsFemale ? RuleSet.Gender.Male : RuleSet.Gender.Female;
				spouseOptions.IsSpouse = true;
				spouseOptions.PartnerAge = age;
				spouseOptions.SpecifiedDynasty = false;
				spouseOptions.ID = 0;

				List<Character> posSpouse = CreateRandomCharacter( spouseOptions, availDynasties, false );

				curChar.CurrentSpouse = posSpouse[0];
				posSpouse[0].CurrentSpouse = curChar;

				int youngestAge = (int)curChar.CurrentSpouse.CustomFlags["age"];

				if( age < youngestAge )
					youngestAge = age;

				int marriageYear = m_options.Random.Next( 1, youngestAge - 16 );
				int month = m_options.Random.Next( 1, 12 );
				int day = m_options.Random.Next( 1, 28 );

				EventOption spouseEvent = new EventOption( new DateTime( m_options.StartDate - marriageYear, month, day ) );
				spouseEvent.SubOptions.Add( new IntegerOption( "add_spouse", curChar.ID ) );	
				curChar.CurrentSpouse.Events.Add( spouseEvent );

				EventOption curCharEvent = new EventOption( new DateTime( m_options.StartDate - marriageYear, month, day ) );
				curCharEvent.SubOptions.Add( new IntegerOption( "add_spouse", posSpouse[0].ID ) );
				curChar.Events.Add( curCharEvent );

				retList.Add( posSpouse[0] );
			}
			#endregion

			#region Law Generation

			if( !charOptions.IsSpouse )
			{
				LawSet laws = m_options.RuleSet.LawRules.GetLawSet( curCul, curRel, m_options.Random );
				if( outputLaws )
				{
					Log( " --Fetched realm laws: " +
								 "\n      Succession: " + laws.Succession +
								 "\n      Gender: " + laws.Gender +
								 "\n      Crown: " + laws.CrownAuthority +
								 "\n      Feudal Levy: " + laws.FeudalLevy +
								 "\n      Feudal Tax: " + laws.FeudalTax +
								 "\n      City Levy: " + laws.CityLevy +
								 "\n      City Tax: " + laws.CityTax +
								 "\n      Church Levy: " + laws.ChurchLevy +
								 "\n      Church Tax: " + laws.ChurchTax +
								 "\n      Iqta Levy: " + laws.IqtaLevy +
								 "\n      Iqta Tax:" + laws.IqtaTax );
				}
				curChar.CustomFlags["laws"] = laws;
			}
			#endregion

			return retList;
		}

		private EventOption GetBirthDeathEvent( int year, string eventName )
		{
			int month = m_options.Random.Next( 1, 12 );
			int day = m_options.Random.Next( 1, 28 );

			EventOption ev = new EventOption( new DateTime(year, month, day) );
			ev.SubOptions.Add( new StringOption( eventName, "yes", OptionType.ID ) );

			return ev;
		}
		#endregion
	}
}
