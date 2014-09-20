using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TitleGenerator.Tasks
{
	class DeleteModTask : SharedTask
	{
		public DeleteModTask( Options options, Logger log ) : base( options, log )
		{
			
		}

		protected override bool Execute()
		{
			string dirTemp = Path.Combine( m_options.Data.MyDocsDir.FullName, m_options.Mod.Path ).Replace( '\\', '/' );
			if( Directory.Exists( dirTemp ) )
				Directory.Delete( dirTemp, true );
			return true;
		}
	}
}
