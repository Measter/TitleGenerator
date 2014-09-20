using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Parsers.Culture;
using Parsers.Religion;
using Pdoxcl2Sharp;

namespace TitleGenerator.HistoryRules
{
	public class RuleSet
	{
		public List<CharacterRule> CharRules = new List<CharacterRule>();
		public List<string> MaleCultures = new List<string>();
		public List<string> FemaleCultures = new List<string>();
		public List<string> MaleReligions = new List<string>();
		public List<string> FemaleReligions = new List<string>();
		public List<string> IgnoredTitles = new List<string>();
		public List<string> MuslimLawFollowers = new List<string>();
		public List<string> RequiredRules = new List<string>();

		public string Name
		{
			get;
			private set;
		}

		public List<string> Errors = new List<string>();

		public LawRules LawRules = new LawRules();

		public int LiegeCultureChance = -1;
		public int LiegeReligionChance = -1;
		public int FemaleRulerChance = -1;
		public int RulerSpouseChance = -1;
		public bool? RepsForceCustomDuchies = null;
		public int RepublicExpandChance = -1;
		public int RepublicExpandMax = -1;

		public int EmpireMinSize = -1;
		public int KingdomMinSize = -1;
		public int DuchyMinSize = -1;

		public int CharacterStartID = -1;
		public bool? ClearCharacters = null;

		public int CulGenDynastyPrefix = -1;
		public int CulGenBastardPrefix = -1;
		public int CulGenPatronymIsPrefix = -1;
		public int CulGenMalePatronym = -1;
		public int CulGenFemalePatronym = -1;
		public int CulGenAncestorName = -1;
		public int CulGenDisinheritFromBlinding = -1;
		public int CulGenDukesCalledKings = -1;
		public int CulGenFounderNamesDynasty = -1;
		public int CulGenDynastyTitleNames = -1;



		private static RuleSet m_ruleset;
		public static RuleSet Parse( string filename, string name )
		{
			m_ruleset = new RuleSet();
			m_ruleset.Name = name;

			if( !File.Exists( filename ) )
			{
				m_ruleset.Errors.Add( string.Format( "File not found: {0}", filename ) );
				return m_ruleset;
			}

			using( FileStream fs = new FileStream( filename, FileMode.Open ) )
			{
				try
				{
					ParadoxParser.Parse( fs, ParseRule );
					m_ruleset.LawRules.ParentRuleSet = m_ruleset;
				} catch( Exception ex )
				{
					m_ruleset.Errors.Add( ex.ToString() );
				}
			}

			return m_ruleset;
		}

		private static void ParseRule( ParadoxParser parser, string tag )
		{
			switch( tag )
			{
				case "ignored_titles":
					m_ruleset.IgnoredTitles.AddRange( parser.ReadStringList() );
					break;
				case "muslim_laws":
					m_ruleset.MuslimLawFollowers.AddRange( parser.ReadStringList() );
					break;
				case "male_culture":
					m_ruleset.MaleCultures.AddRange( parser.ReadStringList() );
					break;
				case "female_culture":
					m_ruleset.FemaleCultures.AddRange( parser.ReadStringList() );
					break;
				case "male_religion":
					m_ruleset.MaleReligions.AddRange( parser.ReadStringList() );
					break;
				case "female_religion":
					m_ruleset.FemaleReligions.AddRange( parser.ReadStringList() );
					break;
				case "required_rules":
					m_ruleset.RequiredRules.AddRange( parser.ReadStringList() );
					break;

				case "levy_tax_weight":
					List<int> ltw = (List<int>)parser.ReadIntList();

					if( ltw.Count >= 4 )
					{
						int i = 0;
						m_ruleset.LawRules.LevyTax.Min = ltw[0];
						m_ruleset.LawRules.LevyTax.Normal = ltw[1];
						m_ruleset.LawRules.LevyTax.Large = ltw[2];
						m_ruleset.LawRules.LevyTax.Max = ltw[3];
					}
					break;

				case "character":
					m_ruleset.CharRules.Add( ParseCharacter( parser ) );
					break;
				case "succession_laws":
					ParseLaws( parser, m_ruleset.LawRules.SuccessionLaws );
					break;
				case "gender_laws":
					ParseLaws( parser, m_ruleset.LawRules.GenderLaws );
					break;
				case "misc":
					parser.Parse( ParseMiscRules );
					break;
				case "cul_gen":
					parser.Parse( ParseCultureGenRules );
					break;
			}
		}

		private static void ParseCultureGenRules( ParadoxParser parser, string tag )
		{
			switch ( tag )
			{
				case "dynasty_prefix":
					m_ruleset.CulGenDynastyPrefix = parser.ReadInt32();
					break;
				case "bastard_prefix":
					m_ruleset.CulGenBastardPrefix = parser.ReadInt32();
					break;
				case "patronym_is_prefix":
					m_ruleset.CulGenPatronymIsPrefix = parser.ReadInt32();
					break;
				case "male_patronym":
					m_ruleset.CulGenMalePatronym = parser.ReadInt32();
					break;

				case "female_patronym":
					m_ruleset.CulGenFemalePatronym = parser.ReadInt32();
					break;
				case "ancestor_name":
					m_ruleset.CulGenAncestorName = parser.ReadInt32();
					break;
				case "disinherit_from_blinding":
					m_ruleset.CulGenDisinheritFromBlinding = parser.ReadInt32();
					break;
				case "dukes_called_kings":
					m_ruleset.CulGenDukesCalledKings = parser.ReadInt32();
					break;

				case "founder_names_dynasty":
					m_ruleset.CulGenFounderNamesDynasty = parser.ReadInt32();
					break;
				case "dynasty_title_names":
					m_ruleset.CulGenDynastyTitleNames = parser.ReadInt32();
					break;
			}
		}

		private static void ParseMiscRules( ParadoxParser parser, string tag )
		{
			switch ( tag )
			{
				case "liege_culture_chance":
					m_ruleset.LiegeCultureChance = parser.ReadInt32();
					break;
				case "liege_religion_chance":
					m_ruleset.LiegeReligionChance = parser.ReadInt32();
					break;
				case "female_ruler_chance":
					m_ruleset.FemaleRulerChance = parser.ReadInt32();
					break;
				case "ruler_spouse_chance":
					m_ruleset.RulerSpouseChance = parser.ReadInt32();
					break;

				case "reps_force_custom_duchies":
					m_ruleset.RepsForceCustomDuchies = parser.ReadBool();
					break;
				case "rep_expand_chance":
					m_ruleset.RepublicExpandChance = parser.ReadInt32();
					break;
				case "rep_expand_max":
					m_ruleset.RepublicExpandMax = parser.ReadInt32();
					break;
				case "empire_min_size":
					m_ruleset.EmpireMinSize = parser.ReadInt32();
					break;

				case "kingdom_min_size":
					m_ruleset.KingdomMinSize = parser.ReadInt32();
					break;
				case "duchy_min_size":
					m_ruleset.DuchyMinSize = parser.ReadInt32();
					break;
				case "clear_characters":
					m_ruleset.ClearCharacters = parser.ReadBool();
					break;
				case "character_start_id":
					m_ruleset.CharacterStartID = parser.ReadInt32();
					break;
			}
		}

		private static void ParseLaws( ParadoxParser parser, List<Law> lawRules )
		{
			Law l = new Law( "" );

			Action<ParadoxParser, string> lawGroup = ( p, s ) =>
													   {
														   l = new Law( s );

														   Action<ParadoxParser, string> lawOptions = ( p2, s2 ) =>
														   {
															   switch( s2 )
															   {
																   case "banned_religion_group":
																   case "banned_religion":
																	   l.BannedReligions.Add( p2.ReadString() );
																	   break;
																   case "allowed_religion_group":
																   case "allowed_religion":
																	   l.AllowedReligions.Add( p2.ReadString() );
																	   break;

																   case "banned_culture_group":
																   case "banned_culture":
																	   l.BannedCultures.Add( p2.ReadString() );
																	   break;
																   case "allowed_culture_group":
																   case "allowed_culture":
																	   l.AllowedCultures.Add( p2.ReadString() );
																	   break;
															   }
														   };

														   p.Parse( lawOptions );

														   lawRules.Add( l );
													   };

			parser.Parse( lawGroup );
		}

		private static CharacterRule ParseCharacter( ParadoxParser parser )
		{
			CharacterRule cr = new CharacterRule();

			Action<ParadoxParser, string> getOptions = ( p, s ) =>
													   {
														   switch( s )
														   {
															   case "gender":
																   cr.IsFemale = p.ReadString() == "female";
																   break;
															   case "religion":
																   cr.Religion = p.ReadString();
																   break;
															   case "culture":
																   cr.Culture = p.ReadString();
																   break;
															   case "id":
																   cr.ID = p.ReadInt32();
																   break;
															   case "dynasty":
																   cr.Dynasty = p.ReadInt32();
																   break;
															   case "write_character":
																   cr.WriteCharacter = p.ReadBool();
																   break;
															   case "title":
																   cr.Titles.Add( p.ReadString() );
																   break;
														   }
													   };

			parser.Parse( getOptions );

			return cr;
		}

		public Gender GetGender( Culture cul, Religion rel )
		{
			if( MaleCultures.Contains( cul.Name ) || MaleCultures.Contains( cul.Group.Name ) )
				return Gender.Male;

			if( MaleReligions.Contains( rel.Name ) || MaleReligions.Contains( rel.Group.Name ) )
				return Gender.Male;

			if( FemaleCultures.Contains( cul.Name ) || FemaleCultures.Contains( cul.Group.Name ) )
				return Gender.Female;

			if( FemaleReligions.Contains( rel.Name ) || FemaleReligions.Contains( rel.Group.Name ) )
				return Gender.Female;

			return Gender.Random;
		}

		public enum Gender
		{
			Random,
			Male,
			Female
		}
	}

	public class LawRules
	{
		public List<Law> SuccessionLaws = new List<Law>();
		public List<Law> GenderLaws = new List<Law>();
		public LevyTaxValue LevyTax = new LevyTaxValue();

		public RuleSet ParentRuleSet;

		public LawSet GetLawSet( Culture cul, Religion rel, Random rand )
		{
			LawSet ls = new LawSet();

			List<Law> posLaws = FilterLaws( cul, rel, SuccessionLaws );
			ls.Succession = posLaws.RandomItem( rand ).Name;

			posLaws = FilterLaws( cul, rel, GenderLaws );
			ls.Gender = posLaws.RandomItem( rand ).Name;

			ls.CrownAuthority = "centralization_" + rand.Normal( 0, 4 ).ToString();

			ls.CityLevy = "city_contract_" + WeightedLawNum( rand ).ToString();
			ls.CityTax = "city_tax_" + WeightedLawNum( rand ).ToString();

			if( IsMuslimLaw( cul, rel ) )
			{
				ls.isMuslim = true;

				ls.IqtaLevy = "iqta_contract_" + WeightedLawNum( rand ).ToString();
				ls.IqtaTax = "iqta_tax_" + WeightedLawNum( rand ).ToString();
			} else
			{
				ls.isMuslim = false;

				ls.ChurchLevy = "temple_contract_" + WeightedLawNum( rand ).ToString();
				ls.ChurchTax = "temple_tax_" + WeightedLawNum( rand ).ToString();

				ls.FeudalLevy = "feudal_contract_" + WeightedLawNum( rand ).ToString();
				ls.FeudalTax = "feudal_tax_" + WeightedLawNum( rand ).ToString();
			}

			return ls;
		}

		private bool IsMuslimLaw( Culture cul, Religion rel )
		{
			bool retVal = ParentRuleSet.MuslimLawFollowers.Contains( cul.Name );

			if( ParentRuleSet.MuslimLawFollowers.Contains( cul.Group.Name ) )
				retVal = true;

			if( ParentRuleSet.MuslimLawFollowers.Contains( rel.Name ) )
				retVal = true;

			if( ParentRuleSet.MuslimLawFollowers.Contains( rel.Group.Name ) )
				retVal = true;

			return retVal;
		}

		private int WeightedLawNum( Random rand )
		{
			int i = 0;

			int randNum = rand.Next( LevyTax.Min + LevyTax.Normal + LevyTax.Large + LevyTax.Max );

			if( randNum < LevyTax.Min )
				i = 0;
			else if( randNum < ( LevyTax.Min + LevyTax.Normal ) )
				i = 1;
			else if( randNum < ( LevyTax.Min + LevyTax.Normal + LevyTax.Large ) )
				i = 2;
			else if( randNum < ( LevyTax.Min + LevyTax.Normal + LevyTax.Large + LevyTax.Max ) )
				i = 3;

			return i;
		}

		private List<Law> FilterLaws( Culture cul, Religion rel, IEnumerable<Law> lawList )
		{
			List<Law> posLaws = new List<Law>();
			foreach( Law sc in lawList )
			{
				if( sc.BannedCultures.Contains( cul.Name ) || sc.BannedCultures.Contains( cul.Group.Name ) )
					continue;
				if( sc.BannedReligions.Contains( rel.Name ) || sc.BannedReligions.Contains( rel.Group.Name ) )
					continue;

				if( sc.AllowedCultures.Count > 0 )
					if( !sc.AllowedCultures.Contains( cul.Name ) && !sc.AllowedCultures.Contains( cul.Group.Name ) )
						continue;
				if( sc.AllowedReligions.Count > 0 )
					if( !sc.AllowedReligions.Contains( rel.Name ) && !sc.AllowedReligions.Contains( rel.Group.Name ) )
						continue;

				posLaws.Add( sc );
			}

			return posLaws;
		}
	}

	public struct LawSet
	{
		public string Succession;
		public string Gender;

		public string CrownAuthority;

		public string FeudalLevy;
		public string FeudalTax;
		public string CityLevy;
		public string CityTax;
		public string ChurchLevy;
		public string ChurchTax;
		public string IqtaLevy;
		public string IqtaTax;

		public bool isMuslim;
	}

	public struct LevyTaxValue
	{
		public int Min;
		public int Normal;
		public int Large;
		public int Max;
	}

	public struct Law
	{
		public string Name;
		public List<string> BannedReligions;
		public List<string> AllowedReligions;
		public List<string> BannedCultures;
		public List<string> AllowedCultures;

		public Law( string name )
		{
			BannedCultures = new List<string>();
			BannedReligions = new List<string>();
			AllowedCultures = new List<string>();
			AllowedReligions = new List<string>();
			Name = name;
		}

		public override string ToString()
		{
			return string.Format( "Name: {0}", Name );
		}
	}

	public class CharacterRule
	{
		public bool IsFemale;
		public string Religion = null;
		public string Culture = null;
		public int ID = -1;
		public int Dynasty = -1;
		public List<string> Titles = new List<string>();
		public bool WriteCharacter = true;
	}
}
