using System.Collections.Generic;

namespace TitleGenerator.Tasks
{
	public interface ITask
	{
		event MessageUpdate Message;

		List<string> Errors
		{
			get;
			set;
		}

		bool Run();
	}

	public delegate void MessageUpdate( string message );
}
