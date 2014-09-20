using System.Collections.Generic;
using System.IO;
using Parsers.Dynasty;
using Parsers.Title;

namespace TitleGenerator.Tasks.History.Independent
{
	class IndependentCountsTask : CreateHistoryShared
	{
		public IndependentCountsTask( Options options, Logger log ) : base( options, log )
		{
			
		}

		protected override bool CreateHistory( StreamWriter charWriter )
		{
			Log( "Creating Counts" );

			Dictionary<int, Dynasty> availDynasties = new Dictionary<int, Dynasty>( m_options.Data.Dynasties );

			List<Title> titleList = new List<Title>( m_options.Data.Counties.Values );
			MakeCharactersForTitles( charWriter, availDynasties, titleList, false, null, false, null, null, null );

			return true;
		} 
	}
}
