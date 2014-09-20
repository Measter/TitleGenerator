using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Measter;
using Parsers;
using Parsers.Culture;
using Parsers.Dynasty;
using Parsers.Localisations;
using Parsers.Map;
using Parsers.Mod;
using Parsers.Options;
using Parsers.Province;
using Parsers.Religion;
using Parsers.Title;
using TitleGenerator.HistoryRules;

namespace TitleGenerator
{
	public class CK2Data
	{
		[Flags]
		public enum DataTypes
		{
			Cultures = 1,
			Dynasties = 2,
			LandedTitles = 4,
			Nicknames = 8,
			Religions = 16,
			Traits = 32,
			Characters = 64,
			Localisations = 128,
			Mods = 256,
			Provinces = 512,
			ConvertTable = 1024,
			History = 2048,
			MarkovChains = 4096
		}

		private readonly ModReader m_modReader;
		private readonly CultureReader m_cultureReader;
		private readonly ReligionReader m_religionReader;
		private readonly DynastyReader m_dynastyReader;
		private readonly TitleReader m_titleReader;
		private readonly ProvinceReader m_provinceReader;

		private readonly LocalisationReader m_localisationStrings;
		private readonly ConverterTableReader m_converterTableReader;

		private readonly Logger m_log;

		private readonly Dictionary<string, CultureGroup> m_customCultureGroups;
		private readonly Dictionary<string, Culture> m_customCultures;
		private bool m_useCustomCulture;
		private readonly List<List<MarkovWordGenerator>> m_wordGenerators;
		public bool HasFullMarkovChains
		{
			get;
			private set;
		}


		public CK2Data( Logger log )
		{
			m_modReader = new ModReader();
			m_cultureReader = new CultureReader();
			m_religionReader = new ReligionReader();
			m_dynastyReader = new DynastyReader();
			m_titleReader = new TitleReader();
			m_localisationStrings = new LocalisationReader();
			m_provinceReader = new ProvinceReader();
			m_converterTableReader = new ConverterTableReader();

			m_customCultureGroups = new Dictionary<string, CultureGroup>();
			m_customCultures = new Dictionary<string, Culture>();
			m_wordGenerators = new List<List<MarkovWordGenerator>>();

			m_log = log;
			HadError = false;
		}

		public string Error
		{
			get;
			private set;
		}

		public bool HadError
		{
			get;
			private set;
		}

		public DirectoryInfo InstallDir
		{
			get;
			private set;
		}
		public DirectoryInfo MyDocsDir
		{
			get;
			private set;
		}

		#region Accessors

		public ReadOnlyCollection<Mod> GetMods
		{
			get;
			private set;
		}

		public ReadOnlyDictionary<string, CultureGroup> CultureGroups
		{
			get;
			private set;
		}
		public ReadOnlyDictionary<string, Culture> Cultures
		{
			get;
			private set;
		}

		public ReadOnlyDictionary<string, ReligionGroup> ReligionGroups
		{
			get;
			private set;
		}
		public ReadOnlyDictionary<string, Religion> Religions
		{
			get;
			private set;
		}

		public ReadOnlyDictionary<int, Dynasty> Dynasties
		{
			get;
			private set;
		}

		public ReadOnlyDictionary<string, Title> Counties
		{
			get;
			private set;
		}
		public ReadOnlyDictionary<string, Title> Duchies
		{
			get;
			private set;
		}
		public ReadOnlyDictionary<string, Title> Kingdoms
		{
			get;
			private set;
		}
		public ReadOnlyDictionary<string, Title> Empires
		{
			get;
			private set;
		}

		public ReadOnlyDictionary<string, string> Localisations
		{
			get;
			private set;
		}
		public ReadOnlyDictionary<int, Province> Provinces
		{
			get;
			private set;
		}

		public ReadOnlyDictionary<string, string> NationTable
		{
			get;
			private set;
		}

		public ReadOnlyDictionary<string, ReadOnlyCollection<EventOption>> History
		{
			get;
			private set;
		}

		#endregion

		public void SetCustomCultures( List<CultureGroup> cultureGroups )
		{
			m_useCustomCulture = true;
			m_customCultureGroups.Clear();
			m_customCultures.Clear();

			foreach( CultureGroup cg in cultureGroups )
			{
				m_customCultureGroups.Add( cg.Name, cg );
				foreach( var culture in cg.Cultures )
					m_customCultures.Add( culture.Key, culture.Value );
			}
		}

		public void ClearCustomCultures( Options options )
		{
			m_log.Log( "Clearing custom cultures from data.", Logger.LogType.Data );
			foreach( var pair in m_customCultureGroups )
			{
				options.RuleSet.MaleCultures.Remove( pair.Key );
				options.RuleSet.FemaleCultures.Remove( pair.Key );
				options.RuleSet.MuslimLawFollowers.Remove( pair.Key );

				foreach( Law law in options.RuleSet.LawRules.GenderLaws )
				{
					law.AllowedCultures.Remove( pair.Key );
					law.BannedCultures.Remove( pair.Key );
				}
				foreach( Law law in options.RuleSet.LawRules.SuccessionLaws )
				{
					law.AllowedCultures.Remove( pair.Key );
					law.BannedCultures.Remove( pair.Key );
				}
			}

			foreach( var pair in m_customCultures )
			{
				options.RuleSet.MaleCultures.Remove( pair.Key );
				options.RuleSet.FemaleCultures.Remove( pair.Key );
				options.RuleSet.MuslimLawFollowers.Remove( pair.Key );

				foreach( Law law in options.RuleSet.LawRules.GenderLaws )
				{
					law.AllowedCultures.Remove( pair.Key );
					law.BannedCultures.Remove( pair.Key );
				}
				foreach( Law law in options.RuleSet.LawRules.SuccessionLaws )
				{
					law.AllowedCultures.Remove( pair.Key );
					law.BannedCultures.Remove( pair.Key );
				}
			}

			m_useCustomCulture = false;
			m_customCultureGroups.Clear();
			m_customCultures.Clear();
		}

		public bool TryGetCulture( string key, out Culture cul )
		{
			bool found = false;

			if( m_useCustomCulture )
				found = m_customCultures.TryGetValue( key, out cul );

			if( !found )
				found = Cultures.TryGetValue( key, out cul );

			cul = null;
			return found;
		}
		public bool TryGetCultureGroup( string key, out CultureGroup culGroup )
		{
			bool found = false;

			if( m_useCustomCulture )
				found = m_customCultureGroups.TryGetValue( key, out culGroup );

			if( !found )
				found = CultureGroups.TryGetValue( key, out culGroup );

			culGroup = null;
			return found;
		}
		public bool ContainsCulture( string key )
		{
			bool found = false;
			if( m_useCustomCulture )
				found = m_customCultures.ContainsKey( key );

			if( !found )
				found = Cultures.ContainsKey( key );

			return found;
		}
		public bool ContainsCultureGroup( string key )
		{
			bool found = false;
			if( m_useCustomCulture )
				found = m_customCultureGroups.ContainsKey( key );

			if( !found )
				found = CultureGroups.ContainsKey( key );

			return found;
		}
		public Culture GetCulture( string key )
		{
			if( m_useCustomCulture && m_customCultures.ContainsKey( key ) )
				return m_customCultures[key];
			return Cultures[key];
		}
		public CultureGroup GetCultureGroup( string key )
		{
			if( m_useCustomCulture && m_customCultureGroups.ContainsKey( key ) )
				return m_customCultureGroups[key];
			return CultureGroups[key];
		}
		public KeyValuePair<string, Culture> GetRandomCulture( Random rand )
		{
			if( m_useCustomCulture && rand.Next(2) == 0 )
				return m_customCultures.RandomItem( rand );
			return Cultures.RandomItem( rand );
		}

		public bool SetInstallPath( DirectoryInfo ckDir )
		{
			if( !ckDir.Exists )
			{
				Error = "Unable to find the directory.\n\n" + ckDir.FullName;
				HadError = true;
				return false;
			}

			if( !File.Exists( Path.Combine( ckDir.FullName, "ck2.exe" ).Replace( '\\', '/' ) ) &&
				!File.Exists( Path.Combine( ckDir.FullName, "ck2" ).Replace( '\\', '/' ) ) &&
				!File.Exists( Path.Combine( ckDir.FullName, "CK2game.exe" ).Replace( '\\', '/' ) ) &&
				!File.Exists( Path.Combine( ckDir.FullName, "GK2game" ).Replace( '\\', '/' ) ) )
			{
				Error = "Unable to find Crusader Kings II in the following directory:\n\n" + ckDir.FullName;
				HadError = true;
				return false;
			}

			InstallDir = ckDir;
			SetModDirs();
			return true;
		}

		public void Clear( DataTypes dataList )
		{
			#region Cultures and Dynasties
			if( dataList.IsFlagSet<DataTypes>( DataTypes.Cultures ) )
			{
				m_cultureReader.CultureGroups.Clear();
				m_cultureReader.Cultures.Clear();
				m_cultureReader.Errors.Clear();
			}

			if( dataList.IsFlagSet<DataTypes>( DataTypes.Dynasties ) )
			{
				m_dynastyReader.Dynasties.Clear();
				m_dynastyReader.Errors.Clear();
			}
			#endregion

			#region Landed Titles
			if( dataList.IsFlagSet<DataTypes>( DataTypes.LandedTitles ) )
			{
				m_titleReader.Counties.Clear();
				m_titleReader.Duchies.Clear();
				m_titleReader.Kingdoms.Clear();
				m_titleReader.Empires.Clear();
				m_titleReader.Errors.Clear();
			}
			#endregion

			#region Religions and Traits
			if( dataList.IsFlagSet<DataTypes>( DataTypes.Religions ) )
			{
				m_religionReader.Religions.Clear();
				m_religionReader.ReligionGroups.Clear();
				m_religionReader.Errors.Clear();
			}
			#endregion

			#region Localisations
			if( dataList.IsFlagSet<DataTypes>( DataTypes.Localisations ) )
			{
				m_localisationStrings.LocalisationStrings.Clear();
				m_localisationStrings.Errors.Clear();
			}
			#endregion

			#region Mods and Provinces
			if( dataList.IsFlagSet<DataTypes>( DataTypes.Mods ) )
			{
				m_modReader.Mods.Clear();
				m_modReader.Errors.Clear();
			}

			if( dataList.IsFlagSet<DataTypes>( DataTypes.Provinces ) )
			{
				m_provinceReader.Provinces.Clear();
				m_provinceReader.Errors.Clear();
			}
			#endregion
		}

		public bool LoadData( List<Mod> selected, DataTypes dataList )
		{
			Clear( dataList );

			#region Dynasties and Cultures
			if( dataList.IsFlagSet<DataTypes>( DataTypes.Dynasties ) )
			{
				m_log.Log( "Loading Dynasties", Logger.LogType.Data );
				if( !LoadData( m_dynastyReader, selected, @"common\dynasties", "dynasties", "*.txt", true ) )
					return false;
				Dynasties = new ReadOnlyDictionary<int, Dynasty>( m_dynastyReader.Dynasties );
				m_log.Log( "  --Dynasties: " + m_dynastyReader.Dynasties.Count, Logger.LogType.Data );
			}

			if( dataList.IsFlagSet<DataTypes>( DataTypes.Cultures ) )
			{
				m_log.Log( "Loading Cultures", Logger.LogType.Data );
				if( !LoadData( m_cultureReader, selected, @"common\cultures", "cultures", "*.txt", true ) )
					return false;
				Cultures = new ReadOnlyDictionary<string, Culture>( m_cultureReader.Cultures );
				CultureGroups = new ReadOnlyDictionary<string, CultureGroup>( m_cultureReader.CultureGroups );
				m_log.Log( " --Culture Groups: " + m_cultureReader.CultureGroups.Count, Logger.LogType.Data );
				m_log.Log( " --Cultures: " + m_cultureReader.Cultures.Count, Logger.LogType.Data );
			}
			#endregion

			#region Titles
			if( dataList.IsFlagSet<DataTypes>( DataTypes.LandedTitles ) )
			{
				m_log.Log( "Loading Titles", Logger.LogType.Data );
				if( !LoadData( m_titleReader, selected, @"common\landed_titles", "landed_titles", "*.txt", true ) )
					return false;
				Counties = new ReadOnlyDictionary<string, Title>( m_titleReader.Counties );
				Duchies = new ReadOnlyDictionary<string, Title>( m_titleReader.Duchies );
				Kingdoms = new ReadOnlyDictionary<string, Title>( m_titleReader.Kingdoms );
				Empires = new ReadOnlyDictionary<string, Title>( m_titleReader.Empires );

				m_log.Log( " --Empires: " + m_titleReader.Empires.Count, Logger.LogType.Data );
				m_log.Log( " --Kingdoms: " + m_titleReader.Kingdoms.Count, Logger.LogType.Data );
				m_log.Log( " --Duchies: " + m_titleReader.Duchies.Count, Logger.LogType.Data );
				m_log.Log( " --Counties: " + m_titleReader.Counties.Count, Logger.LogType.Data );
			}
			#endregion

			#region Religions
			if( dataList.IsFlagSet<DataTypes>( DataTypes.Religions ) )
			{
				m_log.Log( "Loading Religions", Logger.LogType.Data );
				if( !LoadData( m_religionReader, selected, @"common\religions", "religions", "*.txt", true ) )
					return false;
				Religions = new ReadOnlyDictionary<string, Religion>( m_religionReader.Religions );
				ReligionGroups = new ReadOnlyDictionary<string, ReligionGroup>( m_religionReader.ReligionGroups );
				m_log.Log( " --Religion Groups: " + m_religionReader.ReligionGroups.Count, Logger.LogType.Data );
				m_log.Log( " --Religions: " + m_religionReader.Religions.Count, Logger.LogType.Data );
			}
			#endregion

			#region Localisations

			if( dataList.IsFlagSet<DataTypes>( DataTypes.Localisations ) )
			{
				m_log.Log( "Loading Localisations", Logger.LogType.Data );
				if( !LoadData( m_localisationStrings, selected, @"localisation", "localisations", "*.csv", true ) )
					return false;
				Localisations = new ReadOnlyDictionary<string, string>( m_localisationStrings.LocalisationStrings );
				m_log.Log( " --Localisations: " + m_localisationStrings.LocalisationStrings.Count, Logger.LogType.Data );
			}
			#endregion

			#region Provinces and Mods
			if( dataList.IsFlagSet<DataTypes>( DataTypes.Provinces ) )
			{
				m_log.Log( "Loading Provinces", Logger.LogType.Data );
				if( !LoadData( m_provinceReader, selected, @"history\provinces", "provinces", "*.txt", true ) )
					return false;
				Provinces = new ReadOnlyDictionary<int, Province>( m_provinceReader.Provinces );
				m_log.Log( " --Provinces: " + m_provinceReader.Provinces.Count, Logger.LogType.Data );
			}

			if( dataList.IsFlagSet<DataTypes>( DataTypes.Mods ) )
			{
				m_log.Log( "Loading Mods", Logger.LogType.Data );
				if( !LoadMods() )
					return false;
				GetMods = m_modReader.Mods.AsReadOnly();
				m_log.Log( " --Mods:" + m_modReader.Mods.Count, Logger.LogType.Data );
			}
			#endregion

			#region Converter Table and History

			if( dataList.IsFlagSet<DataTypes>( DataTypes.ConvertTable ) )
			{
				m_log.Log( "Loading Converter Table", Logger.LogType.Data );
				if( !LoadData( m_converterTableReader, selected, "common/eu4_converter", "converter", "*.csv", false ) )
					return false;
				NationTable = new ReadOnlyDictionary<string, string>( m_converterTableReader.Nations );
				m_log.Log( " --Nation Tables: " + m_converterTableReader.Nations.Count, Logger.LogType.Data );
			}
			#endregion

			if( dataList.IsFlagSet<DataTypes>( DataTypes.MarkovChains ) )
			{
				m_log.Log( "Loading Markov Chains", Logger.LogType.Data );
				HasFullMarkovChains = LoadMarkovChains();
				if( HasFullMarkovChains )
					m_log.Log( "Markov Chains Loaded.", Logger.LogType.Data );
			}

			return true;
		}

		private void OutputError( ReaderBase reader, string errorString )
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine( "Errors encountered loading " + errorString + ":" );
			sb.AppendLine();

			foreach( string err in reader.Errors )
				sb.AppendLine( err );

			Error = sb.ToString();
			HadError = true;
		}

		public Dynasty GetDynastyByCulture( Dictionary<int, Dynasty> availDynasties, Culture cul, Random rand )
		{
			if( m_useCustomCulture && cul.CustomFlags.ContainsKey( "orig" ) )
				cul = (Culture)cul.CustomFlags["orig"];

			return m_dynastyReader.GetDynastyByCulture( availDynasties, cul, rand );
		}


		private void SetModDirs()
		{
			string myDocs = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
			MyDocsDir = new DirectoryInfo( Path.Combine( myDocs, @"Paradox Interactive\Crusader Kings II\" ).Replace( '\\', '/' ) );
		}

		private bool LoadMods()
		{
			m_modReader.Mods.Clear();
			m_modReader.Errors.Clear();

			m_modReader.ParseFolder( Path.Combine( InstallDir.FullName, @"mod" ).Replace( '\\', '/' ), ModReader.Folder.CKDir );
			m_modReader.ParseFolder( Path.Combine( MyDocsDir.FullName, @"mod" ).Replace( '\\', '/' ), ModReader.Folder.MyDocs );

			if( m_modReader.Errors.Count != 0 )
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendLine( "Errors encountered during mod loading: " );
				sb.AppendLine();

				foreach( string err in m_modReader.Errors )
					sb.AppendLine( err );

				Error = sb.ToString();
				HadError = true;

				return false;
			}

			return true;
		}

		public bool LinkData( List<Mod> selected, DataTypes dataList )
		{
			if ( dataList.IsFlagSet<DataTypes>( DataTypes.Cultures | DataTypes.Dynasties ) )
				m_dynastyReader.LinkCultures( m_cultureReader.Cultures );

			if( dataList.IsFlagSet<DataTypes>( DataTypes.LandedTitles | DataTypes.Provinces ) )
				m_titleReader.LinkCounties( m_provinceReader.Provinces );

			//Parse adjacencies
			if ( dataList.IsFlagSet<DataTypes>( DataTypes.Provinces ) )
				if ( !ParseAdjacencies( selected ) )
					return false;

			return true;
		}

		private bool ParseAdjacencies( List<Mod> selected )
		{
			m_log.Log( "Parsing Adjacencies", Logger.LogType.Data );
			
			string userDir = string.Empty, dirTemp;
			string mapFile = Path.Combine( InstallDir.FullName, "map/default.map" ).Replace( '\\', '/' );

			//Discover which default.map and setup.log file to read.
			foreach( Mod m in selected )
			{
				dirTemp = m.ModPathType == ModReader.Folder.CKDir ? InstallDir.FullName : MyDocsDir.FullName;
				dirTemp = Path.Combine( dirTemp, m.Path );
				dirTemp = Path.Combine( dirTemp, "map/default.map" ).Replace( '\\', '/' );

				if( File.Exists( dirTemp ) )
				{
					mapFile = dirTemp;
					userDir = m.UserDir;
				}
			}

			string setupLog = Path.Combine( MyDocsDir.FullName, userDir );
			setupLog = Path.Combine( setupLog, "logs/setup.log" ).Replace( '\\', '/' );

			if ( !File.Exists( setupLog ) )
			{
				// Fall back to vanilla file.
				m_log.Log( "Unable to find the following file: " + setupLog, Logger.LogType.Error );
				m_log.Log( "Falling back to vanilla", Logger.LogType.Error );

				setupLog = Path.Combine( MyDocsDir.FullName, "logs/setup.log" ).Replace( '\\', '/' );
			}

			if( !File.Exists( setupLog ) )
			{
				Error = "Unable to find the following file: ";
				Error += setupLog;
				Error += "\n";
				Error += "Please start CKII with the mods you wish to use.";
				HadError = true;
				return false;
			}

			Map map = Map.Parse( mapFile );

			//Discover which mod to get adjacency from.
			string adjFile = Path.Combine( InstallDir.FullName, "map/" );
			adjFile = Path.Combine( adjFile, map.Adjacencies ).Replace( '\\', '/' );
			foreach( Mod m in selected )
			{
				dirTemp = m.ModPathType == ModReader.Folder.CKDir ? InstallDir.FullName : MyDocsDir.FullName;
				dirTemp = Path.Combine( dirTemp, m.Path );
				dirTemp = Path.Combine( dirTemp, "map/" + map.Adjacencies ).Replace( '\\', '/' );

				if( File.Exists( dirTemp ) )
					adjFile = dirTemp;
			}

			m_provinceReader.ParseAdjacencies( setupLog, map, adjFile );
			return true;
		}


		private bool LoadData( ReaderBase reader, IEnumerable<Mod> mods, string subDir, string errorString, string ext, bool falseOnNoDirectory )
		{
			Dictionary<string, string> files = new Dictionary<string, string>();
			DirectoryInfo dir;

			// Oldest First. Discover if vanilla should be loaded.
			bool loadVanilla = mods.All( m => !m.Replaces.Contains( subDir ) );

			if( loadVanilla )
			{
				dir = new DirectoryInfo( Path.Combine( InstallDir.FullName, subDir ).Replace( '\\', '/' ) );

				// Hack so it works when the eu4 converter isn't present.
				if( !dir.Exists && falseOnNoDirectory )
					return false;
				if( !dir.Exists )
					return true;

				m_log.Log( "Loading files from " + dir.FullName, Logger.LogType.Data );
				FileInfo[] list = dir.GetFiles( ext );

				foreach( FileInfo f in list )
					files[f.Name] = f.FullName;
			}

			// If using mods, load files.
			string dirTemp;
			foreach( Mod m in mods )
			{
				dirTemp = m.ModPathType == ModReader.Folder.CKDir ? InstallDir.FullName : MyDocsDir.FullName;
				dirTemp = Path.Combine( dirTemp, m.Path );
				dirTemp = Path.Combine( dirTemp, subDir ).Replace( '\\', '/' );

				if( !Directory.Exists( dirTemp ) )
					continue;

				dir = new DirectoryInfo( dirTemp );
				m_log.Log( "Loading files from " + dir.FullName, Logger.LogType.Data );
				FileInfo[] list = dir.GetFiles( ext );

				foreach( FileInfo f in list )
					files[f.Name] = f.FullName;
			}

			foreach( var file in files )
			{
				m_log.Log( "Reading File: " + file.Value, Logger.LogType.Data );
				reader.Parse( file.Value );
			}

			if( reader.Errors.Count > 0 )
			{
				OutputError( reader, errorString );

				return false;
			}

			return true;
		}



		private bool LoadMarkovChains()
		{
			DirectoryInfo dir = new DirectoryInfo( "namedata" );
			if( !dir.Exists )
				return false;

			FileInfo[] files = dir.GetFiles( "*.bin" );
			var groups = files.GroupBy( f => f.Name[0] );

			MarkovWordGenerator gen;
			List<MarkovWordGenerator> genList;
			foreach( var gr in groups )
			{
				genList = new List<MarkovWordGenerator>();

				foreach( FileInfo file in gr )
				{
					using( FileStream fs = new FileStream( file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read ) )
					using( BinaryReader br = new BinaryReader( fs ) )
					{
						gen = new MarkovWordGenerator( br );
						genList.Add( gen );
					}
				}

				m_wordGenerators.Add( genList );
			}

			if ( m_wordGenerators.Count == 0 )
				return false;

			return true;
		}
		
		public List<string> GenerateWords( int genGroup, int genID, int count, int minLen, int maxLen )
		{
			List<string> names = new List<string>();

			List<MarkovWordGenerator> genList = m_wordGenerators[genGroup % m_wordGenerators.Count];
			MarkovWordGenerator gen = genList[genID % genList.Count];

			for( int i = 0; i < count; i++ )
			{
				names.Add( gen.NextName( minLen, maxLen ) );
			}

			return names;
		}

		public string GenerateWord( int genGroup, int genID, int minLen, int maxLen )
		{
			List<MarkovWordGenerator> genList = m_wordGenerators[genGroup % m_wordGenerators.Count];
			MarkovWordGenerator gen = genList[genID % genList.Count];

			return gen.NextName( minLen, maxLen );
		}

		public void SetMarkovRandom( Random rand )
		{
			foreach ( var genList in m_wordGenerators )
			{
				foreach ( var gen in genList )
				{
					gen.SetRandom( rand );
				}
			}
		}
	}
}