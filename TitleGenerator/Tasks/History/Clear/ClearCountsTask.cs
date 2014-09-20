using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TitleGenerator.Tasks.History.Clear
{
	class ClearCountsTask : ClearHistoryShared
	{
		public ClearCountsTask( Options options, Logger log ) : base( options, log )
		{
			
		}

		protected override bool Execute()
		{
			Log( "Clearing County History" );

			string path = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path );
			path = Path.Combine( path, "history/titles" ).Replace( '\\', '/' );

			if ( !Directory.Exists( path ) )
				Directory.CreateDirectory( path );

			ClearHistoryCreateFiles( m_options.Data.Counties );
			
			return true;
		}
	}
}
