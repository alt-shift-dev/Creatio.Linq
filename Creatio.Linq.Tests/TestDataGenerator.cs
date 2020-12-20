using System;
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
			_ = userConnection ?? throw new ArgumentNullException(nameof(userConnection));
			
			var customerA = userConnection
				.CreateEntity("Account", new
				{
					Name = "CustomerA",
					TypeId = Consts.Account.Type.Customer,
					AccountCategoryId = Consts.Account.Category.A,
					CountryId = Consts.Country.Russia,
					// this column will be used to distinguish items generated for unit test
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

		public static void GenerateActivities(UserConnection userConnection)
		{
			_ = userConnection ?? throw new ArgumentNullException(nameof(userConnection));
			
			// duration: 1 day, start today
			userConnection.CreateEntity("Activity", new
			{
				Title = $"Test_Email",
				PriorityId = Consts.Activity.Priority.High,
				TypeId = Consts.Activity.Type.Email,
				StatusId = Consts.Activity.Status.NotStarted,
				StartDate = DateTime.Today,
				DueDate = DateTime.Today + TimeSpan.FromDays(1),
				// this column will be used to distinguish items generated for unit test
				DetailedResult = "$UnitTest$",	
			});

			// duration: 2 days, start today
			userConnection.CreateEntity("Activity", new
			{
				Title = $"Test_Task",
				PriorityId = Consts.Activity.Priority.Low,
				TypeId = Consts.Activity.Type.Task,
				StatusId = Consts.Activity.Status.Cancelled,
				StartDate = DateTime.Today,
				DueDate = DateTime.Today + TimeSpan.FromDays(2),
				DetailedResult = "$UnitTest$",
			});

			// duration: 1 day, start yesterday
			userConnection.CreateEntity("Activity", new
			{
				Title = $"Test_Call",
				PriorityId = Consts.Activity.Priority.Medium,
				TypeId = Consts.Activity.Type.Call,
				StatusId = Consts.Activity.Status.Completed,
				StartDate = DateTime.Today - TimeSpan.FromDays(1),
				DueDate = DateTime.Today,
				DetailedResult = "$UnitTest$",
			});
		}
	}
}