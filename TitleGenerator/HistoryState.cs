using System.Collections.Generic;
using Parsers.Province;
using Parsers.Title;

namespace TitleGenerator
{
	public class HistoryState
	{
		public Dictionary<string, Title> Empires;
		public Dictionary<string, Title> Kingdoms;
		public Dictionary<string, Title> Duchies;
		public Dictionary<string, Title> Counties;
		public Dictionary<int, Province> Provinces;

		public HistoryState()
		{
			Empires = new Dictionary<string, Title>();
			Kingdoms = new Dictionary<string, Title>();
			Duchies = new Dictionary<string, Title>();
			Counties = new Dictionary<string, Title>();
			Provinces = new Dictionary<int, Province>();
		}
	}
}