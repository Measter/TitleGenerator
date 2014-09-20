using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Measter;
using Parsers.Title;
using DebugBreak = TitleGenerator.Includes.DebugBreak;

namespace TitleGenerator.Tasks.History.Clear
{
	class ClearHistoryShared : SharedTask
	{
		public ClearHistoryShared( Options options, Logger log )
			: base( options, log )
		{

		}

		protected void ClearHistoryCreateFiles( ReadOnlyDictionary<string, Title> titleDict )
		{
			char prefix;
			string newID;
			foreach( KeyValuePair<string, Title> c in titleDict )
			{
				if( TaskStatus.Abort )
					break;

				if( m_options.RuleSet.IgnoredTitles.Contains( c.Key ) )
				{
					Log( " --" + c.Value.TitleID + " ID in Ignored List." );
					continue;
				}

				prefix = c.Value.TitleID[0];
				CreateHistoryFile( c.Value );

				//newID = "d" + c.Value.TitleID.Substring( 1 );
				//if( prefix != 'c' || !m_options.CreateDuchies || m_options.Data.Duchies.ContainsKey( newID ) )
				//	continue;
				//CreateHistoryFile( newID );
			}
		}

		private void CreateHistoryFile( Title c )
		{
			string filePath;
			FileInfo titleFile;
			Log( " --" + c.TitleID + " History Cleared" );
			filePath = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			filePath = Path.Combine( filePath, "history/titles" );
			filePath = Path.Combine( filePath, c.TitleID + ".txt" ).Replace( '\\', '/' );

			titleFile = new FileInfo( filePath );
			StreamWriter writ = new StreamWriter( titleFile.Open( FileMode.Create, FileAccess.Write ),
												  Encoding.GetEncoding( 1252 ) );

			//if ( !c.TitleID.StartsWith( "c_" ) )
			//{													 
			//	writ.WriteLine("1.1.1={");

			//	if( c.TitleID.StartsWith( "d_" ) )
			//		writ.WriteLine( "\tde_jure_liege=k_null_title" );
			//	else
			//		writ.WriteLine( "\tde_jure_liege=0" );

			//	writ.WriteLine("}");
			//}

			writ.Dispose();
		}
	}
}
