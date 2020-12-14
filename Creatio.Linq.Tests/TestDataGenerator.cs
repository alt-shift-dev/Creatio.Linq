using Terrasoft.Core;

namespace Creatio.Linq.Tests
{
	/// <summary>
	/// Generates data for tests.
	/// </summary>
	public class TestDataGenerator
	{
		public static void GenerateAccounts(UserConnection userConnection)
		{
			var customerA = userConnection
				.CreateEntity("Account", new
				{
					Name = "CustomerA",
					TypeId = Consts.Account.Type.Customer,
					AccountCategoryId = Consts.Account.Category.A,
					CountryId = Consts.Country.Russia,
					Notes = "$UnitTest$"
				});

			var customerAPhone = userConnection
				.CreateEntity("AccountCommunication", new
				{
					AccountId = customerA.PrimaryColumnValue,
					CommunicationTypeId = Consts.CommunicationType.PrimaryPhone,
					Number = "79001234567"
				});

			var customerB = userConnection
				.CreateEntity("Account", new
				{
					Name = "CustomerB",
					TypeId = Consts.Account.Type.Customer,
					AccountCategoryId = Consts.Account.Category.B,
					CountryId = Consts.Country.Russia,
					Notes = "$UnitTest$"
				});

			var customerBEmail = userConnection
				.CreateEntity("AccountCommunication", new
				{
					AccountId = customerB.PrimaryColumnValue,
					CommunicationTypeId = Consts.CommunicationType.Email,
					Number = "customer_b@example.com"
				});


			var partnerB = userConnection
				.CreateEntity("Account", new
				{
					Name = "PartnerB",
					TypeId = Consts.Account.Type.Partner,
					AccountCategoryId = Consts.Account.Category.A,
					CountryId = Consts.Country.Russia,
					Notes = "$UnitTest$"
				});
		}
	}
}