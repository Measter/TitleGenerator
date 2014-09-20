using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace TitleGenerator
{
	class SettingsStore
	{
		public string Filename;
		public byte Version;

		public string CKIIDirectory = string.Empty;
		public string AllowsSelected = string.Empty;
		public string GainsSelected = string.Empty;
		public string SelectedMods = string.Empty;
		public string SelectedRules = "VanillaRuleSet;";

		public int HistoryMode = 0;
		public int HistoryClearLevel = 0;
		public int HistoryCreateType = 4;
		public decimal HistoryStartDate = 1066;
		public decimal HistorySeed = 0;
		public bool HistorySeedEnabled = false;
		public bool HistoryGenerateCultures = false;

		public decimal HistoryMinRealms = 70;
		public decimal HistoryMaxRealms = 200;
		public decimal HistoryMinRepublics = 2;
		public decimal HistoryMaxRepublics = 4;
		public decimal HistoryMinTheocracies = 3;
		public decimal HistoryMaxTheocracies = 10;
		public decimal HistoryCulGroupMin = 5;
		public decimal HistoryCulGroupMax = 8;
		public decimal HistoryCulMin = 3;
		public decimal HistoryCulMax = 6;

		public bool TitleCreateDuchies = false;
		public bool TitleCreateKingdoms = true;
		public bool TitleCreateEmpires = true;
		public bool TitleShortKingdoms = false;
		public bool TitleShortEmpires = false;
		public bool TitleRandomColour = false;

		public int TitleRestrictCulture = 0;
		public int TitleRestrictReligion = 0;
		public decimal TitleRestrictCounty = 3;
		public decimal TitleRestrictDuchy = 4;
		public decimal TitleRestrictKingdom = 3;

		public bool TitleReplaceDeJure = true;

		public Point WindowLocation = new Point( 200, 200 );

		public void Load()
		{
			if( !File.Exists( Filename ) )
				return;

			using( FileStream fs = new FileStream( Filename, FileMode.Open, FileAccess.Read, FileShare.None ) )
			using( BinaryReader br = new BinaryReader( fs ) )
			{
				byte vers = br.ReadByte();
				if( vers == 1 )
					LoadVersion1( br );
				else if( vers == 2 )
					LoadVersion2( br );
				else if( vers == 3 )
					LoadVersion3( br );
				else if( vers == 4 )
					LoadVersion4( br );
				else
					LoadVersion5( br );
			}
		}

		private void LoadVersion1( BinaryReader br )
		{
			CKIIDirectory = br.ReadString();
			AllowsSelected = br.ReadString();
			GainsSelected = br.ReadString();
			SelectedRules = br.ReadString();
			SelectedMods = br.ReadString();

			HistoryMode = br.ReadInt32();
			HistoryClearLevel = br.ReadInt32();
			HistoryCreateType = br.ReadInt32();
			HistoryStartDate = br.ReadDecimal();
			HistorySeed = br.ReadDecimal();
			HistorySeedEnabled = br.ReadBoolean();
			br.ReadBoolean(); // Was History Create Kingdoms.

			HistoryMinRealms = br.ReadDecimal();
			HistoryMaxRealms = br.ReadDecimal();
			HistoryMinRepublics = br.ReadDecimal();
			HistoryMaxRepublics = br.ReadDecimal();
			HistoryMinTheocracies = br.ReadDecimal();
			HistoryMaxTheocracies = br.ReadDecimal();

			TitleCreateDuchies = br.ReadBoolean();
			TitleCreateKingdoms = br.ReadBoolean();
			TitleCreateEmpires = br.ReadBoolean();
			TitleShortKingdoms = br.ReadBoolean();
			TitleShortEmpires = br.ReadBoolean();

			TitleRestrictCulture = br.ReadInt32();
			TitleRestrictReligion = br.ReadInt32();
			TitleRestrictCounty = br.ReadDecimal();
			TitleRestrictDuchy = br.ReadDecimal();
			TitleRestrictKingdom = br.ReadDecimal();

			TitleReplaceDeJure = br.ReadBoolean();

			WindowLocation = new Point( br.ReadInt32(), br.ReadInt32() );
		}

		private void LoadVersion2( BinaryReader br )
		{
			CKIIDirectory = br.ReadString();
			AllowsSelected = br.ReadString();
			GainsSelected = br.ReadString();
			SelectedRules = br.ReadString();
			SelectedMods = br.ReadString();

			HistoryMode = br.ReadInt32();
			HistoryClearLevel = br.ReadInt32();
			HistoryCreateType = br.ReadInt32();
			HistoryStartDate = br.ReadDecimal();
			HistorySeed = br.ReadDecimal();
			HistorySeedEnabled = br.ReadBoolean();
			HistoryGenerateCultures = br.ReadBoolean();
			br.ReadBoolean(); // Was History Create Kingdoms.

			HistoryMinRealms = br.ReadDecimal();
			HistoryMaxRealms = br.ReadDecimal();
			HistoryMinRepublics = br.ReadDecimal();
			HistoryMaxRepublics = br.ReadDecimal();
			HistoryMinTheocracies = br.ReadDecimal();
			HistoryMaxTheocracies = br.ReadDecimal();

			TitleCreateDuchies = br.ReadBoolean();
			TitleCreateKingdoms = br.ReadBoolean();
			TitleCreateEmpires = br.ReadBoolean();
			TitleShortKingdoms = br.ReadBoolean();
			TitleShortEmpires = br.ReadBoolean();

			TitleRestrictCulture = br.ReadInt32();
			TitleRestrictReligion = br.ReadInt32();
			TitleRestrictCounty = br.ReadDecimal();
			TitleRestrictDuchy = br.ReadDecimal();
			TitleRestrictKingdom = br.ReadDecimal();

			TitleReplaceDeJure = br.ReadBoolean();

			WindowLocation = new Point( br.ReadInt32(), br.ReadInt32() );
		}

		private void LoadVersion3( BinaryReader br )
		{
			CKIIDirectory = br.ReadString();
			AllowsSelected = br.ReadString();
			GainsSelected = br.ReadString();
			SelectedRules = br.ReadString();
			SelectedMods = br.ReadString();

			HistoryMode = br.ReadInt32();
			HistoryClearLevel = br.ReadInt32();
			HistoryCreateType = br.ReadInt32();
			HistoryStartDate = br.ReadDecimal();
			HistorySeed = br.ReadDecimal();
			HistorySeedEnabled = br.ReadBoolean();
			HistoryGenerateCultures = br.ReadBoolean();
			br.ReadBoolean();  // Was History Create Kingdoms.

			HistoryMinRealms = br.ReadDecimal();
			HistoryMaxRealms = br.ReadDecimal();
			HistoryMinRepublics = br.ReadDecimal();
			HistoryMaxRepublics = br.ReadDecimal();
			HistoryMinTheocracies = br.ReadDecimal();
			HistoryMaxTheocracies = br.ReadDecimal();
			HistoryCulGroupMin = br.ReadDecimal();
			HistoryCulGroupMax = br.ReadDecimal();
			HistoryCulMin = br.ReadDecimal();
			HistoryCulMax = br.ReadDecimal();

			TitleCreateDuchies = br.ReadBoolean();
			TitleCreateKingdoms = br.ReadBoolean();
			TitleCreateEmpires = br.ReadBoolean();
			TitleShortKingdoms = br.ReadBoolean();
			TitleShortEmpires = br.ReadBoolean();

			TitleRestrictCulture = br.ReadInt32();
			TitleRestrictReligion = br.ReadInt32();
			TitleRestrictCounty = br.ReadDecimal();
			TitleRestrictDuchy = br.ReadDecimal();
			TitleRestrictKingdom = br.ReadDecimal();

			TitleReplaceDeJure = br.ReadBoolean();

			WindowLocation = new Point( br.ReadInt32(), br.ReadInt32() );
		}

		private void LoadVersion4( BinaryReader br )
		{
			CKIIDirectory = br.ReadString();
			AllowsSelected = br.ReadString();
			GainsSelected = br.ReadString();
			SelectedRules = br.ReadString();
			SelectedMods = br.ReadString();

			HistoryMode = br.ReadInt32();
			HistoryClearLevel = br.ReadInt32();
			HistoryCreateType = br.ReadInt32();
			HistoryStartDate = br.ReadDecimal();
			HistorySeed = br.ReadDecimal();
			HistorySeedEnabled = br.ReadBoolean();
			HistoryGenerateCultures = br.ReadBoolean();
			br.ReadBoolean();  // Was History Create Kingdoms.

			HistoryMinRealms = br.ReadDecimal();
			HistoryMaxRealms = br.ReadDecimal();
			HistoryMinRepublics = br.ReadDecimal();
			HistoryMaxRepublics = br.ReadDecimal();
			HistoryMinTheocracies = br.ReadDecimal();
			HistoryMaxTheocracies = br.ReadDecimal();
			HistoryCulGroupMin = br.ReadDecimal();
			HistoryCulGroupMax = br.ReadDecimal();
			HistoryCulMin = br.ReadDecimal();
			HistoryCulMax = br.ReadDecimal();

			TitleCreateDuchies = br.ReadBoolean();
			TitleCreateKingdoms = br.ReadBoolean();
			TitleCreateEmpires = br.ReadBoolean();
			TitleShortKingdoms = br.ReadBoolean();
			TitleShortEmpires = br.ReadBoolean();
			TitleRandomColour = br.ReadBoolean();

			TitleRestrictCulture = br.ReadInt32();
			TitleRestrictReligion = br.ReadInt32();
			TitleRestrictCounty = br.ReadDecimal();
			TitleRestrictDuchy = br.ReadDecimal();
			TitleRestrictKingdom = br.ReadDecimal();

			TitleReplaceDeJure = br.ReadBoolean();

			WindowLocation = new Point( br.ReadInt32(), br.ReadInt32() );
		}

		private void LoadVersion5( BinaryReader br )
		{
			CKIIDirectory = br.ReadString();
			AllowsSelected = br.ReadString();
			GainsSelected = br.ReadString();
			SelectedRules = br.ReadString();
			SelectedMods = br.ReadString();

			HistoryMode = br.ReadInt32();
			HistoryClearLevel = br.ReadInt32();
			HistoryCreateType = br.ReadInt32();
			HistoryStartDate = br.ReadDecimal();
			HistorySeed = br.ReadDecimal();
			HistorySeedEnabled = br.ReadBoolean();
			HistoryGenerateCultures = br.ReadBoolean();

			HistoryMinRealms = br.ReadDecimal();
			HistoryMaxRealms = br.ReadDecimal();
			HistoryMinRepublics = br.ReadDecimal();
			HistoryMaxRepublics = br.ReadDecimal();
			HistoryMinTheocracies = br.ReadDecimal();
			HistoryMaxTheocracies = br.ReadDecimal();
			HistoryCulGroupMin = br.ReadDecimal();
			HistoryCulGroupMax = br.ReadDecimal();
			HistoryCulMin = br.ReadDecimal();
			HistoryCulMax = br.ReadDecimal();

			TitleCreateDuchies = br.ReadBoolean();
			TitleCreateKingdoms = br.ReadBoolean();
			TitleCreateEmpires = br.ReadBoolean();
			TitleShortKingdoms = br.ReadBoolean();
			TitleShortEmpires = br.ReadBoolean();
			TitleRandomColour = br.ReadBoolean();

			TitleRestrictCulture = br.ReadInt32();
			TitleRestrictReligion = br.ReadInt32();
			TitleRestrictCounty = br.ReadDecimal();
			TitleRestrictDuchy = br.ReadDecimal();
			TitleRestrictKingdom = br.ReadDecimal();

			TitleReplaceDeJure = br.ReadBoolean();

			WindowLocation = new Point( br.ReadInt32(), br.ReadInt32() );
		}

		public void Save()
		{
			using( FileStream fs = new FileStream( Filename, FileMode.Create, FileAccess.Write, FileShare.None ) )
			using( BinaryWriter bw = new BinaryWriter( fs ) )
			{
				bw.Write( Version );

				bw.Write( CKIIDirectory );
				bw.Write( AllowsSelected );
				bw.Write( GainsSelected );
				bw.Write( SelectedRules );
				bw.Write( SelectedMods );

				bw.Write( HistoryMode );
				bw.Write( HistoryClearLevel );
				bw.Write( HistoryCreateType );
				bw.Write( HistoryStartDate );
				bw.Write( HistorySeed );
				bw.Write( HistorySeedEnabled );
				bw.Write( HistoryGenerateCultures );

				bw.Write( HistoryMinRealms );
				bw.Write( HistoryMaxRealms );
				bw.Write( HistoryMinRepublics );
				bw.Write( HistoryMaxRepublics );
				bw.Write( HistoryMinTheocracies );
				bw.Write( HistoryMaxTheocracies );
				bw.Write( HistoryCulGroupMin );
				bw.Write( HistoryCulGroupMax );
				bw.Write( HistoryCulMin );
				bw.Write( HistoryCulMax );

				bw.Write( TitleCreateDuchies );
				bw.Write( TitleCreateKingdoms );
				bw.Write( TitleCreateEmpires );
				bw.Write( TitleShortKingdoms );
				bw.Write( TitleShortEmpires );
				bw.Write( TitleRandomColour );

				bw.Write( TitleRestrictCulture );
				bw.Write( TitleRestrictReligion );
				bw.Write( TitleRestrictCounty );
				bw.Write( TitleRestrictDuchy );
				bw.Write( TitleRestrictKingdom );

				bw.Write( TitleReplaceDeJure );

				bw.Write( WindowLocation.X );
				bw.Write( WindowLocation.Y );
			}
		}
	}
}
