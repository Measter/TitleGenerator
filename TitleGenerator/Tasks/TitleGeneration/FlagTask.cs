using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Measter;
using Parsers.Mod;
using Parsers.Title;

namespace TitleGenerator.Tasks.TitleGeneration
{
	class FlagTask : SharedTask
	{
		public FlagTask( Options options, Logger log )
			: base( options, log )
		{

		}

		protected override bool Execute()
		{
			Log( "Creating Flags" );
			SendMessage( "Creating Flags" );

			string writePathStr = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			writePathStr = Path.Combine( writePathStr, "gfx/flags" ).Replace( '\\', '/' );

			DirectoryInfo writePath = new DirectoryInfo( writePathStr );
			if( !writePath.Exists )
				writePath.Create();

			//Youngest first.
			Mod[] revList = m_options.SelectedMods.ToArray();
			Array.Reverse( revList );

			if( m_options.UseMod )
			{
				DirectoryInfo dirTemp;
				string dirTempStr;
				foreach( var m in revList )
				{
					if( TaskStatus.Abort )
						return false;

					dirTemp = m.ModPathType == ModReader.Folder.CKDir ? m_options.Data.InstallDir : m_options.Data.MyDocsDir;
					dirTempStr = Path.Combine( dirTemp.FullName, m.Path ).Replace( '\\', '/' );
					dirTemp = new DirectoryInfo( dirTempStr );

					if( dirTemp.Exists )
						CreateFlags( dirTemp );
				}
			}

			if( !m_options.Replaces.IsFlagSet<Options.EReplace>( Options.EReplace.Flags ) )
			{
				if( TaskStatus.Abort )
					return false;
				CreateFlags( m_options.Data.InstallDir );
			}

			return true;
		}

		private void CreateFlags( DirectoryInfo dir )
		{
			DirectoryInfo readDir = new DirectoryInfo( Path.Combine( dir.FullName, "gfx/flags" ).Replace( '\\', '/' ) );

			string writeDirStr = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			writeDirStr = Path.Combine( writeDirStr, "gfx/flags" ).Replace( '\\', '/' );
			DirectoryInfo writeDir = new DirectoryInfo( writeDirStr );

			if( !readDir.Exists )
				return;

			if( TaskStatus.Abort )
				return;
			if( !writeDir.Exists )
				writeDir.Create();

			CreateFlagsFromCounties( writeDir, readDir );

			if( TaskStatus.Abort )
				return;
			CreateFlagsFromDuchies( writeDir, readDir );

			if( TaskStatus.Abort )
				return;
			CreateFlagsFromKingdoms( writeDir, readDir );
		}

		private void CreateFlagsFromKingdoms( DirectoryInfo writeDir, DirectoryInfo readDir )
		{
			if( !m_options.CreateEmpires )
				return;

			Log( "Getting Kingdom Flags" );

			FileInfo[] flags = readDir.GetFiles( "k_*.tga" );

			Log( "Creating Flags" );

			Title t;
			string flagName;
			foreach( FileInfo f in flags )
			{
				if( TaskStatus.Abort )
					return;

				flagName = f.Name.Replace( f.Extension, "" );
				SendMessage( "Creating Flags... " + flagName );

				Log( flagName );
				Log( " --Checking for Duchy" );

				t = FetchTitle( flagName, m_options.Data.Kingdoms );
				if( t == null || IsFilteredTitle( t ) )
					continue;

				if( !m_options.CreateEmpires )
					continue;
				Log( " --Checking for Empire" );
				flagName = "e_" + flagName.Substring( 2 );
				WriteFlag( writeDir, f, flagName, TitleLevel.Empire );
			}
		}

		private void CreateFlagsFromDuchies( DirectoryInfo writeDir, DirectoryInfo readDir )
		{
			if( !m_options.CreateKingdoms )
				return;

			Log( "Getting Duchy Flags" );

			FileInfo[] flags = readDir.GetFiles( "d_*.tga" );

			Log( "Creating Flags" );

			Title t;
			string flagName;
			foreach( FileInfo f in flags )
			{
				if( TaskStatus.Abort )
					return;

				flagName = f.Name.Replace( f.Extension, "" );
				SendMessage( "Creating Flags... " + flagName );

				Log( flagName );
				Log( " --Checking for Duchy" );

				t = FetchTitle( flagName, m_options.Data.Duchies );
				if( t == null || IsFilteredTitle( t ) )
					continue;

				Log( " --Checking for Kingdom" );
				flagName = "k_" + flagName.Substring( 2 );
				if( !WriteFlag( writeDir, f, flagName, TitleLevel.Kingdom ) )
					continue;

				if( !m_options.CreateEmpires )
					continue;
				Log( " --Checking for Empire" );
				flagName = "e_" + flagName.Substring( 2 );
				WriteFlag( writeDir, f, flagName, TitleLevel.Empire );
			}
		}

		private void CreateFlagsFromCounties( DirectoryInfo writeDir, DirectoryInfo readDir )
		{
			if( !m_options.CreateDuchies )
				return;

			Log( "Getting County Flags" );

			FileInfo[] flags = readDir.GetFiles( "c_*.tga" );

			Log( "Creating Flags" );

			Title t;
			string flagName;
			foreach( FileInfo f in flags )
			{
				if( TaskStatus.Abort )
					return;

				flagName = f.Name.Replace( f.Extension, "" );
				SendMessage( "Creating Flags... " + flagName );

				Log( flagName );
				Log( " --Checking for County" );

				t = FetchTitle( flagName, m_options.Data.Counties );
				if( t == null || IsFilteredTitle( t ) )
					continue;

				Log( " --Checking for Duchy" );
				flagName = "d_" + flagName.Substring( 2 );
				if( !WriteFlag( writeDir, f, flagName, TitleLevel.Duchy ) )
					continue;

				if( !m_options.CreateKingdoms )
					continue;
				Log( " --Checking for Kingdom" );
				flagName = "k_" + flagName.Substring( 2 );
				if( !WriteFlag( writeDir, f, flagName, TitleLevel.Kingdom ) )
					continue;

				if( !m_options.CreateEmpires )
					continue;
				Log( " --Checking for Empire" );
				flagName = "e_" + flagName.Substring( 2 );
				WriteFlag( writeDir, f, flagName, TitleLevel.Empire );
			}
		}

		private bool WriteFlag( DirectoryInfo writeDir, FileInfo f, string flagName, TitleLevel level )
		{
			ReadOnlyDictionary<string, Title> list;
			if( level == TitleLevel.Duchy )
				list = m_options.Data.Duchies;
			else if( level == TitleLevel.Kingdom )
				list = m_options.Data.Kingdoms;
			else
				list = m_options.Data.Empires;

			Title t = FetchTitle( flagName, list );
			if( t != null )
				return false;

			Log( " --Writing Flag" );
			string newPath = Path.Combine( writeDir.FullName, flagName ).Replace( '\\', '/' ) + ".tga";
			if( !File.Exists( newPath ) )
			{
				Log( " --" + flagName );
				f.CopyTo( newPath );
			}
			return true;
		}

		private Title FetchTitle( string s, ReadOnlyDictionary<string, Title> list )
		{
			return list.ToList().Find( d => d.Value.TitleID == s ).Value;
		}

		private static bool IsFilteredTitle( Title c )
		{
			if( c.Primary )
				return true;
			if( c.Capital == -1 )
				return true;
			if( c.Landless )
				return true;

			return false;
		}
	}
}
