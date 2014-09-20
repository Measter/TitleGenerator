using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Measter;
using Parsers.Culture;
using Parsers.Province;
using Parsers.Title;
using TitleGenerator.HistoryRules;
using TitleGenerator.Includes;

namespace TitleGenerator.Tasks.History
{
	class CultureGenerationTask : SharedTask
	{
		private List<Province> m_possibleProvinces;
		private List<Province> m_unusableProvs;


		public CultureGenerationTask( Options options, Logger log )
			: base( options, log )
		{
		}

		protected override bool Execute()
		{
			m_possibleProvinces = FilterIgnoredProvinces();
			m_unusableProvs = new List<Province>();

			FilterSingleProvinces( m_possibleProvinces, m_unusableProvs );

			List<CultureGroup> cultureGroups = GenerateCultureGroups();
			GenerateCultures( cultureGroups );

			UpdateRuleSet( cultureGroups );

			NullifyOldCultures( m_options.Data.Counties );
			NullifyOldCultures( m_options.Data.Duchies );
			NullifyOldCultures( m_options.Data.Kingdoms );
			NullifyOldCultures( m_options.Data.Empires );

			WriteCultures( cultureGroups );
			m_options.Data.SetCustomCultures( cultureGroups );

			return true;
		}

		private void NullifyOldCultures( ReadOnlyDictionary<string, Title> counties )
		{
			foreach( var pair in counties )
			{
				if( pair.Value.IsTitular )
					continue;

				pair.Value.CustomFlags["old_cul"] = pair.Value.Culture;
				pair.Value.Culture = null;
			}
		}

		private void UpdateRuleSet( List<CultureGroup> cultureGroups )
		{
			Culture oldCul;
			CultureGroup oldGroup;

			foreach( CultureGroup group in cultureGroups )
			{
				oldGroup = (CultureGroup)group.CustomFlags["baseCulGroup"];

				UpdateSet( oldGroup.Name, group.Name );

				foreach( var cul in group.Cultures )
				{
					oldCul = (Culture)cul.Value.CustomFlags["baseCul"];
					UpdateSet( oldCul.Name, cul.Value.Name );
				}
			}
		}

		private void UpdateSet( string oldGroup, string group )
		{
			// Update rule set.
			if( m_options.RuleSet.MaleCultures.Contains( oldGroup ) )
				m_options.RuleSet.MaleCultures.Add( group );
			if( m_options.RuleSet.FemaleCultures.Contains( oldGroup ) )
				m_options.RuleSet.FemaleCultures.Add( group );
			if( m_options.RuleSet.MuslimLawFollowers.Contains( oldGroup ) )
				m_options.RuleSet.MuslimLawFollowers.Add( group );

			// Laws
			foreach( Law law in m_options.RuleSet.LawRules.GenderLaws )
			{
				if( law.AllowedCultures.Contains( oldGroup ) )
					law.AllowedCultures.Add( group );
				if( law.BannedCultures.Contains( oldGroup ) )
					law.BannedCultures.Add( group );
			}
			foreach( Law law in m_options.RuleSet.LawRules.SuccessionLaws )
			{
				if( law.AllowedCultures.Contains( oldGroup ) )
					law.AllowedCultures.Add( group );
				if( law.BannedCultures.Contains( oldGroup ) )
					law.BannedCultures.Add( group );
			}
		}



		private List<CultureGroup> GenerateCultureGroups()
		{
			List<CultureGroup> groups = new List<CultureGroup>();

			int groupCount = m_options.Random.Next( m_options.HistoryCulGroupMin, m_options.HistoryCulGroupMax );
			MakeCultureGroups( groups, groupCount );

			Log( "Growing Culture Groups" );
			bool finishedGrowing;
			do
			{
				finishedGrowing = true;
				foreach( CultureGroup cg in groups )
				{
					GrowCultureGroup( cg, ref finishedGrowing );
				}
			} while( !finishedGrowing );

			return groups;
		}

		private void GrowCultureGroup( CultureGroup cg, ref bool finishedGrowing )
		{
			List<Province> provsOwnedByGroup;
			provsOwnedByGroup = (List<Province>)cg.CustomFlags["provs"];

			int currCount = provsOwnedByGroup.Count;

			for( int i = 0; i < currCount; i++ )
			{
				Province ownProv = provsOwnedByGroup[i];
				if( ownProv.CustomFlags.ContainsKey( "culGroupSurrounded" ) )
					continue;

				foreach( Province adj in ownProv.Adjacencies )
				{
					if( m_options.RuleSet.IgnoredTitles.Contains( adj.Title ) )
						continue;

					if( adj.CustomFlags.ContainsKey( "owningCultureGroup" ) )
						continue;

					adj.CustomFlags["owningCultureGroup"] = cg;
					provsOwnedByGroup.Add( adj );
					m_possibleProvinces.Remove( adj );
					finishedGrowing = false;
				}
				ownProv.CustomFlags["culGroupSurrounded"] = true;
			}
		}

		private void MakeCultureGroups( List<CultureGroup> groups, int groupCount )
		{
			SendMessage( "Generating Culture Groups" );
			Log( "Generating Culture Groups" );

			Province prov;
			List<Province> provsOwnedByCultureGroup;
			List<Province> provs = new List<Province>( m_possibleProvinces );
			CultureGroup baseCulGroup, thisCulGroup;
			Title county;

			int genOffset = m_options.Random.Next( 256 );

			for( int i = 0; i < groupCount; i++ )
			{
				if( provs.Count == 0 )
					break;

				prov = provs.RandomItem( m_options.Random );
				provs.Remove( prov );
				m_possibleProvinces.Remove( prov );

				#region Filtering
				if( prov.Title == null || !m_options.Data.Counties.ContainsKey( prov.Title ) )
				{
					i--;
					m_log.Log( String.Format( "CG Gen: Province {0}-{1}: Title ID is null or doesn't exist.", prov.ID, prov.Title ), Logger.LogType.Error );
					continue;
				}

				if( !m_options.Data.Localisations.ContainsKey( prov.Title + "_adj" ) )
				{
					i--;
					m_log.Log( string.Format( "CG Gen: Province {0}-{1}: No adjective localisation.", prov.ID, prov.Title ), Logger.LogType.Error );
					continue;
				}

				if( prov.Culture == null || !m_options.Data.ContainsCulture( prov.Culture ) )
				{
					i--;
					m_log.Log( String.Format( "CG Gen: Province {0}-{1}: Culture is null or doesn't exist.", prov.ID, prov.Title ), Logger.LogType.Error );
					continue;
				}
				#endregion

				baseCulGroup = m_options.Data.GetCulture( prov.Culture ).Group;
				county = m_options.Data.Counties[prov.Title];
				provsOwnedByCultureGroup = new List<Province>();
				provsOwnedByCultureGroup.Add( prov );

				Log( "Province: " + prov.Title );

				thisCulGroup = CopyCultureGroup( county, baseCulGroup );
				thisCulGroup.CustomFlags["baseCulGroup"] = baseCulGroup;
				thisCulGroup.CustomFlags["provs"] = provsOwnedByCultureGroup;
				thisCulGroup.CustomFlags["name_group"] = i + genOffset;

				prov.CustomFlags["owningCultureGroup"] = thisCulGroup;

				groups.Add( thisCulGroup );
			}
		}




		private void GenerateCultures( List<CultureGroup> cultureGroups )
		{
			foreach( CultureGroup group in cultureGroups )
			{
				int culCount = m_options.Random.Next( m_options.HistoryCulMin, m_options.HistoryCulMax );
				CultureGroup baseGroup = (CultureGroup)group.CustomFlags["baseCulGroup"];

				MakeCultures( group, culCount, baseGroup );

				bool finishedGrowing;
				do
				{
					finishedGrowing = true;
					foreach( var pair in group.Cultures )
					{
						GrowCultures( pair.Value, ref finishedGrowing );
					}
				} while( !finishedGrowing );
			}
		}

		private void GrowCultures( Culture cul, ref bool finishedGrowing )
		{
			List<Province> provsOwnedByCul;
			provsOwnedByCul = (List<Province>)cul.CustomFlags["provs"];

			int currCount = provsOwnedByCul.Count;

			for( int i = 0; i < currCount; i++ )
			{
				Province ownProv = provsOwnedByCul[i];
				if( ownProv.CustomFlags.ContainsKey( "culSurrounded" ) )
					continue;

				foreach( Province adj in ownProv.Adjacencies )
				{
					if( m_options.RuleSet.IgnoredTitles.Contains( adj.Title ) )
						continue;

					if( ( (CultureGroup)adj.CustomFlags["owningCultureGroup"] ).Name != cul.Group.Name )
						continue;

					if( adj.CustomFlags.ContainsKey( "owningCulture" ) )
						continue;

					if ( !adj.CustomFlags.ContainsKey( "old_cul" ) )
						adj.CustomFlags["old_cul"] = adj.Culture;
					adj.CustomFlags["owningCulture"] = cul;
					adj.Culture = cul.Name;
					provsOwnedByCul.Add( adj );
					finishedGrowing = false;
				}
				ownProv.CustomFlags["culSurrounded"] = true;
			}
		}

		private void MakeCultures( CultureGroup group, int culCount, CultureGroup baseGroup )
		{
			Log( "Generating Cultures" );
			SendMessage( "Generating Cultures" );

			Province prov;
			List<Province> provsOwnedByCulture;
			List<Province> provs = new List<Province>( (IEnumerable<Province>)group.CustomFlags["provs"] );
			Culture baseCul, thisCul;
			Title county;
			List<Culture> baseCuls = baseGroup.Cultures.Values.ToList();

			int nameGenGroup = (int)group.CustomFlags["name_group"];
			int genOffset = m_options.Random.Next( 256 );

			for( int i = 0; i < culCount; i++ )
			{
				if( provs.Count == 0 )
					break;

				baseCul = baseCuls.RandomItem( m_options.Random );

				do
				{
					prov = provs.RandomItem( m_options.Random );
					provs.Remove( prov );
				} while( !m_options.Data.Localisations.ContainsKey( prov.Title + "_adj" ) );

				county = m_options.Data.Counties[prov.Title];
				provsOwnedByCulture = new List<Province>();
				provsOwnedByCulture.Add( prov );

				Log( "Province: " + prov.Title );

				string locString = m_options.Data.Localisations[prov.Title + "_adj"];
				SendMessage( "Generating Culture " + locString.Split( ';' )[1] );

				thisCul = CopyCulture( county, baseCul );
				thisCul.CustomFlags["baseCul"] = baseCul;
				thisCul.CustomFlags["provs"] = provsOwnedByCulture;

				GenerateNewCultureData( thisCul, nameGenGroup, genOffset + i );

				if ( !prov.CustomFlags.ContainsKey( "old_cul" ) )
					prov.CustomFlags["old_cul"] = prov.Culture;
				prov.CustomFlags["owningCulture"] = thisCul;
				prov.Culture = thisCul.Name;

				thisCul.Group = group;

				group.Cultures.Add( thisCul.Name, thisCul );
			}
		}

		private void GenerateNewCultureData( Culture thisCul, int genGroup, int genID )
		{
			thisCul.Colour = GetCultureColour();

			thisCul.MaleNames = m_options.Data.GenerateWords( genGroup, genID, 100, 5, 9 );
			thisCul.FemaleNames = m_options.Data.GenerateWords( genGroup, genID, 100, 5, 9 );

			thisCul.FromDynastyPrefix = m_options.Data.GenerateWord( genGroup, genID, 2, 4 );
			if( m_options.Random.Next( 100 ) < m_options.RuleSet.CulGenDynastyPrefix )
				thisCul.FromDynastyPrefix += " ";

			// Bastard prefix
			if( m_options.Random.Next( 100 ) < m_options.RuleSet.CulGenBastardPrefix )
				thisCul.BastardDynastyPrefix = m_options.Data.GenerateWord( genGroup, genID, 2, 5 );

			thisCul.IsPrefix = m_options.Random.Next( 100 ) < m_options.RuleSet.CulGenPatronymIsPrefix;
			// Has male patronym
			if( m_options.Random.Next( 100 ) < m_options.RuleSet.CulGenMalePatronym )
			{
				thisCul.MalePatronym = m_options.Data.GenerateWord( genGroup, genID, 3, 5 );
				if( m_options.Random.Next( 3 ) % 3 == 0 )
					thisCul.MalePatronym = thisCul.IsPrefix ? thisCul.MalePatronym + " " : " " + thisCul.MalePatronym;
			}

			if( m_options.Random.Next( 100 ) < m_options.RuleSet.CulGenFemalePatronym )
			{
				thisCul.FemalePatronym = m_options.Data.GenerateWord( genGroup, genID, 3, 5 );
				if( m_options.Random.Next( 100 ) % 3 == 0 )
					thisCul.FemalePatronym = thisCul.IsPrefix ? thisCul.FemalePatronym + " " : " " + thisCul.FemalePatronym;
			}

			// Named-after-ancestor chance
			if( m_options.Random.Next( 100 ) < m_options.RuleSet.CulGenAncestorName )
			{
				// Male chances
				int totalChance = m_options.Random.Next( 100 );
				int nextVal = m_options.Random.Next( totalChance );
				thisCul.PaternalGrandfatherChance = nextVal;

				totalChance -= nextVal;
				totalChance = totalChance < 0 ? 0 : totalChance;
				nextVal = m_options.Random.Next( totalChance );
				thisCul.MaternalGrandfatherChance = nextVal;

				totalChance -= nextVal;
				totalChance = totalChance < 0 ? 0 : totalChance;
				thisCul.FatherChance = totalChance;

				// Female chances
				totalChance = m_options.Random.Next( 100 );
				nextVal = m_options.Random.Next( totalChance );
				thisCul.PaternalGrandmotherChance = nextVal;

				totalChance -= nextVal;
				totalChance = totalChance < 0 ? 0 : totalChance;
				nextVal = m_options.Random.Next( totalChance );
				thisCul.MaternalGrandmotherChance = nextVal;

				totalChance -= nextVal;
				totalChance = totalChance < 0 ? 0 : totalChance;
				thisCul.MotherChance = totalChance;
			}


			thisCul.DisinheritFromBlinding = m_options.Random.Next( 100 ) < m_options.RuleSet.CulGenDisinheritFromBlinding;
			thisCul.DukesCalledKings = m_options.Random.Next( 100 ) < m_options.RuleSet.CulGenDukesCalledKings;
			thisCul.FounderNamedDynasties = m_options.Random.Next( 100 ) < m_options.RuleSet.CulGenFounderNamesDynasty;
			thisCul.DynastyTitleNames = m_options.Random.Next( 100 ) < m_options.RuleSet.CulGenDynastyTitleNames;
		}


		private Culture CopyCulture( Title title, Culture oldCul )
		{
			Culture cul = new Culture();

			cul.CustomFlags["from_title"] = title;
			cul.Name = title.TitleID.Substring( 2 ) + "_cul";

			cul.GraphicalCulture = oldCul.GraphicalCulture;
			cul.SecondGraphicalCulture = oldCul.SecondGraphicalCulture;
			cul.GraphicalUnitCulture = oldCul.GraphicalUnitCulture;
			cul.IsHorde = oldCul.IsHorde;
			cul.GrammarTransform = oldCul.GrammarTransform;
			cul.Modifier = oldCul.Modifier;
			cul.CustomFlags["orig"] = oldCul;

			return cul;
		}

		private Color GetCultureColour()
		{
			float hue, sat, val;
			hue = m_options.Random.Next( 101 ) / 100f;
			sat = m_options.Random.Next( 30, 61 ) / 100f;
			val = m_options.Random.Next( 60, 81 ) / 100f;

			return HSVtoRGB( hue, sat, val );
		}




		private CultureGroup CopyCultureGroup( Title title, CultureGroup oldGroup )
		{
			CultureGroup gc = new CultureGroup();

			gc.CustomFlags["from_title"] = title;
			gc.Name = title.TitleID.Substring( 2 ) + "_grp";

			gc.GraphicalCulture = oldGroup.GraphicalCulture;
			gc.MiscOptions = oldGroup.MiscOptions;
			gc.Cultures = new Dictionary<string, Culture>();

			return gc;
		}



		private void WriteCultures( List<CultureGroup> cultureGroups )
		{
			SendMessage( "Writing Cultures" );
			Log( "Writing Cultures" );

			string sFile = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			sFile = Path.Combine( sFile, "common/cultures/kaCultures.txt" ).Replace( '\\', '/' );
			FileInfo cultureFile = new FileInfo( sFile );

			sFile = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			sFile = Path.Combine( sFile, "localisation/kaCultureNames.csv" ).Replace( '\\', '/' );
			FileInfo locFile = new FileInfo( sFile );

			if( !cultureFile.Directory.Exists )
				cultureFile.Directory.Create();
			if( !locFile.Directory.Exists )
				locFile.Directory.Create();

			StreamWriter cultures = new StreamWriter( cultureFile.Open( FileMode.Create, FileAccess.Write ),
													  Encoding.GetEncoding( 1252 ) );
			StreamWriter locs = new StreamWriter( locFile.Open( FileMode.Create, FileAccess.Write ),
												  Encoding.GetEncoding( 1252 ) );

			foreach( CultureGroup group in cultureGroups )
				WriteCultureGroup( group, cultures, locs );

			locs.Dispose();
			cultures.Dispose();
		}

		private void WriteCultureGroup( CultureGroup group, StreamWriter cultures, StreamWriter locs )
		{
			Title tit = (Title)group.CustomFlags["from_title"];

			if( m_options.Data.Localisations.ContainsKey( tit.TitleID + "_adj" ) )
			{
				string loc = m_options.Data.Localisations[tit.TitleID + "_adj"].Substring( 2 ).Replace( "_adj", "_grp" );
				locs.WriteLine( loc );
			}

			cultures.WriteLine( "{0} = {{", group.Name );
			cultures.WriteLine( "\tgraphical_culture = {0}", group.GraphicalCulture );

			foreach( var cul in group.Cultures )
				WriteCulture( cul.Value, cultures, locs );

			cultures.WriteLine( "}" );
		}

		private void WriteCulture( Culture cul, StreamWriter cultures, StreamWriter locs )
		{
			Title tit = (Title)cul.CustomFlags["from_title"];

			if( m_options.Data.Localisations.ContainsKey( tit.TitleID + "_adj" ) )
			{
				string loc = m_options.Data.Localisations[tit.TitleID + "_adj"].Substring( 2 ).Replace( "_adj", "_cul" );
				locs.WriteLine( loc );
			}

			cultures.WriteLine( "\t{0} = {{", cul.Name );

			cultures.WriteLine( "\t\tgraphical_culture = {0}", cul.GraphicalCulture );
			if( !String.IsNullOrEmpty( cul.SecondGraphicalCulture ) )
				cultures.WriteLine( "\t\tsecond_graphical_culture = {0}", cul.SecondGraphicalCulture );
			if( !String.IsNullOrEmpty( cul.GraphicalUnitCulture ) )
				cultures.WriteLine( "\t\tgraphical_unit_culture = {0}", cul.GraphicalUnitCulture );

			if( !String.IsNullOrEmpty( cul.GrammarTransform ) )
				cultures.WriteLine( "\t\tgrammar_transform = {0}", cul.GrammarTransform );
			if( !String.IsNullOrEmpty( cul.FromDynastyPrefix ) )
				cultures.WriteLine( "\t\tfrom_dynasty_prefix = \"{0}\"", cul.FromDynastyPrefix );
			if( !String.IsNullOrEmpty( cul.MalePatronym ) )
				cultures.WriteLine( "\t\tmale_patronym = \"{0}\"", cul.MalePatronym );
			if( !String.IsNullOrEmpty( cul.FemalePatronym ) )
				cultures.WriteLine( "\t\tfemale_patronym = \"{0}\"", cul.FemalePatronym );

			if( !String.IsNullOrEmpty( cul.BastardDynastyPrefix ) )
				cultures.WriteLine( "\t\tbastard_dynasty_prefix = \"{0}\"", cul.BastardDynastyPrefix );

			cultures.WriteLine( "\t\tprefix = {0}", cul.IsPrefix ? "yes" : "no" );
			cultures.WriteLine( "\t\thorde = {0}", cul.IsHorde ? "yes" : "no" );
			cultures.WriteLine( "\t\tdynasty_title_names = {0}", cul.DynastyTitleNames ? "yes" : "no" );
			cultures.WriteLine( "\t\tfounder_named_dynasties = {0}", cul.FounderNamedDynasties ? "yes" : "no" );
			cultures.WriteLine( "\t\tdukes_called_kings = {0}", cul.DukesCalledKings ? "yes" : "no" );
			cultures.WriteLine( "\t\tbaron_titles_hidden = {0}", cul.BaronTitlesHidden ? "yes" : "no" );
			cultures.WriteLine( "\t\tcount_titles_hidden = {0}", cul.CountTitlesHidden ? "yes" : "no" );
			cultures.WriteLine( "\t\tdisinherit_from_blinding = {0}", cul.DisinheritFromBlinding ? "yes" : "no" );
			cultures.WriteLine( "\t\tused_for_random = {0}", cul.UsedForRandom ? "yes" : "no" );

			cultures.WriteLine( "\t\tpat_grf_name_chance = {0}", cul.PaternalGrandfatherChance );
			cultures.WriteLine( "\t\tmat_grf_name_chance = {0}", cul.MaternalGrandfatherChance );
			cultures.WriteLine( "\t\tfather_name_chance = {0}", cul.FatherChance );

			cultures.WriteLine( "\t\tpat_grm_name_chance = {0}", cul.PaternalGrandmotherChance );
			cultures.WriteLine( "\t\tmat_grm_name_chance = {0}", cul.MaternalGrandmotherChance );
			cultures.WriteLine( "\t\tmother_name_chance = {0}", cul.MotherChance );

			cultures.WriteLine( "\t\tcolor = {{ {0} {1} {2} }}", cul.Colour.R, cul.Colour.G, cul.Colour.B );

			cultures.WriteLine( "\t\tmodifier = {0}", cul.Modifier );

			cultures.WriteLine( "\t\tmale_names = {" );
			foreach( string name in cul.MaleNames )
			{
				if( name.Contains( " " ) )
					cultures.Write( "\"{0}\" ", name );
				else
					cultures.Write( "{0} ", name );
			}
			cultures.WriteLine( "\n\t\t}" );
			cultures.WriteLine( "\t\tfemale_names = {" );
			foreach( string name in cul.FemaleNames )
			{
				if( name.Contains( " " ) )
					cultures.Write( "\"{0}\" ", name );
				else
					cultures.Write( "{0} ", name );
			}
			cultures.WriteLine( "\n\t\t}" );

			cultures.WriteLine( "\t}" );
		}
	}
}
