using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Parsers.Mod;

namespace TitleGenerator.Tasks.History
{
	internal class ClearCharactersTask : SharedTask
	{
		public ClearCharactersTask( Options options, Logger log ) : base( options, log ) {}

		protected override bool Execute()
		{
			Log( "Clearing Character Files" );
			
			List<string> files = new List<string>();
			DirectoryInfo dir;
			string charDir;

			charDir = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			charDir = Path.Combine( charDir, "history/characters" ).Replace( '\\', '/' );
			if ( !Directory.Exists( charDir ) )
				Directory.CreateDirectory( charDir );

			charDir = "history/characters";

			// See if vanilla is being loaded.
			bool loadVanilla = m_options.SelectedMods.All( m => !m.Replaces.Contains( charDir ) );

			if ( loadVanilla )
			{
				dir = new DirectoryInfo( Path.Combine( m_options.Data.InstallDir.FullName, charDir ).Replace( '\\', '/' ) );

				FileInfo[] list = dir.GetFiles( "*.txt" );
				foreach ( FileInfo f in list )
					if ( !files.Contains( f.Name ) )
						files.Add( f.Name );
			}

			// Files from selected mods.
			string dirTemp;
			foreach ( Mod m in m_options.SelectedMods )
			{
				dirTemp = m.ModPathType == ModReader.Folder.CKDir
					          ? m_options.Data.InstallDir.FullName
					          : m_options.Data.MyDocsDir.FullName;
				dirTemp = Path.Combine( dirTemp, m.Path );
				dirTemp = Path.Combine( dirTemp, charDir ).Replace( '\\', '/' );

				if ( !Directory.Exists( dirTemp ) )
					continue;

				dir = new DirectoryInfo( dirTemp );
				FileInfo[] list = dir.GetFiles( "*.txt" );
				foreach ( FileInfo f in list )
					if ( !files.Contains( f.Name ) )
						files.Add( f.Name );
			}


			// Create blanks.
			foreach( string f in files )
			{
				Log( " --" + f );
				CreateBlank( f );
			}

			return true;
		}

		private void CreateBlank( string s )
		{
			string filePath;
			FileInfo charFile;
			filePath = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			filePath = Path.Combine( filePath, "history/characters" );
			filePath = Path.Combine( filePath, s ).Replace( '\\', '/' );

			charFile = new FileInfo( filePath );
			charFile.Open( FileMode.Create, FileAccess.Write ).Close();
		}
	}
}