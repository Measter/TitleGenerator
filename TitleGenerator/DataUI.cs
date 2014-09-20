using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using FolderSelect;
using Parsers.Mod;
using TitleGenerator.HistoryRules;
using TitleGenerator.Properties;
using TitleGenerator.Tasks;
using TitleGenerator.Tasks.History;
using TitleGenerator.Tasks.History.Clear;
using TitleGenerator.Tasks.History.Independent;
using TitleGenerator.Tasks.TitleGeneration;


namespace TitleGenerator
{
	public partial class DataUI : Form
	{
		private readonly FolderSelectDialog m_fsd;
		private CK2Data m_ck2Data;
		private Dictionary<string, FileInfo> m_ruleSets;
		private Dictionary<string, FileInfo> m_gainsScripts;
		private Dictionary<string, FileInfo> m_allowsScripts;
		private RuleSet m_rules;
		private Logger m_log;
		private ProgressPopup m_progressPopup;
		private List<Mod> m_selectedMods;
		private bool m_updatePath = true;
		private bool m_hasUIData = false;

		private SettingsStore m_settings;

		public DataUI()
		{
			InitializeComponent();

			m_fsd = new FolderSelectDialog();
			m_fsd.Title = "Please select your Crusader Kings II install directory.";

			m_log = new Logger();
			m_log.LogLength = 100;
			m_progressPopup = new ProgressPopup( m_log );

			m_ck2Data = new CK2Data( m_log );

			m_settings = new SettingsStore();
			m_settings.Filename = "settings.dat";
			m_settings.Version = 5;

			LogHeader();
		}

		private void LogHeader()
		{
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo( Assembly.GetExecutingAssembly().Location );
			m_log.Log( "Kingdoms Abound Titular Title Generator Version " + fvi.FileVersion, Logger.LogType.Setting );
			m_log.Log( "CK2 Utility Library Version " + Parsers.Version.GetVersion(), Logger.LogType.Setting );
			m_log.Log( "Logging started at " + DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss" ), Logger.LogType.Setting );
		}

		private void ShowError( string body, string title )
		{
			MessageBox.Show( this, body, title, MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
		}


		private Options GetOptions()
		{
			m_log.Log( "Getting Options", Logger.LogType.Data );

			Options o = new Options();
			Options.CultureOption co;
			Options.ReligionOption ro;
			Options.HistoryOption ho;
			Options.CreateHistoryOption cho;

			Mod m = GetMod();

			#region Culture/Religion

			if( cbTitleRestrictCulture.SelectedIndex == 0 )
				co = Options.CultureOption.None;
			else if( cbTitleRestrictCulture.SelectedIndex == 1 )
				co = Options.CultureOption.Culture;
			else
				co = Options.CultureOption.CultureGroup;

			if( cbTitleRestrictReligion.SelectedIndex == 0 )
				ro = Options.ReligionOption.None;
			else if( cbTitleRestrictReligion.SelectedIndex == 1 )
				ro = Options.ReligionOption.Religion;
			else
				ro = Options.ReligionOption.ReligionGroup;
			#endregion

			#region History
			if( cbHistoryMode.SelectedIndex == 0 )
			{
				ho = Options.HistoryOption.None;
				cho = Options.CreateHistoryOption.None;
			} else if( cbHistoryMode.SelectedIndex == 1 )
			{
				// Clear history mode.
				cho = Options.CreateHistoryOption.None;
				if( cbHistoryClearLevel.SelectedIndex == 0 )
					ho = Options.HistoryOption.Duchy;
				else if( cbHistoryClearLevel.SelectedIndex == 1 )
					ho = Options.HistoryOption.Kingdom;
				else
					ho = Options.HistoryOption.Empire;
			} else
			{
				// Create history mode.
				ho = Options.HistoryOption.County;
				if( cbHistoryCreateType.SelectedIndex == 0 )
					cho = Options.CreateHistoryOption.Counts;
				else if( cbHistoryCreateType.SelectedIndex == 1 )
					cho = Options.CreateHistoryOption.Dukes;
				else if( cbHistoryCreateType.SelectedIndex == 2 )
					cho = Options.CreateHistoryOption.Kings;
				else if( cbHistoryCreateType.SelectedIndex == 3 )
					cho = Options.CreateHistoryOption.Empires;
				else
					cho = Options.CreateHistoryOption.Random;
			}
			#endregion

			o.ReplaceDeJure = cbTitleReplaceDeJure.Checked;

			#region Replaces
			foreach( Mod mod in m_selectedMods )
			{
				if( mod.Replaces.Contains( @"common\landed_titles" ) )
					o.Replaces |= Options.EReplace.Titles;

				if( mod.Replaces.Contains( @"common\cultures" ) )
					o.Replaces |= Options.EReplace.Cultures;

				if( mod.Replaces.Contains( @"common\Religions" ) )
					o.Replaces |= Options.EReplace.Religions;

				if( mod.Replaces.Contains( @"gfx\flags" ) )
					o.Replaces |= Options.EReplace.Flags;

				if( mod.Replaces.Contains( @"localisation" ) )
					o.Replaces |= Options.EReplace.Localisation;

				if( mod.Replaces.Contains( @"history\provinces" ) )
					o.Replaces |= Options.EReplace.Provinces;

				if( mod.Replaces.Contains( @"common\dynasties" ) )
					o.Replaces |= Options.EReplace.Dynasties;

				if( mod.Replaces.Contains( @"common\eu4_converter" ) )
					o.Replaces |= Options.EReplace.Converter;
			}
			#endregion

			o.Data = m_ck2Data;
			o.HistoryState = new HistoryState();
			o.ReligionLimit = ro;
			o.CultureLimit = co;
			o.History = ho;
			o.CreateHistoryType = cho;

			int seed;
			if( cbHistorySeed.Checked )
				seed = (int)nudHistorySeed.Value;
			else
			{
				TimeSpan time = DateTime.Now - new DateTime( 1970, 1, 1, 0, 0, 0 );
				seed = (int)time.TotalSeconds;
			}
			o.Seed = seed;
			o.Random = new Random( seed );
			o.Data.SetMarkovRandom( o.Random );

			o.StartDate = (int)nudStartDate.Value;
			o.HistoryMaxChar = (int)nudHistoryMaxRealms.Value;
			o.HistoryMinChar = (int)nudHistoryMinRealms.Value;
			o.HistoryMaxReps = (int)nudHistoryMaxRepublics.Value;
			o.HistoryMinReps = (int)nudHistoryMinRepublics.Value;
			o.HistoryMinTheoc = (int)nudHistoryMinTheocracies.Value;
			o.HistoryMaxTheoc = (int)nudHistoryMaxTheocracies.Value;
			o.HistoryCulGroupMin = (int)nudHistoryCulGroupMin.Value;
			o.HistoryCulGroupMax = (int)nudHistoryCulGroupMax.Value;
			o.HistoryCulMin = (int)nudHistoryCulMin.Value;
			o.HistoryCulMax = (int)nudHistoryCulMax.Value;

			o.Mod = m;
			o.SelectedMods = m_selectedMods;

			o.KingdomShortNames = cbTitleShortKingdoms.Checked;
			o.EmpireShortNames = cbTitleShortEmpires.Checked;
			o.UseMod = m_selectedMods.Count > 0;

			o.CreateDuchies = cbTitleCreateDuchies.Checked;
			o.CreateKingdoms = cbTitleCreateKingdoms.Checked;
			o.CreateEmpires = cbTitleCreateEmpires.Checked;
			o.CountyLimit = (int)nudTitleLowerCounty.Value;
			o.DuchyLimit = (int)nudTitleLowerDuchy.Value;
			o.KingdomLimit = (int)nudTitleLowerKingdom.Value;
			o.RandomTitleColour = cbTitleRandomColour.Checked;

			ParseScripts( o );
			o.RuleSet = m_rules;

			o.CharID = m_rules.CharacterStartID != -1 ? m_rules.CharacterStartID : 100000000;

			return o;
		}

		private void ParseScripts( Options o )
		{
			List<string> scriptLines = new List<string>();
			string scriptName;

			m_log.Log( "Parsing Allows Scripts", Logger.LogType.Data );
			scriptName = m_allowsScripts[(string)lbAllowsList.SelectedItem].FullName;
			scriptLines.AddRange( File.ReadAllLines( scriptName ) );

			o.DuchyAllowsScript = Parse( scriptLines, o, 'd' );
			o.KingdomAllowsScript = Parse( scriptLines, o, 'k' );
			o.EmpireAllowsScript = Parse( scriptLines, o, 'e' );

			m_log.Log( "Parsing Gains Scripts", Logger.LogType.Data );
			scriptLines.Clear();
			scriptName = m_gainsScripts[(string)lbGainsList.SelectedItem].FullName;
			scriptLines.AddRange( File.ReadAllLines( scriptName ) );

			o.DuchyEffectsScript = Parse( scriptLines, o, 'd' );
			o.KingdomEffectsScript = Parse( scriptLines, o, 'k' );
			o.EmpireEffectsScript = Parse( scriptLines, o, 'e' );
		}

		private string Parse( List<string> lines, Options o, char level )
		{
			StringBuilder sb = new StringBuilder();
			string line;
			int condLvl = 0, delLevel = 0;
			bool delLine = false;

			for( int i = 0; i < lines.Count; i++ )
			{
				line = lines[i].Trim();

				if( line.StartsWith( "$EndIf", true, System.Globalization.CultureInfo.InvariantCulture ) )
				{
					if( condLvl == delLevel )
						delLine = false;

					condLvl--;
					continue;
				}

				if( line.StartsWith( "$If", true, System.Globalization.CultureInfo.InvariantCulture ) )
				{
					condLvl++;
					if( !delLine && !UsingCondition( line, o, level ) )
					{
						delLine = true;
						delLevel = condLvl;
					}
					continue;
				}

				if( !delLine )
				{
					sb.AppendLine( lines[i] );
					continue;
				}
			}

			return sb.ToString();
		}

		private bool UsingCondition( string line, Options o, char level )
		{
			if( line.Equals( "$IfNoCul", StringComparison.InvariantCultureIgnoreCase ) &&
				o.CultureLimit == Options.CultureOption.None )
				return true;
			if( line.Equals( "$IfCul", StringComparison.InvariantCultureIgnoreCase ) &&
				o.CultureLimit == Options.CultureOption.Culture )
				return true;
			if( line.Equals( "$IfCulGroup", StringComparison.InvariantCultureIgnoreCase ) &&
				o.CultureLimit == Options.CultureOption.CultureGroup )
				return true;

			if( line.Equals( "$IfNoRel", StringComparison.InvariantCultureIgnoreCase ) &&
				o.ReligionLimit == Options.ReligionOption.None )
				return true;
			if( line.Equals( "$IfRel", StringComparison.InvariantCultureIgnoreCase ) &&
				o.ReligionLimit == Options.ReligionOption.Religion )
				return true;
			if( line.Equals( "$IfRelGroup", StringComparison.InvariantCultureIgnoreCase ) &&
				o.ReligionLimit == Options.ReligionOption.ReligionGroup )
				return true;

			if( line.Equals( "$IfIsDuchy", StringComparison.InvariantCultureIgnoreCase ) && level == 'd' )
				return true;
			if( line.Equals( "$IfIsKingdom", StringComparison.InvariantCultureIgnoreCase ) && level == 'k' )
				return true;
			if( line.Equals( "$IfIsEmpire", StringComparison.InvariantCultureIgnoreCase ) && level == 'e' )
				return true;

			if( line.Equals( "$IfMakeDeJure", StringComparison.InvariantCultureIgnoreCase ) && o.ReplaceDeJure )
				return true;

			return false;
		}

		private Mod GetMod()
		{
			m_log.Log( "Getting Mod", Logger.LogType.Data );
			Mod m = new Mod();

			string[] invalidChars = {"-", "+", "/", "\\", " ", "[", "]", "<", ">",
                               "*", "^", "%", "$", "#", "@", "!", "(", ")",};

			string cleanedOutput = invalidChars.Aggregate( tbModName.Text, ( c, t ) => c.Replace( t, "" ) );

			m.Name = tbModName.Text;
			m.Path = Path.Combine( "mod", cleanedOutput ).Replace( '\\', '/' );
			m.ModFile = cleanedOutput + ".mod";
			m.ModPathType = ModReader.Folder.MyDocs;

			foreach( Mod mod in m_selectedMods )
				m.Dependencies.Add( mod.Name );

			return m;
		}

		#region Data Panel Functions
		private void LoadCK2()
		{
			m_ck2Data.LoadData( null, CK2Data.DataTypes.Mods );

			DirectoryInfo dir;

			string path = Path.Combine( Environment.CurrentDirectory, "scripts/rules" ).Replace( '\\', '/' );
			dir = new DirectoryInfo( path );
			if( !dir.Exists )
				throw new DirectoryNotFoundException( "Unable to find scripts/rules" );
			m_ruleSets = dir.GetFiles( "*.txt", SearchOption.TopDirectoryOnly ).ToDictionary( f => f.Name.Replace( f.Extension, "" ) );

			path = Path.Combine( Environment.CurrentDirectory, "scripts/gains" ).Replace( '\\', '/' );
			dir = new DirectoryInfo( path );
			if( !dir.Exists )
				throw new DirectoryNotFoundException( "Unable to find scripts/gains" );
			m_gainsScripts = dir.GetFiles( "*.txt", SearchOption.TopDirectoryOnly ).ToDictionary( f => f.Name.Replace( f.Extension, "" ) );

			path = Path.Combine( Environment.CurrentDirectory, "scripts/allows" ).Replace( '\\', '/' );
			dir = new DirectoryInfo( path );
			if( !dir.Exists )
				throw new DirectoryNotFoundException( "Unable to find scripts/allows" );
			m_allowsScripts = dir.GetFiles( "*.txt", SearchOption.TopDirectoryOnly ).ToDictionary( f => f.Name.Replace( f.Extension, "" ) );

			PopulateControls();
		}

		private void PopulateControls()
		{
			int i;
			m_log.Log( "Splitting Selected Rules List", Logger.LogType.Data );
			List<string> names = m_settings.SelectedRules.Split( ';' ).ToList();

			m_log.Log( "Listing Allows Scripts", Logger.LogType.Data );
			foreach( var fi in m_allowsScripts )
			{
				m_log.Log( " --" + fi.Key, Logger.LogType.Data );
				i = lbAllowsList.Items.Add( fi.Key );
				if( fi.Key == m_settings.AllowsSelected )
					lbAllowsList.SelectedIndex = i;
			}
			m_log.Log( "Listing Gains Scripts", Logger.LogType.Data );
			foreach( var fi in m_gainsScripts )
			{
				m_log.Log( " --" + fi.Key, Logger.LogType.Data );
				i = lbGainsList.Items.Add( fi.Key );
				if( fi.Key == m_settings.GainsSelected )
					lbGainsList.SelectedIndex = i;
			}
			m_log.Log( "Listing Rule Sets", Logger.LogType.Data );
			foreach( var fi in m_ruleSets )
			{
				m_log.Log( " --" + fi.Key, Logger.LogType.Data );
				i = cbRuleList.Items.Add( fi.Key );
				if( names.Contains( fi.Key ) )
					cbRuleList.SetItemChecked( i, true );
			}

			m_log.Log( "Fetching Mods", Logger.LogType.Data );
			ReadOnlyCollection<Mod> mods = m_ck2Data.GetMods;
			m_log.Log( "Splitting Selected Mods List", Logger.LogType.Data );
			names = m_settings.SelectedMods.Split( ';' ).ToList();

			m_log.Log( "Filtering and Listing Mods", Logger.LogType.Data );
			foreach( Mod m in mods )
			{
				m_log.Log( " --" + m.Name, Logger.LogType.Data );
				string dirTemp = m.ModPathType == ModReader.Folder.CKDir ? m_ck2Data.InstallDir.FullName : m_ck2Data.MyDocsDir.FullName;
				dirTemp = Path.Combine( dirTemp, m.Path );

				if( !Directory.Exists( Path.Combine( dirTemp, "common/landed_titles" ) )
				 && !Directory.Exists( Path.Combine( dirTemp, "common/religions" ) )
				 && !Directory.Exists( Path.Combine( dirTemp, "common/cultures" ) )
				 && !Directory.Exists( Path.Combine( dirTemp, "common/dynasties" ) )
				 && !Directory.Exists( Path.Combine( dirTemp, "gfx/flags" ) )
				 && !Directory.Exists( Path.Combine( dirTemp, "localisation" ) )
				 && !Directory.Exists( Path.Combine( dirTemp, "history/provinces" ) )
				 && !Directory.Exists( Path.Combine( dirTemp, "map" ) ) )
					continue;

				i = cbModList.Items.Add( m );
				if( names.Contains( m.Name ) )
					cbModList.SetItemChecked( i, true );
			}

			m_log.Log( "Setting Selected Indexes", Logger.LogType.Data );
			if( lbAllowsList.SelectedIndex == -1 )
				lbAllowsList.SelectedIndex = 0;
			if( lbGainsList.SelectedIndex == -1 )
				lbGainsList.SelectedIndex = 0;

			UpdateModName();
		}

		private void ClearCK2()
		{
			m_ck2Data.Clear( CK2Data.DataTypes.Mods | CK2Data.DataTypes.Religions | CK2Data.DataTypes.Cultures
						   | CK2Data.DataTypes.Dynasties | CK2Data.DataTypes.LandedTitles
						   | CK2Data.DataTypes.Localisations | CK2Data.DataTypes.Provinces );

			cbModList.Items.Clear();
			cbRuleList.Items.Clear();
			lbAllowsList.Items.Clear();
			lbGainsList.Items.Clear();
		}

		private void CheckCKDir( string selectedPath )
		{
			m_log.Log( "Checking Directory: " + selectedPath, Logger.LogType.Data );

			try
			{
				if( m_ck2Data.SetInstallPath( new DirectoryInfo( selectedPath ) ) )
				{
					if( m_updatePath )
						tbCKFolder.Text = selectedPath;

					gbAllows.Enabled = true;
					gbGains.Enabled = true;
					gbOther.Enabled = true;
					gbMods.Enabled = true;
					gbRuleSets.Enabled = true;
					btnLoadData.Enabled = true;

					ClearCK2();
					LoadCK2();
					m_hasUIData = true;
				} else
				{
					gbAllows.Enabled = false;
					gbGains.Enabled = false;
					gbOther.Enabled = false;
					gbMods.Enabled = false;
					gbRuleSets.Enabled = false;
					btnLoadData.Enabled = false;

					ShowError( "Unable to find CK2 install in path.\n\n" + selectedPath, "Unable to Find CK2" );

					ClearCK2();
					m_hasUIData = false;
				}
			} catch( Exception ex )
			{
				if( m_ck2Data.HadError )
				{
					m_log.Log( m_ck2Data.Error, Logger.LogType.Error );
					m_log.Log( "", Logger.LogType.Error );
				}

				m_log.Log( ex.ToString(), Logger.LogType.Error );
				m_log.Dump( "log.txt" );
				ShowError( "An error has been encountered while loading. Please check the log file.", "Error" );
			}
		}

		private void UpdateModName()
		{
			var checkedMods = cbModList.CheckedItems;

			if( checkedMods.Count == 0 )
				tbModName.Text = "Kingdoms Abound";
			else if( checkedMods.Count == 1 )
				tbModName.Text = "KA for " + ( (Mod)checkedMods[0] ).Name;
			else
				tbModName.Text = "KA for Multiple Mods";
		}

		private List<Mod> SortMods()
		{
			List<Mod> selected = cbModList.CheckedItems.Cast<Mod>().ToList();

			if( selected.Count <= 1 )
				return selected;

			for( int i = 1; i < selected.Count; i++ )
			{
				for( int j = 0; j < i; j++ )
				{
					Mod current = selected[j];
					Mod next = selected[i];

					if( !current.Dependencies.Contains( next.Name ) && next.Dependencies.Count != 0 )
						continue;

					selected[j] = next;
					selected[i] = current;
				}
			}

			return selected;
		}

		private bool LoadRulesets()
		{
			m_rules = new RuleSet();

			m_log.Log( "Loading Rule Sets", Logger.LogType.Data );
			var rules = cbRuleList.CheckedItems;
			RuleSet rs;
			List<RuleSet> ruleSets = new List<RuleSet>();
			foreach( string s in rules )
			{
				rs = RuleSet.Parse( m_ruleSets[s].FullName, s );

				if( rs.Errors.Count > 0 )
				{
					m_rules = rs;
					return false;
				}

				ruleSets.Add( rs );
			}

			m_log.Log( "Loaded " + ruleSets.Count + " Rule Sets", Logger.LogType.Data );

			#region Check Rule Sets Have Loaded Required Rules.
			bool hasAllLoaded = true;
			List<string> reqList = new List<string>();
			foreach( RuleSet r in ruleSets )
			{
				foreach( string requiredRule in r.RequiredRules )
				{
					if( !rules.Contains( requiredRule ) )
					{
						hasAllLoaded = false;
						reqList.Add( requiredRule );
					}
				}
			}

			if( !hasAllLoaded )
			{
				StringBuilder errorSb = new StringBuilder();
				errorSb.AppendLine( "One or more selected rule sets have requirements that are not checked." );
				errorSb.AppendLine().AppendLine();
				reqList.ForEach( s => errorSb.AppendLine( s ) );
				errorSb.AppendLine();
				errorSb.Append( "Please check the required rule sets." );

				m_log.Log( errorSb.ToString(), Logger.LogType.Data );
				ShowError( errorSb.ToString(), "Rule Sets Required." );
				return false;
			}
			#endregion

			#region Sort Rule Sets
			if( ruleSets.Count > 1 )
			{
				for( int i = 1; i < ruleSets.Count; i++ )
				{
					for( int j = 0; j < i; j++ )
					{
						RuleSet curr = ruleSets[j];
						RuleSet next = ruleSets[i];
						if( curr.RequiredRules.Contains( next.Name ) && next.RequiredRules.Count != 0 )
							continue;

						ruleSets[j] = next;
						ruleSets[i] = curr;
					}
				}
			}
			#endregion

			m_log.Log( "Combining Rule Sets", Logger.LogType.Data );
			#region Combining
			foreach( RuleSet rule in ruleSets )
			{
				foreach( CharacterRule cr in rule.CharRules )
					if( !m_rules.CharRules.Contains( cr ) )
						m_rules.CharRules.Add( cr );

				foreach( string s in rule.IgnoredTitles )
					if( !m_rules.IgnoredTitles.Contains( s ) )
						m_rules.IgnoredTitles.Add( s );

				foreach( string s in rule.FemaleCultures )
					if( !m_rules.FemaleCultures.Contains( s ) )
						m_rules.FemaleCultures.Add( s );

				foreach( string s in rule.FemaleReligions )
					if( !m_rules.FemaleReligions.Contains( s ) )
						m_rules.FemaleReligions.Add( s );

				foreach( string s in rule.MaleCultures )
					if( !m_rules.MaleCultures.Contains( s ) )
						m_rules.MaleCultures.Add( s );

				foreach( string s in rule.MaleReligions )
					if( !m_rules.MaleReligions.Contains( s ) )
						m_rules.MaleReligions.Add( s );

				foreach( string s in rule.MuslimLawFollowers )
					if( !m_rules.MuslimLawFollowers.Contains( s ) )
						m_rules.MuslimLawFollowers.Add( s );

				foreach( Law l in rule.LawRules.GenderLaws )
					if( !m_rules.LawRules.GenderLaws.Contains( l ) )
						m_rules.LawRules.GenderLaws.Add( l );

				foreach( Law l in rule.LawRules.SuccessionLaws )
					if( !m_rules.LawRules.SuccessionLaws.Contains( l ) )
						m_rules.LawRules.SuccessionLaws.Add( l );

				if( rule.LawRules.LevyTax.Min != 0
				  && rule.LawRules.LevyTax.Normal != 0
				  && rule.LawRules.LevyTax.Large != 0
				  && rule.LawRules.LevyTax.Max != 0 )
				{
					m_rules.LawRules.LevyTax = rule.LawRules.LevyTax;
					m_rules.LawRules.ParentRuleSet = m_rules;
				}

				if( rule.LiegeCultureChance != -1 )
					m_rules.LiegeCultureChance = rule.LiegeCultureChance;
				if( rule.LiegeReligionChance != -1 )
					m_rules.LiegeReligionChance = rule.LiegeReligionChance;
				if( rule.FemaleRulerChance != -1 )
					m_rules.FemaleRulerChance = rule.FemaleRulerChance;
				if( rule.RulerSpouseChance != -1 )
					m_rules.RulerSpouseChance = rule.RulerSpouseChance;
				if( rule.RepsForceCustomDuchies != null )
					m_rules.RepsForceCustomDuchies = rule.RepsForceCustomDuchies;
				if( rule.RepublicExpandChance != -1 )
					m_rules.RepublicExpandChance = rule.RepublicExpandChance;
				if( rule.RepublicExpandMax != -1 )
					m_rules.RepublicExpandMax = rule.RepublicExpandMax;

				if( rule.EmpireMinSize != -1 )
					m_rules.EmpireMinSize = rule.EmpireMinSize;
				if( rule.KingdomMinSize != -1 )
					m_rules.KingdomMinSize = rule.KingdomMinSize;
				if( rule.DuchyMinSize != -1 )
					m_rules.DuchyMinSize = rule.DuchyMinSize;

				if( rule.ClearCharacters != null )
					m_rules.ClearCharacters = rule.ClearCharacters;
				if( rule.CharacterStartID != -1 )
					m_rules.CharacterStartID = rule.CharacterStartID;

				if( rule.CulGenDynastyPrefix != -1 )
					m_rules.CulGenDynastyPrefix = rule.CulGenDynastyPrefix;
				if( rule.CulGenBastardPrefix != -1 )
					m_rules.CulGenBastardPrefix = rule.CulGenBastardPrefix;

				if( rule.CulGenPatronymIsPrefix != -1 )
					m_rules.CulGenPatronymIsPrefix = rule.CulGenPatronymIsPrefix;
				if( rule.CulGenMalePatronym != -1 )
					m_rules.CulGenMalePatronym = rule.CulGenMalePatronym;
				if( rule.CulGenFemalePatronym != -1 )
					m_rules.CulGenFemalePatronym = rule.CulGenFemalePatronym;

				if( rule.CulGenAncestorName != -1 )
					m_rules.CulGenAncestorName = rule.CulGenAncestorName;
				if( rule.CulGenDisinheritFromBlinding != -1 )
					m_rules.CulGenDisinheritFromBlinding = rule.CulGenDisinheritFromBlinding;
				if( rule.CulGenDukesCalledKings != -1 )
					m_rules.CulGenDukesCalledKings = rule.CulGenDukesCalledKings;
				if( rule.CulGenFounderNamesDynasty != -1 )
					m_rules.CulGenFounderNamesDynasty = rule.CulGenFounderNamesDynasty;
				if( rule.CulGenDynastyTitleNames != -1 )
					m_rules.CulGenDynastyTitleNames = rule.CulGenDynastyTitleNames;
			}
			#endregion

			return true;
		}

		private void VerifyData()
		{
			bool isInvalid = false;

			if( m_ck2Data.Counties == null || m_ck2Data.Duchies == null )
				isInvalid = true;
			if( m_ck2Data.Kingdoms == null || m_ck2Data.Empires == null )
				isInvalid = true;
			if( m_ck2Data.Cultures == null || m_ck2Data.CultureGroups == null )
				isInvalid = true;
			if( m_ck2Data.Religions == null || m_ck2Data.ReligionGroups == null )
				isInvalid = true;
			if( m_ck2Data.Dynasties == null || m_ck2Data.Provinces == null )
				isInvalid = true;

			if( isInvalid )
				throw new Exception( "One or more data dictionaries are null." );

			if( m_ck2Data.Counties.Count == 0 || m_ck2Data.Duchies.Count == 0 )
				isInvalid = true;
			if( m_ck2Data.Kingdoms.Count == 0 || m_ck2Data.Empires.Count == 0 )
				isInvalid = true;
			if( m_ck2Data.Cultures.Count == 0 || m_ck2Data.CultureGroups.Count == 0 )
				isInvalid = true;
			if( m_ck2Data.Religions.Count == 0 || m_ck2Data.ReligionGroups.Count == 0 )
				isInvalid = true;
			if( m_ck2Data.Dynasties.Count == 0 || m_ck2Data.Provinces.Count == 0 )
				isInvalid = true;

			if( isInvalid )
				throw new Exception( "One or more data dictionaries are empty." );
		}

		#endregion

		private void RunTasks( List<ITask> tasks, string message, Logger.LogType type )
		{
			m_progressPopup.SetTasks( tasks, message );

			if( m_progressPopup.ShowDialog( this ) == DialogResult.Abort )
			{
				ShowError( "An error was encountered running the selected task.\n\nPlease check the log file.", "Unable to complete task." );
				m_log.Log( "An error was encountered running the selected task:", Logger.LogType.Data );
				foreach( ITask t in tasks )
					foreach( string s in t.Errors )
						m_log.Log( s, type );
			}
		}

		#region Data Panel Events
		private void btnCKFolder_Click( object sender, EventArgs e )
		{
			if( m_fsd.ShowDialog( this.Handle ) )
				CheckCKDir( m_fsd.FileName );
		}

		private void tbCKFolder_Leave( object sender, EventArgs e )
		{
			if( String.IsNullOrEmpty( tbCKFolder.Text ) )
				return;

			m_updatePath = false;
			CheckCKDir( tbCKFolder.Text );
			m_updatePath = true;
		}

		private void btnLoadData_Click( object sender, EventArgs e )
		{
			if( cbRuleList.CheckedItems.Count == 0 )
			{
				ShowError( "You must have at least one rule set selected to continue.", "Rule Set Required" );
				return;
			}

			m_log.Log( "Loading CK2 Data.", Logger.LogType.Data );

			FileInfo fi = m_gainsScripts[(string)lbGainsList.SelectedItem];
			if( !fi.Exists )
			{
				m_log.Log( "Unable to find gain_effects script:" + fi.Name, Logger.LogType.Data );
				m_log.Dump( "log.txt" );
				ShowError( "Cannot find Gain Effects script file: " + fi.Name, "Unable to load data." );
				return;
			}

			fi = m_allowsScripts[(string)lbAllowsList.SelectedItem];
			if( !fi.Exists )
			{
				m_log.Log( "Unable to find gain_effects script:" + fi.Name, Logger.LogType.Data );
				m_log.Dump( "log.txt" );
				ShowError( "Cannot find Allows script file: " + fi.Name, "Unable to load data." );
				return;
			}

			m_log.Log( "Sorting Mods", Logger.LogType.Data );
			m_selectedMods = SortMods();
			m_log.Log( m_selectedMods.Count + " Mods Sorted", Logger.LogType.Data );

			if( !LoadRulesets() )
			{
				ShowError( "An error was encountered while parsing the Rule Sets.\n\nPlease check the log file.", "Unable to load data." );
				m_log.Log( "The following errors were encountered parsing the selected Rule Sets: ", Logger.LogType.Data );
				m_log.Dump( "log.txt" );
				foreach( string s in m_rules.Errors )
					m_log.Log( s, Logger.LogType.Data );

				return;
			}

			LoadTask lt = new LoadTask( m_ck2Data, m_log, m_selectedMods );
			List<ITask> tasks = new List<ITask>();
			tasks.Add( lt );
			RunTasks( tasks, "Loading Data from CKII", Logger.LogType.Data );

			VerifyData();

			btnGenerateTitles.Enabled = !TaskStatus.Abort;
		}

		private void cbModList_MouseUp( object sender, MouseEventArgs e )
		{
			UpdateModName();
		}

		private void cbFullLog_CheckedChanged( object sender, EventArgs e )
		{
			m_log.FullLog = cbFullLog.Checked;
		}
		#endregion

		private void DataUI_Load( object sender, EventArgs e )
		{
			m_settings.Load();

			this.Location = m_settings.WindowLocation;

			string dirTemp = m_settings.CKIIDirectory;
			if( !String.IsNullOrEmpty( dirTemp ) )
				CheckCKDir( dirTemp );

			cbHistoryMode.SelectedIndex = m_settings.HistoryMode;
			cbHistoryClearLevel.SelectedIndex = m_settings.HistoryClearLevel;
			cbHistoryCreateType.SelectedIndex = m_settings.HistoryCreateType;
			nudStartDate.Value = m_settings.HistoryStartDate;
			nudHistorySeed.Value = m_settings.HistorySeed;
			cbHistorySeed.Checked = m_settings.HistorySeedEnabled;
			nudHistoryMinRealms.Value = m_settings.HistoryMinRealms;
			nudHistoryMaxRealms.Value = m_settings.HistoryMaxRealms;
			nudHistoryMinRepublics.Value = m_settings.HistoryMinRepublics;
			nudHistoryMaxRepublics.Value = m_settings.HistoryMaxRepublics;
			nudHistoryMinTheocracies.Value = m_settings.HistoryMinTheocracies;
			nudHistoryMaxTheocracies.Value = m_settings.HistoryMaxTheocracies;
			nudHistoryCulGroupMax.Value = m_settings.HistoryCulGroupMax;
			nudHistoryCulGroupMin.Value = m_settings.HistoryCulGroupMin;
			nudHistoryCulMax.Value = m_settings.HistoryCulMax;
			nudHistoryCulMin.Value = m_settings.HistoryCulMin;
			cbHistoryCultureGen.Checked = m_settings.HistoryGenerateCultures;

			cbTitleCreateDuchies.Checked = m_settings.TitleCreateDuchies;
			cbTitleCreateKingdoms.Checked = m_settings.TitleCreateKingdoms;
			cbTitleCreateEmpires.Checked = m_settings.TitleCreateEmpires;
			cbTitleShortKingdoms.Checked = m_settings.TitleShortKingdoms;
			cbTitleShortEmpires.Checked = m_settings.TitleShortEmpires;
			cbTitleRandomColour.Checked = m_settings.TitleRandomColour;

			cbTitleRestrictCulture.SelectedIndex = m_settings.TitleRestrictCulture;
			cbTitleRestrictReligion.SelectedIndex = m_settings.TitleRestrictReligion;
			nudTitleLowerCounty.Value = m_settings.TitleRestrictCounty;
			nudTitleLowerDuchy.Value = m_settings.TitleRestrictDuchy;
			nudTitleLowerKingdom.Value = m_settings.TitleRestrictKingdom;

			cbTitleReplaceDeJure.Checked = m_settings.TitleReplaceDeJure;
		}

		private void DataUI_FormClosed( object sender, FormClosedEventArgs e )
		{
			if( !m_hasUIData )
				return;

			m_settings.WindowLocation = this.Location;

			m_settings.CKIIDirectory = m_ck2Data.InstallDir.FullName;
			m_settings.AllowsSelected = (string)lbAllowsList.SelectedItem;
			m_settings.GainsSelected = (string)lbGainsList.SelectedItem;

			CheckedListBox.CheckedItemCollection checkedItems = cbRuleList.CheckedItems;
			string temp = checkedItems.Cast<string>().Aggregate( string.Empty, ( c, m ) => c + ( m + ";" ) );
			m_settings.SelectedRules = temp;

			checkedItems = cbModList.CheckedItems;
			temp = checkedItems.Cast<Mod>().Aggregate( string.Empty, ( c, m ) => c + ( m.Name + ";" ) );
			m_settings.SelectedMods = temp;

			m_settings.HistoryMode = cbHistoryMode.SelectedIndex;
			m_settings.HistoryClearLevel = cbHistoryClearLevel.SelectedIndex;
			m_settings.HistoryCreateType = cbHistoryCreateType.SelectedIndex;
			m_settings.HistoryStartDate = nudStartDate.Value;
			m_settings.HistorySeed = nudHistorySeed.Value;
			m_settings.HistorySeedEnabled = cbHistorySeed.Checked;
			m_settings.HistoryMinRealms = nudHistoryMinRealms.Value;
			m_settings.HistoryMaxRealms = nudHistoryMaxRealms.Value;
			m_settings.HistoryMinRepublics = nudHistoryMinRepublics.Value;
			m_settings.HistoryMaxRepublics = nudHistoryMaxRepublics.Value;
			m_settings.HistoryMinTheocracies = nudHistoryMinTheocracies.Value;
			m_settings.HistoryMaxTheocracies = nudHistoryMaxTheocracies.Value;
			m_settings.HistoryCulGroupMin = nudHistoryCulGroupMin.Value;
			m_settings.HistoryCulGroupMax = nudHistoryCulGroupMax.Value;
			m_settings.HistoryCulMin = nudHistoryCulMin.Value;
			m_settings.HistoryCulMax = nudHistoryCulMax.Value;
			m_settings.HistoryGenerateCultures = cbHistoryCultureGen.Checked;

			m_settings.TitleCreateDuchies = cbTitleCreateDuchies.Checked;
			m_settings.TitleCreateKingdoms = cbTitleCreateKingdoms.Checked;
			m_settings.TitleCreateEmpires = cbTitleCreateEmpires.Checked;
			m_settings.TitleShortKingdoms = cbTitleShortKingdoms.Checked;
			m_settings.TitleShortEmpires = cbTitleShortEmpires.Checked;
			m_settings.TitleRandomColour = cbTitleRandomColour.Checked;

			m_settings.TitleRestrictCulture = cbTitleRestrictCulture.SelectedIndex;
			m_settings.TitleRestrictReligion = cbTitleRestrictReligion.SelectedIndex;
			m_settings.TitleRestrictCounty = nudTitleLowerCounty.Value;
			m_settings.TitleRestrictDuchy = nudTitleLowerDuchy.Value;
			m_settings.TitleRestrictKingdom = nudTitleLowerKingdom.Value;

			m_settings.TitleReplaceDeJure = cbTitleReplaceDeJure.Checked;

			m_settings.Save();

			if( cbFullLog.Checked )
				m_log.Dump( "log.txt" );
		}


		#region Generate Panel Events
		private void cbHistoryMode_SelectedIndexChanged( object sender, EventArgs e )
		{
			if( cbHistoryMode.SelectedIndex == 0 )
			{
				gbHistoryClearOptions.Enabled = false;
				gbHistoryGenerateOptions.Enabled = false;
			} else if( cbHistoryMode.SelectedIndex == 1 )
			{
				gbHistoryClearOptions.Enabled = true;
				gbHistoryGenerateOptions.Enabled = false;
			} else
			{
				gbHistoryClearOptions.Enabled = false;
				gbHistoryGenerateOptions.Enabled = true;
			}
		}


		private void cbHistorySeed_CheckedChanged( object sender, EventArgs e )
		{
			nudHistorySeed.Enabled = cbHistorySeed.CheckState == CheckState.Checked;
		}

		private void cbHistoryCreateType_SelectedIndexChanged( object sender, EventArgs e )
		{
			gbHistoryFullCreateOptions.Enabled = cbHistoryCreateType.SelectedIndex == 4;
		}

		private void cbHistoryCultureGen_CheckedChanged( object sender, EventArgs e )
		{
			gbHistoryCulGroups.Enabled = cbHistoryCultureGen.Checked;
			gbHistoryCultures.Enabled = cbHistoryCultureGen.Checked;
		}


		private void nudHistoryMinRealms_ValueChanged( object sender, EventArgs e )
		{
			if( nudHistoryMinRealms.Value > nudHistoryMaxRealms.Value )
				nudHistoryMinRealms.Value = nudHistoryMaxRealms.Value;
		}

		private void nudHistoryMaxRealms_ValueChanged( object sender, EventArgs e )
		{
			if( nudHistoryMaxRealms.Value < nudHistoryMinRealms.Value )
				nudHistoryMaxRealms.Value = nudHistoryMinRealms.Value;
		}

		private void nudHistoryMinRepublics_ValueChanged( object sender, EventArgs e )
		{
			if( nudHistoryMinRepublics.Value > nudHistoryMaxRepublics.Value )
				nudHistoryMinRepublics.Value = nudHistoryMaxRepublics.Value;
		}

		private void nudHistoryMaxRepublics_ValueChanged( object sender, EventArgs e )
		{
			if( nudHistoryMaxRepublics.Value < nudHistoryMinRepublics.Value )
				nudHistoryMaxRepublics.Value = nudHistoryMinRepublics.Value;
		}

		private void nudHistoryMinTheocracies_ValueChanged( object sender, EventArgs e )
		{
			if( nudHistoryMinTheocracies.Value > nudHistoryMaxTheocracies.Value )
				nudHistoryMinTheocracies.Value = nudHistoryMaxTheocracies.Value;
		}

		private void nudHistoryMaxTheocracies_ValueChanged( object sender, EventArgs e )
		{
			if( nudHistoryMaxTheocracies.Value < nudHistoryMinTheocracies.Value )
				nudHistoryMaxTheocracies.Value = nudHistoryMinTheocracies.Value;
		}


		private void nudHistoryCulGroupMin_ValueChanged( object sender, EventArgs e )
		{
			if( nudHistoryCulGroupMin.Value > nudHistoryCulGroupMax.Value )
				nudHistoryCulGroupMin.Value = nudHistoryCulGroupMax.Value;
		}

		private void nudHistoryCulGroupMax_ValueChanged( object sender, EventArgs e )
		{
			if( nudHistoryCulGroupMax.Value < nudHistoryCulGroupMin.Value )
				nudHistoryCulGroupMax.Value = nudHistoryCulGroupMin.Value;
		}

		private void nudHistoryCulMin_ValueChanged( object sender, EventArgs e )
		{
			if( nudHistoryCulMin.Value > nudHistoryCulMax.Value )
				nudHistoryCulMin.Value = nudHistoryCulMax.Value;
		}

		private void nudHistoryCulMax_ValueChanged( object sender, EventArgs e )
		{
			if( nudHistoryCulMax.Value < nudHistoryCulMin.Value )
				nudHistoryCulMax.Value = nudHistoryCulMin.Value;
		}


		private void GetHistoryTasks( List<ITask> taskList, Options options )
		{
			if( options.History != Options.HistoryOption.None )
			{
				if( options.History == Options.HistoryOption.County )
				{
					taskList.Add( new ClearCountsTask( options, m_log ) );
					taskList.Add( new ClearDukesTask( options, m_log ) );
					taskList.Add( new ClearKingsTask( options, m_log ) );
					taskList.Add( new ClearEmperorsTask( options, m_log ) );
				}
				if( options.History == Options.HistoryOption.Duchy )
				{
					taskList.Add( new ClearDukesTask( options, m_log ) );
					taskList.Add( new ClearKingsTask( options, m_log ) );
					taskList.Add( new ClearEmperorsTask( options, m_log ) );
				}
				if( options.History == Options.HistoryOption.Kingdom )
				{
					taskList.Add( new ClearKingsTask( options, m_log ) );
					taskList.Add( new ClearEmperorsTask( options, m_log ) );
				}
				if( options.History == Options.HistoryOption.Empire )
				{
					taskList.Add( new ClearEmperorsTask( options, m_log ) );
				}
			}

			if( options.CreateHistoryType != Options.CreateHistoryOption.None )
			{
				if( options.RuleSet.ClearCharacters == true )
					taskList.Add( new ClearCharactersTask( options, m_log ) );

				if( options.CreateHistoryType == Options.CreateHistoryOption.Counts )
					taskList.Add( new IndependentCountsTask( options, m_log ) );

				if( options.CreateHistoryType == Options.CreateHistoryOption.Dukes )
					taskList.Add( new IndependentDukesTask( options, m_log ) );

				if( options.CreateHistoryType == Options.CreateHistoryOption.Kings )
					taskList.Add( new IndependentKingsTask( options, m_log ) );

				if( options.CreateHistoryType == Options.CreateHistoryOption.Empires )
					taskList.Add( new IndependentEmperorsTask( options, m_log ) );

				if( options.CreateHistoryType == Options.CreateHistoryOption.Random )
					taskList.Add( new FullHistoryTask( options, m_log ) );
			}
		}

		private void btnGenerateTitles_Click( object sender, EventArgs e )
		{
			try
			{
				m_log.Log( "", Logger.LogType.Setting );
				Options o = GetOptions();
				m_log.Log( o.ToString(), Logger.LogType.Setting );
				nudHistorySeed.Value = o.Seed;

				List<ITask> taskList = new List<ITask>();
				//taskList.Add( new DumpDataTask( o, m_log ) );
				taskList.Add( new DeleteModTask( o, m_log ) );
				taskList.Add( new ApplyProvinceHistory( o, m_log ) );
				if( m_ck2Data.HasFullMarkovChains )
				{
					if( cbHistoryCultureGen.Checked )
						taskList.Add( new CultureGenerationTask( o, m_log ) );
				} else
				{
					var res = MessageBox.Show( this,
									"Unable to generate cultures because of incomplete Name Data.\n\n" +
									"Continue with generation?",
									"Incomplete Name Data",
									 MessageBoxButtons.YesNo, MessageBoxIcon.Warning );
					if( res == DialogResult.No )
						return;
				}
				taskList.Add( new CheckTitleTask( o, m_log ) );

				if( o.CreateDuchies || o.CreateKingdoms || o.CreateEmpires )
				{
					taskList.Add( new LocalisationTask( o, m_log ) );
					taskList.Add( new CreateTitleTask( o, m_log ) );
					taskList.Add( new FlagTask( o, m_log ) );
					taskList.Add( new NationTableTask( o, m_log ) );
				}

				GetHistoryTasks( taskList, o );

				taskList.Add( new ModWriteTask( o, m_log ) );
				taskList.Add( new ResetHistory( o, m_log ) );
				RunTasks( taskList, "Generating Titles", Logger.LogType.Generate );

				o.Data.ClearCustomCultures( o );

			} catch( Exception ex )
			{
				m_log.Log( ex.ToString(), Logger.LogType.Data );
				ShowError( "An error was encountered generating the mod.\n\nPlease check the log file.", "An Error was Encountered" );
				m_log.Dump( "log.txt" );
			}
		}


		#endregion
	}
}
