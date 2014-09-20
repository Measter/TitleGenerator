using System.Collections.Generic;
using System.IO;
using Parsers.Dynasty;
using Parsers.Title;

namespace TitleGenerator.Tasks.History.Independent
{
	class IndependentDukesTask : CreateHistoryShared
	{
		public IndependentDukesTask( Options options, Logger log )
			: base( options, log )
		{
		}

		protected override bool CreateHistory( StreamWriter charWriter )
		{
			Log( "Creating Dukes" );

			Dictionary<int, Dynasty> availDynasties = new Dictionary<int, Dynasty>( m_options.Data.Dynasties );

			List<Title> titles = new List<Title>( m_options.Data.Duchies.Values );
			MakeCharactersForTitles( charWriter, availDynasties, titles, false, null, false, null, null, null );

			return true;
		}  
	}
}
