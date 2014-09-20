using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Measter;
using Parsers.Character;
using Parsers.Culture;
using Parsers.Dynasty;
using Parsers.Province;
using Parsers.Religion;
using Parsers.Title;
using TitleGenerator.HistoryRules;
using TitleGenerator.Includes;
using DebugBreak = TitleGenerator.Includes.DebugBreak;

namespace TitleGenerator.Tasks.History
{
	class FullHistoryTask : CreateHistoryShared
	{
		private List<Province> m_unownedProvs;
		private List<Province> m_unusableProvs;
		private int m_numRealms;
		private List<Character> m_spouses;
		private Dictionary<int, Dynasty> m_availDynasties;
		private StreamWriter m_charWriter;
		private List<Title> m_customDuchies;

		public FullHistoryTask( Options options, Logger log )
			: base( options, log )
		{

		}

		protected override bool CreateHistory( StreamWriter charWriter )
		{
			Log( "Generating Full History" );

			m_charWriter = charWriter;
			m_customDuchies = new List<Title>();

			string titlePath = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			titlePath = Path.Combine( titlePath, "history/provinces" ).Replace( '\\', '/' );
			if( !Directory.Exists( titlePath ) )
				Directory.CreateDirectory( titlePath );

			titlePath = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			titlePath = Path.Combine( titlePath, "common/landed_titles" ).Replace( '\\', '/' );
			if( !Directory.Exists( titlePath ) )
				Directory.CreateDirectory( titlePath );

			m_numRealms = m_options.Random.Next( m_options.HistoryMinChar, m_options.HistoryMaxChar );

			m_unownedProvs = FilterIgnoredProvinces();
			m_unusableProvs = new List<Province>();

			// Ensure max character count is <= the number of usable provinces.
			if( m_options.HistoryMaxChar > m_unownedProvs.Count - 1 )
				m_options.HistoryMaxChar = m_unownedProvs.Count - 1;

			MakeProvincesFeudal( m_unownedProvs );

			m_availDynasties = new Dictionary<int, Dynasty>( m_options.Data.Dynasties );

			GenerateCharacters();

			return true;
		}

		private void GenerateCharacters()
		{
			Log( " --Creating " + m_numRealms + " Characters" );
			SendMessage( "Setting History... Creating Characters" );

			m_spouses = new List<Character>();
			List<Character> repChars = MakeRepublicRealms();

			FilterSingleProvinces( m_unownedProvs, m_unusableProvs );

			List<Character> feudalChars = new List<Character>();
			MakeFeudalRealms( feudalChars );

			ConvertToTheocracy( feudalChars );

			List<Character> owners = new List<Character>();
			owners.AddRange( repChars );
			owners.AddRange( feudalChars );

			foreach( Character c in owners )
				WriteCharacter( m_charWriter, c );
			foreach( Character c in m_spouses )
				WriteCharacter( m_charWriter, c );

			WriteTitleOwners( owners );
			WriteCountyOwners( owners );

			FillEmptyProvinces();

			//Ensure all custom duchies have a liege.
			foreach( Title cd in m_customDuchies )
			{
				if( cd.CustomFlags.ContainsKey( "HasLiege" ) )
					continue;

				string titlePath = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
				titlePath = Path.Combine( titlePath, "history/titles" );
				titlePath = Path.Combine( titlePath, cd.TitleID + ".txt" ).Replace( '\\', '/' );
				MakeDeJureLiege( new FileInfo( titlePath ), false, null, true, cd.Parent.TitleID );
			}

			//Output Republics
			if( repChars.Count > 0 )
				WriteRepublics( repChars );
		}


		private void MakeRepublicChars( List<Character> owners, int reps )
		{
			// Get list of coastal provinces.
			List<Province> provs = m_unownedProvs.Where( p => p.IsCoastal ).ToList();
			Province prov;
			List<Province> provsOwnedByChar;
			List<Character> chars;
			Culture provCul;
			Religion provRel;

			for( int i = 0; i < reps; i++ )
			{
				if( provs.Count == 0 )
					break;

				int id = m_options.Random.Next( provs.Count );
				prov = provs[id];
				provs.Remove( prov );
				m_unownedProvs.Remove( prov );

				#region Filtering
				//Province doesn't have associated title.
				if( prov.Title == null || !m_options.Data.Counties.ContainsKey( prov.Title ) )
				{
					i--;
					m_log.Log( String.Format( "Province {0}-{1}: Title ID is null or doesn't exist.", prov.ID, prov.Title ),
							   Logger.LogType.Error );
					continue;
				}

				if( prov.Culture == null || prov.Religion == null ||
					!m_options.Data.ContainsCulture( prov.Culture ) ||
					!m_options.Data.Religions.ContainsKey( prov.Religion ) )
				{
					i--;
					m_log.Log( String.Format( "Province {0}-{1}: Culture or religion is null or doesn't exist. {2}, {3}",
											  prov.ID, prov.Title, prov.Culture, prov.Religion ),
							   Logger.LogType.Error );
					continue;
				}
				#endregion

				provRel = m_options.Data.Religions[prov.Religion];
				provCul = m_options.Data.GetCulture( prov.Culture );

				Log( "Title " + prov.Title );

				CharacterOption co = new CharacterOption();
				co.Culture = provCul;
				co.SpecifiedCulture = true;
				co.Religion = provRel;
				co.SpecifiedReligion = true;
				co.IsSpouse = false;
				co.Gender = RuleSet.Gender.Male;

				chars = CreateRandomCharacter( co, m_availDynasties, true );

				chars[0].CustomFlags["duchies"] = new List<Title>();
				chars[0].CustomFlags["kingdoms"] = new List<Title>();
				chars[0].CustomFlags["isRep"] = true;
				chars[0].CustomFlags["neighbours"] = new List<int>();
				chars[0].CustomFlags["Liege"] = null;
				chars[0].CustomFlags["Tier"] = TitleTier.Count;

				prov.CustomFlags["charOwned"] = true;
				prov.CustomFlags["charOwner"] = chars[0].ID;

				provsOwnedByChar = new List<Province>();
				chars[0].CustomFlags["provs"] = provsOwnedByChar;
				provsOwnedByChar.Add( prov );

				owners.Add( chars[0] );
				if( chars.Count == 2 )
					m_spouses.Add( chars[1] );
			}
		}

		private List<Character> MakeRepublicRealms()
		{
			List<Character> owners = new List<Character>();

			int reps = m_options.Random.Next( m_options.HistoryMinReps, m_options.HistoryMaxReps );
			if( reps == 0 )
				return owners;

			m_numRealms -= reps;

			Log( " --Creating " + reps + " Republics" );
			SendMessage( "Setting History... Creating Republics" );

			MakeRepublicChars( owners, reps );

			Log( " --Growing Republic Realms" );
			if( m_options.RuleSet.RepublicExpandMax >= 2 && m_options.RuleSet.RepublicExpandChance != 0 )
			{
				bool finishedGrowing;
				do
				{
					finishedGrowing = true;
					foreach( Character owner in owners )
						GrowRepublicRealm( owner, ref finishedGrowing );
				} while( !finishedGrowing );
			}

			Log( " --Making Republic Holdings Cities" );
			ChangeProvinceType( owners, "city" );

			Log( " --Promoting Republics" );
			foreach( Character ch in owners )
				MakeDukes( ch, null );

			return owners;
		}

		private void GrowRepublicRealm( Character owner, ref bool finishedGrowing )
		{
			if( owner.CustomFlags.ContainsKey( "atMaxSize" ) )
				return;

			List<Province> provsOwnedByChar;
			provsOwnedByChar = (List<Province>)owner.CustomFlags["provs"];
			int currCount = provsOwnedByChar.Count;
			int realmSize = currCount;
			List<int> neighbours = (List<int>)owner.CustomFlags["neighbours"];
			int neighbourID;

			if( currCount >= m_options.RuleSet.RepublicExpandMax )
			{
				owner.CustomFlags["atMaxSize"] = true;
				return;
			}

			for( int i = 0; i < currCount; i++ )
			{
				Province ownProv = provsOwnedByChar[i];
				if( ownProv.CustomFlags.ContainsKey( "charSurrounded" ) )
					continue;

				foreach( Province adj in ownProv.Adjacencies )
				{
					if( realmSize >= m_options.RuleSet.RepublicExpandMax )
					{
						owner.CustomFlags["atMaxSize"] = true;
						return;
					}

					if( m_options.RuleSet.IgnoredTitles.Contains( adj.Title ) )
						continue;
					if( !adj.IsCoastal )
						continue;
					if( m_options.Random.Next( 0, 100 ) > m_options.RuleSet.RepublicExpandChance )
					{
						owner.CustomFlags["atMaxSize"] = true;
						return;
					}

					if( adj.CustomFlags.ContainsKey( "charOwned" ) )
					{
						neighbourID = (int)adj.CustomFlags["charOwner"];
						if( !neighbours.Contains( neighbourID ) )
							neighbours.Add( neighbourID );
						continue;
					}

					adj.CustomFlags["charOwned"] = true;
					adj.CustomFlags["charOwner"] = owner.ID;
					provsOwnedByChar.Add( adj );
					m_unownedProvs.Remove( adj );
					finishedGrowing = false;
					realmSize++;
				}
				ownProv.CustomFlags["charSurrounded"] = true;
			}
		}

		private void WriteRepublics( List<Character> repChars )
		{
			Log( " --Writing Republics" );
			SendMessage( "Setting History... Writing Republics" );

			string titlePath = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			titlePath = Path.Combine( titlePath, "common/landed_titles/KATitlesRep.txt" ).Replace( '\\', '/' );
			StreamWriter titleWriter = new StreamWriter( titlePath, false, Encoding.GetEncoding( 1252 ) );

			Character c;
			List<Province> provsOwnedByChar;
			string liege;
			int id;

			for( int i = 0; i < repChars.Count; i++ )
			{
				c = repChars[i];
				liege = ( (List<Title>)c.CustomFlags["duchies"] )[0].TitleID;
				provsOwnedByChar = (List<Province>)c.CustomFlags["provs"];
				id = ( i * 6 ) + 1;

				//Output family holding.
				string name = GetFamilyHoldingName( id );

				Log( String.Format( "Character {0}, ID: {1}:", c.Name, c.ID ) );
				Log( String.Format( " --Holding {0}", name ) );

				//Title definition.
				titleWriter.WriteLine( name + " = {" );
				titleWriter.WriteLine( "\tculture = " + c.Culture );
				titleWriter.WriteLine( "\treligion = " + c.Religion );
				titleWriter.WriteLine( "}" );

				//Title history
				titlePath = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
				titlePath = Path.Combine( titlePath, "history/titles/" + name + ".txt" ).Replace( '\\', '/' );
				using( StreamWriter sw = new StreamWriter( titlePath, false, Encoding.GetEncoding( 1252 ) ) )
				{
					DateTime outDate = c.Events[0].Date.AddDays( 1 );

					sw.Write( outDate.Year + "." + outDate.Month );
					sw.WriteLine( "." + outDate.Day + "= {" );
					sw.WriteLine( "\tholding_dynasty = " + c.Dynasty );
					sw.WriteLine( "\tliege = \"" + liege + "\"" );
					sw.WriteLine( "\tholder = " + c.ID.ToString() );
					sw.WriteLine( "}" );
				}

				//Update province history;
				foreach( Province p in provsOwnedByChar )
				{
					//Barony history
					titlePath = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
					titlePath = Path.Combine( titlePath, "history/titles/" + p.Settlements[0].Title + ".txt" ).Replace( '\\', '/' );
					using( StreamWriter sw = new StreamWriter( titlePath, false, Encoding.GetEncoding( 1252 ) ) )
					{
						DateTime outDate = c.Events[0].Date.AddDays( 1 );

						sw.Write( outDate.Year + "." + outDate.Month );
						sw.WriteLine( "." + outDate.Day + "= {" );
						sw.WriteLine( "\tholder = " + c.ID );
						sw.WriteLine( "}" );
					}
				}

				CreatePatritians( titleWriter, liege, id, c.Culture, c.Religion );
			}

			titleWriter.Close();
		}

		private void CreatePatritians( StreamWriter titleWriter, string liege, int id, string culture, string religion )
		{
			int numPat = m_options.Random.Next( 2, 5 );
			List<Character> chars;
			Log( string.Format( " --Creating {0} Patricians", numPat ) );
			for( int i = 1; i <= numPat; i++ )
			{
				Log( string.Format( "   --Patrician {0}:", i ) );

				string name = GetFamilyHoldingName( id + i );
				Log( String.Format( "     Holding: {0}", name ) );

				CharacterOption co = new CharacterOption();
				co.Culture = m_options.Data.GetCulture( culture );
				co.SpecifiedCulture = true;
				co.Religion = m_options.Data.Religions[religion];
				co.SpecifiedReligion = true;
				co.Gender = RuleSet.Gender.Male;
				co.IsSpouse = false;

				chars = CreateRandomCharacter( co, m_availDynasties, false );
				foreach( Character c in chars )
					WriteCharacter( m_charWriter, c );
				Log( String.Format( "     ID: {0}", chars[0].ID ) );

				//Title definition.
				titleWriter.WriteLine( name + " = {" );
				titleWriter.WriteLine( "\tculture = " + culture );
				titleWriter.WriteLine( "\treligion = " + religion );
				titleWriter.WriteLine( "}" );

				//Title history
				string titlePath = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
				titlePath = Path.Combine( titlePath, "history/titles/" + name + ".txt" ).Replace( '\\', '/' );
				using( StreamWriter sw = new StreamWriter( titlePath, false, Encoding.GetEncoding( 1252 ) ) )
				{
					DateTime outDate = chars[0].Events[0].Date.AddDays( 1 );

					sw.Write( outDate.Year + "." + outDate.Month );
					sw.WriteLine( "." + outDate.Day + "= {" );
					sw.WriteLine( "\tholding_dynasty = " + chars[0].Dynasty );
					sw.WriteLine( "\tliege = \"" + liege + "\"" );
					sw.WriteLine( "\tholder = " + chars[0].ID.ToString() );
					sw.WriteLine( "}" );
				}
			}
		}

		private string GetFamilyHoldingName( int i )
		{
			string parts = string.Empty;

			int rem;
			while( i > 0 )
			{
				i = Math.DivRem( i, 4, out rem );

				switch( rem )
				{
					case 0:
						parts = "ze" + parts;
						break;
					case 1:
						parts = "on" + parts;
						break;
					case 2:
						parts = "tw" + parts;
						break;
					case 3:
						parts = "th" + parts;
						break;
				}
			}

			return "b_rep_" + parts;
		}



		#region Promotion
		private void MakeKings( Character owner, Character liege )
		{
			Log( "Making Kings" );
			SendMessage( "Setting History... Making Kings" );

			List<Title> possibleKingdoms = null;
			Title homeTitle;

			List<Province> provsOwnedByChar = (List<Province>)owner.CustomFlags["provs"];
			List<Title> duchiesOwnedByChar = (List<Title>)owner.CustomFlags["duchies"];
			List<Title> kingdomsOwnerByChar = (List<Title>)owner.CustomFlags["kingdoms"];

			if( duchiesOwnedByChar.Count == 0 )
				return; //Can't be a king or have vassals without a duchy;

			bool isKing = WillBeKing( duchiesOwnedByChar, provsOwnedByChar );

			if( isKing )
			{
				possibleKingdoms = GetPossibleKingdoms( duchiesOwnedByChar, owner );
				homeTitle = duchiesOwnedByChar[0];

				owner.CustomFlags["kingdoms"] = possibleKingdoms;

				if( m_options.CreateKingdoms && m_options.Random.NextDouble() > 0.5 )
				{
					owner.CustomFlags["HasCustomKingdom"] = true;
					Title customKingdom = new Title();
					customKingdom.TitleID = "k_" + homeTitle.TitleID.Substring( 2 );
					customKingdom.Culture = homeTitle.Culture;
					customKingdom.CustomFlags["isCustom"] = true;


					if( !m_options.Data.Kingdoms.ContainsKey( customKingdom.TitleID ) )
					{
						customKingdom.Parent = homeTitle.Parent;
						possibleKingdoms.Insert( 0, customKingdom );
					}
				}

				kingdomsOwnerByChar.AddRange( possibleKingdoms );
			}

			if( !isKing || kingdomsOwnerByChar.Count == 0 )
				owner.CustomFlags["Tier"] = TitleTier.Duke;
		}

		private List<Title> GetPossibleKingdoms( List<Title> duchiesOwnedByChar, Character owner )
		{
			List<Title> pos = new List<Title>();

			int ownedInKingdom;
			Title kingdom;

			foreach( Title title in duchiesOwnedByChar )
			{
				ownedInKingdom = 0;
				kingdom = title.Parent;
				if( kingdom.CustomFlags.ContainsKey( "owned" ) )
					continue;

				if( m_options.RuleSet.IgnoredTitles.Contains( kingdom.TitleID ) )
					continue;

				foreach( var subTitle in kingdom.SubTitles )
				{
					if( !subTitle.Value.CustomFlags.ContainsKey( "owner" ) )
						continue;

					int ownerID = (int)subTitle.Value.CustomFlags["owner"];
					if( ownerID == owner.ID )
						ownedInKingdom++;
				}

				double percentOwned = (double)ownedInKingdom / kingdom.SubTitles.Count;
				if( percentOwned < 0.55 )
					continue;

				if( !pos.Contains( kingdom ) )
					pos.Add( kingdom );
			}

			return pos;
		}

		private bool WillBeKing( List<Title> duchiesOwnedByChar, List<Province> provsOwnedByChar )
		{
			if( duchiesOwnedByChar.Count < m_options.DuchyLimit ||
				 provsOwnedByChar.Count < m_options.RuleSet.KingdomMinSize )
			{
				return false;
			}

			int size = provsOwnedByChar.Count;
			double kingChance = size / ( m_options.RuleSet.KingdomMinSize * 1.5 );

			if( m_options.Random.NextDouble() > kingChance )
				return false;

			return true;
		}


		private void MakeDukes( Character owner, Character liege )
		{
			Title duchy = null;
			bool? createDuchy;

			List<Province> provsOwnerByChar = (List<Province>)owner.CustomFlags["provs"];
			List<Title> duchiesOwnedByChar = (List<Title>)owner.CustomFlags["duchies"];

			if( owner.CustomFlags.ContainsKey( "isRep" ) )
			{
				//always make republics a duchy.
				if( m_options.RuleSet.RepsForceCustomDuchies == false || m_options.CreateDuchies == false )
				{
					//duchy = m_options.Data.Counties[provsOwnerByChar[0].Title].Parent;
					foreach( Province prov in provsOwnerByChar )
					{
						duchy = m_options.Data.Counties[prov.Title].Parent;
						if( !duchy.CustomFlags.ContainsKey( "owned" ) )
							break;
					}
					if( duchy == null )
					{
						if( duchiesOwnedByChar.Count == 0 )
							owner.CustomFlags["Tier"] = TitleTier.Count;
						return; // Unplayable duchy. 
					}
					duchy.CustomFlags["owned"] = true;
					duchiesOwnedByChar.Add( duchy );
					createDuchy = null;
				} else
				{
					//Hackery to make it work.
					createDuchy = true;
					duchy = new Title();
					duchy.SubTitles.Add( m_options.Data.Counties[provsOwnerByChar[0].Title].TitleID,
										 m_options.Data.Counties[provsOwnerByChar[0].Title] );
					duchiesOwnedByChar.Add( duchy );
				}
			} else
			{
				GetFeudalDuchies( owner, duchiesOwnedByChar, provsOwnerByChar );

				createDuchy = null;
			}

			if( createDuchy == null )
				createDuchy = ( m_options.CreateDuchies && m_options.Random.NextDouble() > 0.5
								&& duchiesOwnedByChar.Count > 0 );

			if( createDuchy == true )
			{
				owner.CustomFlags["HasCustomDuchy"] = true;
				Title homeTitle = duchiesOwnedByChar[0].SubTitles.Values.ToList()[0];
				Title customDuchy = new Title();

				customDuchy.TitleID = "d_" + homeTitle.TitleID.Substring( 2 );
				customDuchy.Culture = homeTitle.Culture;
				customDuchy.CustomFlags["isCustom"] = true;

				if( !m_options.Data.Duchies.ContainsKey( customDuchy.TitleID ) )
				{
					// More hackery.
					if( owner.CustomFlags.ContainsKey( "isRep" ) )
						duchiesOwnedByChar.RemoveAt( 0 );

					m_customDuchies.Add( customDuchy );
					customDuchy.Parent = homeTitle.Parent.Parent;
					duchiesOwnedByChar.Insert( 0, customDuchy );
				}
			}

			if( duchiesOwnedByChar.Count == 0 )
				owner.CustomFlags["Tier"] = TitleTier.Count;
		}

		private void GetFeudalDuchies( Character owner, List<Title> duchiesOwnedByChar, List<Province> provsOwnerByChar )
		{
			int ownedInDuchy;
			Province prov;
			Title duchy;

			foreach( Province p in provsOwnerByChar )
			{
				ownedInDuchy = 0;

				//A province may not have an associated county title.
				if( p.Title == null || !m_options.Data.Counties.ContainsKey( p.Title ) )
					continue;

				duchy = m_options.Data.Counties[p.Title].Parent;
				if( duchy.CustomFlags.ContainsKey( "owned" ) || m_options.RuleSet.IgnoredTitles.Contains( duchy.TitleID ) )
					continue;

				foreach( var pair in duchy.SubTitles )
				{
					if( !m_options.Data.Provinces.ContainsKey( pair.Value.CountyID ) )
						continue;

					prov = m_options.Data.Provinces[pair.Value.CountyID];

					if( !prov.CustomFlags.ContainsKey( "charOwned" ) )
						continue;

					int ownerID = (int)prov.CustomFlags["charOwner"];
					if( ownerID == owner.ID )
						ownedInDuchy++;
				}

				double percentOwned = (double)ownedInDuchy / duchy.SubTitles.Count;

				if( percentOwned < 0.55 )
					continue;

				duchy.CustomFlags["owned"] = true;
				duchy.CustomFlags["owner"] = owner.ID;
				if( !duchiesOwnedByChar.Contains( duchy ) )
					duchiesOwnedByChar.Add( duchy );
			}
		}
		#endregion


		private void ConvertToTheocracy( List<Character> owners )
		{
			Log( " --Making Theocracies" );
			int numTheocs = m_options.Random.Next( m_options.HistoryMinTheoc, m_options.HistoryMaxTheoc + 1 );
			if( numTheocs > 0 )
			{
				List<Character> theocChars = new List<Character>();
				List<Character> possibleChars = new List<Character>( owners );
				for( int i = 0; i < numTheocs; i++ )
				{
					Character ch = possibleChars.RandomItem( m_options.Random );
					possibleChars.Remove( ch );
					Log( "   --" + ch.ID );
					theocChars.Add( ch );
					if( possibleChars.Count == 0 )
						break;
				}
				ChangeProvinceType( theocChars, "temple" );
			}
		}



		private void MakeFeudalRealms( List<Character> allChars, Character liege = null,
									   TitleTier maxTier = TitleTier.King, int numRealms = -1, List<Province> usableProvs = null )
		{
			Log( " --Creating " + m_numRealms + " Feudal Realms" );
			SendMessage( "Setting History... Creating Realms" );

			if( numRealms == -1 )
				numRealms = m_numRealms;
			if( usableProvs == null )
				usableProvs = m_unownedProvs;

			// Create characters and randomly assign them home provinces.
			List<Character> owners = CreateNewFeudalCharacters( numRealms, usableProvs, liege );

			Log( " --Growing Territory" );
			SendMessage( "Setting History... Growing Territory" );

			#region Growth Phase
			bool finishedGrowing;
			do
			{
				finishedGrowing = true;
				foreach( Character owner in owners )
					GrowRealm( owner, ref finishedGrowing, usableProvs );
			} while( !finishedGrowing );
			#endregion

			List<Character> vassals = new List<Character>();

			//Set potential tier.
			if( maxTier > TitleTier.Count )
			{
				foreach( Character ch in owners )
				{
					SetRealmTier( ch, maxTier );

					TitleTier tier = (TitleTier)ch.CustomFlags["Tier"];
					if( tier >= TitleTier.Duke )
						MakeDukes( ch, liege );
					if( tier >= TitleTier.King )
						MakeKings( ch, liege );

					if( tier > TitleTier.Count )
						vassals.AddRange( MakeVassals( ch ) );
				}
			}

			allChars.AddRange( vassals );
			allChars.AddRange( owners );
		}

		private List<Character> MakeVassals( Character liege )
		{
			Log( " -- Making vassals for " + liege.ID );

			List<Character> vassals = new List<Character>();

			TitleTier vassalTier = (TitleTier)liege.CustomFlags["Tier"];
			switch( vassalTier )
			{
				case TitleTier.Duke:
					vassalTier = TitleTier.Count;
					break;
				case TitleTier.King:
					vassalTier = TitleTier.Duke;
					break;
				case TitleTier.Emperor:
					vassalTier = TitleTier.King;
					break;
				default:
					return vassals;
			}

			List<Province> usable = GetUnReservedProvinces( liege );

			if( !usable.Any() )
				return vassals;

			//Prepare for vassal growth.
			foreach( Province prov in usable )
			{
				prov.CustomFlags.Remove( "charSurrounded" );
				prov.CustomFlags.Remove( "charOwned" );
				prov.CustomFlags.Remove( "charOwner" );
			}

			int vassalSize = 1;
			switch( vassalTier )
			{
				case TitleTier.Count:
					vassalSize = usable.Count / 3;
					break;
				case TitleTier.Duke:
					vassalSize = usable.Count / m_options.RuleSet.DuchyMinSize;
					break;
				case TitleTier.King:
					vassalSize = usable.Count / m_options.RuleSet.KingdomMinSize;
					break;
			}

			int numVassals = vassalTier == TitleTier.Count
										   ? vassalSize
										   : m_options.Random.Next( (int)( vassalSize * 0.75 ), (int)( vassalSize * 1.25 ) );

			MakeFeudalRealms( vassals, liege, vassalTier, numVassals, usable );

			// Ensure *all* provinces are filled.
			if( usable.Any() )
				MakeFeudalRealms( vassals, liege, TitleTier.Count, usable.Count, usable );

			return vassals;
		}

		private List<Province> GetUnReservedProvinces( Character liege )
		{
			List<Province> owned = (List<Province>)liege.CustomFlags["provs"];
			List<Province> usable = new List<Province>( owned.Skip( 5 ) );
			liege.CustomFlags["provs"] = owned.Take( 5 ).ToList();

			UnreserveLiegeDuchiesKingdoms( liege );

			return usable;
		}

		private void UnreserveLiegeDuchiesKingdoms( Character liege )
		{
			List<Province> ownedProvs = (List<Province>)liege.CustomFlags["provs"];
			List<Title> realmDuchies = (List<Title>)liege.CustomFlags["duchies"];

			List<Title> newDuchies = new List<Title>();
			foreach( Province prov in ownedProvs )
			{
				Title county = m_options.Data.Counties[prov.Title];

				Title duchy = realmDuchies.SingleOrDefault( d => d.SubTitles.ContainsKey( county.TitleID ) );
				if( duchy != null )
				{
					newDuchies.Add( duchy );
					realmDuchies.Remove( duchy );
				}
			}

			// Needs at least one duchy.
			if( newDuchies.Count == 0 )
			{
				Title first = realmDuchies.First();
				realmDuchies.Remove( first );
				newDuchies.Add( first );
			}

			foreach( Title duchy in realmDuchies )
			{
				duchy.CustomFlags.Remove( "owned" );
				duchy.CustomFlags.Remove( "owner" );
			}

			liege.CustomFlags["duchies"] = newDuchies;
			liege.CustomFlags["realmDuchies"] = realmDuchies;
		}

		private List<Character> CreateNewFeudalCharacters( int numRealms, List<Province> usableProvs, Character liege = null )
		{
			Log( " --Creating Feudal Characters" );

			List<Character> owners = new List<Character>( numRealms );
			List<Character> chars;
			Province prov;
			List<Province> provsOwnedByChar;
			Culture provCul;
			Religion provRel;

			for( int i = 0; i < numRealms; i++ )
			{
				if( usableProvs.Count == 0 )
					break;

				int id = m_options.Random.Next( usableProvs.Count );

				prov = usableProvs[id];
				usableProvs.Remove( prov );

				#region Filtering
				//Province doesn't have associated title.
				if( prov.Title == null || !m_options.Data.Counties.ContainsKey( prov.Title ) )
				{
					i--;
					m_log.Log( String.Format( "Province {0}-{1}: Title ID is null or doesn't exist.", prov.ID, prov.Title ),
							   Logger.LogType.Error );
					continue;
				}

				if( prov.Culture == null || prov.Religion == null ||
					!m_options.Data.ContainsCulture( prov.Culture ) ||
					!m_options.Data.Religions.ContainsKey( prov.Religion ) )
				{
					i--;
					m_log.Log( String.Format( "Province {0}-{1}: Culture or religion is null or doesn't exist. {2}, {3}",
											  prov.ID, prov.Title, prov.Culture, prov.Religion ),
							   Logger.LogType.Error );
					continue;
				}
				#endregion

				Log( "Title " + prov.Title );

				// Need to handle chance of liege culture and religion.	
				if( liege != null && m_options.Random.Next( 100 ) <= m_options.RuleSet.LiegeCultureChance )
					provCul = m_options.Data.GetCulture( liege.Culture );
				else
					provCul = m_options.Data.GetCulture( prov.Culture );

				if( liege != null && m_options.Random.Next( 100 ) > m_options.RuleSet.LiegeReligionChance )
					provRel = m_options.Data.Religions[liege.Religion];
				else
					provRel = m_options.Data.Religions[prov.Religion];

				CharacterOption co = new CharacterOption();
				co.Culture = provCul;
				co.SpecifiedCulture = true;
				co.Religion = provRel;
				co.SpecifiedReligion = true;
				co.IsSpouse = false;
				co.Gender = RuleSet.Gender.Random;

				chars = CreateRandomCharacter( co, m_availDynasties, true );

				prov.CustomFlags["charOwned"] = true;
				prov.CustomFlags["charOwner"] = chars[0].ID;
				provsOwnedByChar = new List<Province>();
				provsOwnedByChar.Add( prov );

				chars[0].CustomFlags["provs"] = provsOwnedByChar;
				chars[0].CustomFlags["duchies"] = new List<Title>();
				chars[0].CustomFlags["kingdoms"] = new List<Title>();
				chars[0].CustomFlags["neighbours"] = new List<int>();

				chars[0].CustomFlags["realmDuchies"] = new List<Title>();

				chars[0].CustomFlags["VassalCount"] = 0;
				chars[0].CustomFlags["Liege"] = liege;
				chars[0].CustomFlags["Tier"] = TitleTier.Count;

				owners.Add( chars[0] );
				if( chars.Count == 2 )
					m_spouses.Add( chars[1] );
			}

			return owners;
		}

		private void GrowRealm( Character owner, ref bool finishedGrowing, List<Province> usableProvs )
		{
			List<Province> provsOwnedByChar = (List<Province>)owner.CustomFlags["provs"];
			int currCount = provsOwnedByChar.Count;
			List<int> neighbours = (List<int>)owner.CustomFlags["neighbours"];
			int neighbourID;

			for( int i = 0; i < currCount; i++ )
			{
				Province ownProv = provsOwnedByChar[i];
				if( ownProv.CustomFlags.ContainsKey( "charSurrounded" ) )
					continue;

				foreach( Province adj in ownProv.Adjacencies )
				{
					if( m_options.RuleSet.IgnoredTitles.Contains( adj.Title ) )
						continue;
					if( adj.CustomFlags.ContainsKey( "charOwned" ) )
					{
						neighbourID = (int)adj.CustomFlags["charOwner"];
						if( !neighbours.Contains( neighbourID ) )
							neighbours.Add( neighbourID );
						continue;
					}

					adj.CustomFlags["charOwned"] = true;
					adj.CustomFlags["charOwner"] = owner.ID;
					provsOwnedByChar.Add( adj );
					usableProvs.Remove( adj );
					finishedGrowing = false;
				}
				ownProv.CustomFlags["charSurrounded"] = true;
			}
		}

		private void SetRealmTier( Character owner, TitleTier maxTier )
		{
			List<Province> ownedProvs = (List<Province>)owner.CustomFlags["provs"];
			TitleTier tier = TitleTier.Count;

			if( ownedProvs.Count >= m_options.RuleSet.EmpireMinSize )
				tier = TitleTier.Emperor;
			else if( ownedProvs.Count >= m_options.RuleSet.KingdomMinSize )
				tier = TitleTier.King;
			else if( ownedProvs.Count >= m_options.RuleSet.DuchyMinSize )
				tier = TitleTier.Duke;

			if( tier > maxTier )
				tier = maxTier;

			owner.CustomFlags["Tier"] = tier;
		}




		private void FillEmptyProvinces()
		{
			// Fill unowned provinces.
			Log( " --Filling Empty Provinces" );
			SendMessage( "Setting History... Filling Empty Provinces" );
			List<Title> empty = new List<Title>();

			foreach( var prov in m_unownedProvs )
			{
				if( prov.Title != null && m_options.Data.Counties.ContainsKey( prov.Title ) )
					empty.Add( m_options.Data.Counties[prov.Title] );
				else
					m_log.Log( "Province Has Invalid Title: " + prov.Filename, Logger.LogType.Error );
			}

			foreach( var prov in m_unusableProvs )
			{
				if( prov.Title != null && m_options.Data.Counties.ContainsKey( prov.Title ) )
					empty.Add( m_options.Data.Counties[prov.Title] );
				else
					m_log.Log( "Province Has Invalid Title: " + prov.Filename, Logger.LogType.Error );
			}

			MakeCharactersForTitles( m_charWriter, m_availDynasties, empty, false, null, false, null, null, null );
		}

		private void MakeProvincesFeudal( List<Province> provs )
		{
			Log( " --Making Provinces Feudal" );
			foreach( Province p in provs )
			{
				if( !p.Settlements.Any() )
					continue;
				if( p.Settlements[0].Type != "city" && p.Settlements[0].Type != "temple" )
					continue;

				// Try and find a settlement with the same type as the target.
				Settlement swappee = p.Settlements.Find( s => s != p.Settlements[0] && s.Type == "castle" );

				if( swappee != null )
				{
					if( !swappee.CustomFlags.ContainsKey( "oldType" ) )
						swappee.CustomFlags["oldType"] = swappee.Type;
					swappee.Type = p.Settlements[0].Type;
				}

				p.Settlements[0].CustomFlags["oldType"] = p.Settlements[0].Type;
				p.Settlements[0].Type = "castle";
				p.CustomFlags["edited"] = true;
			}
		}

		private void ChangeProvinceType( List<Character> owners, string type )
		{
			foreach( Character owner in owners )
			{
				List<Province> provsOwnedByChar = (List<Province>)owner.CustomFlags["provs"];

				foreach( Province prov in provsOwnedByChar )
				{
					prov.CustomFlags["edited"] = true;

					// Try and find a settlement with the same type as the target.
					Settlement swappee = prov.Settlements.Find( s => s != prov.Settlements[0] && s.Type == type );

					if( swappee != null )
					{
						if( !swappee.CustomFlags.ContainsKey( "oldType" ) )
							swappee.CustomFlags["oldType"] = swappee.Type;
						swappee.Type = prov.Settlements[0].Type;
					}

					if( !prov.Settlements[0].CustomFlags.ContainsKey( "oldType" ) )
						prov.Settlements[0].CustomFlags["oldType"] = prov.Settlements[0].Type;
					prov.Settlements[0].Type = type;
				}
			}
		}


		private void WriteTitleOwners( List<Character> owners )
		{
			Log( " --Writing Title Owners" );
			SendMessage( "Setting History... Writing Title Owners" );

			List<Title> duchies, kingdoms;
			Title liegeTitle;
			FileInfo titleFile;
			Character liege;
			TitleTier tier;
			string titlePath = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			titlePath = Path.Combine( titlePath, "history/titles" );

			foreach( Character owner in owners )
			{
				Log( "   --Writing titles for " + owner.ID );

				liege = (Character)owner.CustomFlags["Liege"];
				tier = (TitleTier)owner.CustomFlags["Tier"];

				liegeTitle = null;
				if( liege != null )
					liegeTitle = GetLiegeTitle( liege );

				duchies = (List<Title>)owner.CustomFlags["duchies"];
				kingdoms = (List<Title>)owner.CustomFlags["kingdoms"];

				foreach( Title title in duchies )
				{
					titleFile = new FileInfo( Path.Combine( titlePath, title.TitleID + ".txt" ) );
					WriteTitleOwner( titleFile, owner );

					if( tier == TitleTier.Duke && liegeTitle != null )
						MakeDeJureLiege( titleFile, true, liegeTitle.TitleID, true, liegeTitle.TitleID );
					else if( tier >= TitleTier.King )
						MakeDeJureLiege( titleFile, true, kingdoms.First().TitleID, true, kingdoms.First().TitleID );
				}

				for( int i = 0; i < kingdoms.Count; i++ )
				{
					// Only write the first if character is only a king.
					if( tier == TitleTier.King && i > 0 )
						break;

					Title title = kingdoms[i];
					titleFile = new FileInfo( Path.Combine( titlePath, title.TitleID + ".txt" ) );
					WriteTitleOwner( titleFile, owner );

					if( tier == TitleTier.King && liegeTitle != null )
						MakeDeJureLiege( titleFile, true, liegeTitle.TitleID, true, liegeTitle.TitleID );
				}

				//Unowned, but within the realm.
				if( tier >= TitleTier.King )
				{
					duchies = (List<Title>)owner.CustomFlags["realmDuchies"];
					foreach( Title title in duchies )
					{
						titleFile = new FileInfo( Path.Combine( titlePath, title.TitleID + ".txt" ) );
						MakeDeJureLiege( titleFile, true, kingdoms.First().TitleID, true, kingdoms.First().TitleID );
					}
				}
			}
		}

		private void WriteCountyOwners( List<Character> owners )
		{
			SendMessage( "Setting History... Writing County Owners" );

			List<Province> provsOwnerByChar;
			FileInfo titleFile;
			string titlePath = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			titlePath = Path.Combine( titlePath, "history/titles" );

			Character liege;
			Title liegeTitle;

			foreach( Character owner in owners )
			{
				liege = (Character)owner.CustomFlags["Liege"];
				liegeTitle = null;
				if( liege != null )
					liegeTitle = GetLiegeTitle( liege );

				provsOwnerByChar = (List<Province>)owner.CustomFlags["provs"];
				foreach( Province p in provsOwnerByChar )
				{
					titleFile = new FileInfo( Path.Combine( titlePath, p.Title + ".txt" ) );
					WriteTitleOwner( titleFile, owner );

					if( liegeTitle != null )
					{
						MakeDeJureLiege( titleFile, true, liegeTitle.TitleID, false, null );
					}
				}
			}
		}

		private static Title GetLiegeTitle( Character liege )
		{
			Title liegeTitle;
			TitleTier liegeTier = (TitleTier)liege.CustomFlags["Tier"];
			switch( liegeTier )
			{
				case TitleTier.Duke:
					liegeTitle = ( (List<Title>)liege.CustomFlags["duchies"] ).First();
					break;
				case TitleTier.King:
					liegeTitle = ( (List<Title>)liege.CustomFlags["kingdoms"] ).First();
					break;
				case TitleTier.Emperor:
					liegeTitle = ( (List<Title>)liege.CustomFlags["empires"] ).First();
					break;
				default:
					throw new ArgumentOutOfRangeException( string.Format( "Liege {0} is a count.", liege.ID ) );
			}
			return liegeTitle;
		}
	}

	internal enum TitleTier
	{
		Count,
		Duke,
		King,
		Emperor
	}
}
