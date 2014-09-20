using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TitleGenerator.Tasks
{
	class ModWriteTask : SharedTask
	{
		public ModWriteTask( Options options, Logger log ) : base( options, log )
		{
			
		}

		protected override bool Execute()
		{
			Log( "Creating .mod File" );
			SendMessage( "Writing Mod File" );

			ModWriter.CreateModFile( m_options.Data.MyDocsDir.FullName, m_options.Mod );

			Log( "Finished" );

			return true;
		}
	}
}
