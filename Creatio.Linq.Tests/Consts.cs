using System;

namespace Creatio.Linq.Tests
{
	/// <summary>
	/// Self-explanatory.
	/// </summary>
	public partial class Consts
	{
		public static class Account
		{
			public static class Type
			{
				public static readonly Guid Customer = new Guid("03A75490-53E6-DF11-971B-001D60E938C6");
				public static readonly Guid Partner = new Guid("F2C0CE97-53E6-DF11-971B-001D60E938C6");
				public static readonly Guid Supplier = new Guid("D34B9DA2-53E6-DF11-971B-001D60E938C6");
				public static readonly Guid OurCompany = new Guid("57412FAD-53E6-DF11-971B-001D60E938C6");
			}

			public static class Category
			{
				public static readonly Guid A = new Guid("37EA507C-55E6-DF11-971B-001D60E938C6");
				public static readonly Guid B = new Guid("38EA507C-55E6-DF11-971B-001D60E938C6");
				public static readonly Guid C = new Guid("54E14A78-22CF-49D5-98F9-932C33E358CE");
				public static readonly Guid D = new Guid("A2C7D7BC-EE8D-46D1-BE19-AC963F31F359");
			}

			public static class Industry
			{
				public static readonly Guid Construction = new Guid("B87F1E2C-F46B-1410-C393-00155D043205");
				public static readonly Guid IT = new Guid("B87F3E3E-F46B-1410-C393-00155D043205");
				public static readonly Guid Banks = new Guid("BD7F1E4A-F36B-1410-C493-00155D043205");
				public static readonly Guid Consulting = new Guid("FB7F1E5C-F36B-1410-C493-00155D043205");
			}
		}

		public static class Contact
		{
			public static class Type
			{
				public static readonly Guid ContactPerson = new Guid("806732EE-F36B-1410-A883-16D83CAB0980");
				public static readonly Guid Customer = new Guid("00783EF6-F36B-1410-A883-16D83CAB0980");
				public static readonly Guid Employee = new Guid("60733EFC-F36B-1410-A883-16D83CAB0980");
			}
		}


		public static class CommunicationType
		{
			public static readonly Guid MobilePhone = new Guid("D4A2DC80-30CA-DF11-9B2A-001D60E938C6");
			public static readonly Guid PrimaryPhone = new Guid("6A3FB10C-67CC-DF11-9B2A-001D60E938C6");
			public static readonly Guid HomePhone = new Guid("0DA6A26B-D7BC-DF11-B00F-001D60E938C6");
			public static readonly Guid Email = new Guid("EE1C85C3-CFCB-DF11-9B2A-001D60E938C6");
		}

		public static class Country
		{
			public static readonly Guid Russia = new Guid("A570B005-E8BB-DF11-B00F-001D60E938C6");
			public static readonly Guid USA = new Guid("E0BE1264-F36B-1410-FA98-00155D043204");
		}

		public static class SysAdminUnitType
		{
			public static readonly int Organization = 0;
			public static readonly int Division = 1;
			public static readonly int Manager = 2;
			public static readonly int Team = 3;
			public static readonly int User = 4;
			public static readonly int PortalUser = 5;
			public static readonly int FunctionalRole = 6;
		}
		
	}

}