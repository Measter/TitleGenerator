using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Parsers.Mod;
using TitleGenerator.HistoryRules;

namespace TitleGenerator
{
	public static class ModWriter
	{
		public static void CreateModFile( string ckDir, Mod mod )
		{
			FileInfo mpath = new FileInfo( ckDir + @"/mod/" + mod.ModFile );
			StreamWriter mw = new StreamWriter( mpath.Open( FileMode.Create, FileAccess.Write ), Encoding.GetEncoding( 1252 ) );

			mw.WriteLine( "name = \"" + mod.Name + "\"" );

			mw.WriteLine( "path = \"" + mod.Path + "\"" );

			mw.WriteLine();

			foreach( string e in mod.Extends )
				mw.WriteLine( "extend = \"" + e + "\"" );

			foreach( string e in mod.Replaces )
				mw.WriteLine( "replace_path = \"" + e + "\"" );

			if( mod.Dependencies.Count >= 0 )
			{
				mw.Write( "dependencies = {" );
				foreach( string s in mod.Dependencies )
					mw.Write( "\"" + s + "\" " );
				mw.WriteLine( "}" );
			}

			mw.Dispose();
		}
	}

	public class Options
	{
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine( " --Culture Limit: " + CultureLimit );
			sb.AppendLine( " --Religion Limit: " + ReligionLimit );
			sb.AppendLine( " --County Limit: " + CountyLimit );
			sb.AppendLine( " --Duchy Limit: " + DuchyLimit );
			sb.AppendLine( " --Kingdom Limit: " + KingdomLimit );
			sb.AppendLine( " --CreateDuchies: " + CreateDuchies );
			sb.AppendLine( " --Create Kingdoms: " + CreateKingdoms );
			sb.AppendLine( " --Create Empires: " + CreateEmpires );
			sb.AppendLine( " --Mod Name: " + Mod );
			sb.AppendLine( " --CK2 Directory: " + Data.InstallDir );
			sb.AppendLine( " --My Docs: " + Data.MyDocsDir );
			sb.AppendLine( " --Use Mod: " + UseMod );

			if( UseMod )
				foreach( Mod mod in SelectedMods )
					sb.AppendLine( "   --" + mod );

			sb.AppendLine( " --Replace De Jure: " + ReplaceDeJure );
			sb.AppendLine( " --Kingdom Short Names: " + KingdomShortNames );
			sb.AppendLine( " --Empire Short Names: " + EmpireShortNames );
			sb.AppendLine( " --Seed: " + Seed );

			sb.Append( " --History clear: " );
			switch( History )
			{
				case HistoryOption.None:
					sb.AppendLine( "None" );
					break;
				case HistoryOption.County:
					sb.AppendLine( "County" );
					break;
				case HistoryOption.Duchy:
					sb.AppendLine( "Duchy" );
					break;
				case HistoryOption.Kingdom:
					sb.AppendLine( "Kingdom" );
					break;
				case HistoryOption.Empire:
					sb.AppendLine( "Empire" );
					break;
			}

			sb.Append( " --History Create: " );
			switch( CreateHistoryType )
			{
				case CreateHistoryOption.Counts:
					sb.AppendLine( "Counts" );
					break;
				case CreateHistoryOption.Dukes:
					sb.AppendLine( "Dukes" );
					break;
				case CreateHistoryOption.Kings:
					sb.AppendLine( "Kings" );
					break;
				case CreateHistoryOption.Empires:
					sb.AppendLine( "Emperors" );
					break;
				case CreateHistoryOption.Random:
					sb.AppendLine( "Full Random " );
					sb.AppendLine( "    --Max Characters: " + HistoryMaxChar );
					sb.AppendLine( "    --Min Characters: " + HistoryMinChar );
					sb.AppendLine( "    --Min Republics: " + HistoryMinReps );
					sb.AppendLine( "    --Max Republics: " + HistoryMaxReps );
					sb.AppendLine( "    --Min Theocracies: " + HistoryMinTheoc );
					sb.AppendLine( "    --Max Theocracies: " + HistoryMaxTheoc );
					break;
				case CreateHistoryOption.None:
					sb.AppendLine( "None" );
					break;
			}

			sb.AppendLine( " --Start Date: " + StartDate.ToString() );

			sb.AppendLine( " --Rule set:" );

			sb.AppendLine( " --Ignored Titles:" );
			foreach ( string s in RuleSet.IgnoredTitles )
				sb.AppendLine( "    --" + s );

			sb.AppendLine( " --Succession Laws:" );
			foreach( Law l in RuleSet.LawRules.SuccessionLaws )
				sb.AppendLine( "    --" + l.Name );

			sb.AppendLine( " --Levy/Tax Weight: " +
						   RuleSet.LawRules.LevyTax.Min + " " +
			               RuleSet.LawRules.LevyTax.Normal + " " +
			               RuleSet.LawRules.LevyTax.Large + " " +
			               RuleSet.LawRules.LevyTax.Max );

			sb.AppendLine( " --Muslim Laws:" );
			foreach ( string s in RuleSet.MuslimLawFollowers )
				sb.AppendLine( "    --" + s );

			sb.AppendLine( " --Gender Laws:" );
			foreach( Law l in RuleSet.LawRules.GenderLaws )
				sb.AppendLine( "    --" + l.Name );

			sb.AppendLine( "    --Male Cultures:" );
			foreach( string c in RuleSet.MaleCultures )
				sb.AppendLine( "        " + c );
			sb.AppendLine( "    --Male Religions:" );
			foreach( string c in RuleSet.MaleReligions )
				sb.AppendLine( "        " + c );
			sb.AppendLine( "    --Female Cultures:" );
			foreach( string c in RuleSet.FemaleCultures )
				sb.AppendLine( "        " + c );
			sb.AppendLine( "    --Female Religions:" );
			foreach( string c in RuleSet.FemaleReligions )
				sb.AppendLine( "        " + c );

			sb.AppendLine( "    --Characters:" );
			foreach( CharacterRule rule in RuleSet.CharRules )
			{
				sb.AppendFormat( "        Character - IsFemale: {0}; Religion: {1}; Culture: {2};", rule.IsFemale, rule.Religion, rule.Culture );
				sb.AppendLine( " Titles: " + rule.Titles.Aggregate( "", ( current, t ) => current + ( t + "," ) ) );
			}

			sb.AppendLine( " --Duchy Allows Script: \n" + DuchyAllowsScript );
			sb.AppendLine( " --Duchy Gain Effects Script: \n" + DuchyEffectsScript );
			sb.AppendLine( " --Kingdom Allows Script: \n" + KingdomAllowsScript );
			sb.AppendLine( " --Kingdom Gain Effects Script: \n" + KingdomEffectsScript );
			sb.AppendLine( " --Empire Allows Script: \n" + EmpireAllowsScript );
			sb.AppendLine( " --Empire Gain Effects Script: \n" + EmpireEffectsScript );

			return sb.ToString();
		}

		public CultureOption CultureLimit;
		public ReligionOption ReligionLimit;
		public HistoryOption History;
		public CreateHistoryOption CreateHistoryType;
		public int StartDate;
		public int CharID;

		public int CountyLimit;
		public int DuchyLimit;
		public int KingdomLimit;

		public bool RandomTitleColour;

		public List<Mod> SelectedMods;
		public Random Random;
		public int Seed;

		public CK2Data Data;
		public HistoryState HistoryState;
		public bool UseMod;
		public bool ReplaceDeJure;
		public bool KingdomShortNames;
		public bool EmpireShortNames;

		public bool CreateDuchies;
		public bool CreateKingdoms;
		public bool CreateEmpires;
		public Mod Mod;
		public EReplace Replaces;

		public int HistoryMaxChar;
		public int HistoryMinChar;
		public int HistoryMaxReps;
		public int HistoryMinReps;
		public int HistoryMaxTheoc;
		public int HistoryMinTheoc;
		public int HistoryCulGroupMax;
		public int HistoryCulGroupMin;
		public int HistoryCulMin;
		public int HistoryCulMax;

		public string DuchyAllowsScript;
		public string KingdomAllowsScript;
		public string EmpireAllowsScript;
		public string DuchyEffectsScript;
		public string KingdomEffectsScript;
		public string EmpireEffectsScript;

		public RuleSet RuleSet;

		[Flags]
		public enum EReplace
		{
			Titles = 0x1,
			Cultures = 0x2,
			Religions = 0x4,
			Flags = 0x8,
			Localisation = 0x10,
			Provinces = 0x20,
			Dynasties = 0x40,
			Converter = 0x80
		}

		public enum CultureOption
		{
			None,
			Culture,
			CultureGroup
		}

		public enum ReligionOption
		{
			None,
			Religion,
			ReligionGroup
		}

		public enum HistoryOption
		{
			None,
			County,
			Duchy,
			Kingdom,
			Empire
		}

		public enum CreateHistoryOption
		{
			None,
			Counts,
			Dukes,
			Kings,
			Empires,
			Random
		}
	}

	public enum TitleLevel
	{
		Empire,
		Kingdom,
		Duchy,
		County
	}
}