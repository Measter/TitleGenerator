using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Parsers.Culture;
using Parsers.Dynasty;
using Parsers.Religion;
using TitleGenerator.HistoryRules;

namespace TitleGenerator.Tasks.History
{
	public struct CharacterOption
	{
		public RuleSet.Gender Gender;
		
		public List<Culture> CultureList;
		public Culture Culture;
		public bool SpecifiedCulture;
		
		public IEnumerable<Religion> ReligionList;
		public Religion Religion;
		public bool SpecifiedReligion;

		public IEnumerable<Dynasty> DynastyList;
		public Dynasty Dynasty;
		public bool SpecifiedDynasty;

		public int ID;

		public bool IsSpouse;
		public int PartnerAge;
	}
}
