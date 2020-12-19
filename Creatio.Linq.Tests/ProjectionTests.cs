using System;
using System.Configuration;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Norbit.TRS;

namespace Creatio.Linq.Tests
{
	[TestClass]
	public class ProjectionTests: CreatioTestBase
	{
		[TestInitialize]
		public void GenerateTestData()
		{
			TestDataGenerator.GenerateAccounts(UserConnection);
		}

		[TestMethod]
		public void ShouldSelectAllColumnsWithoutProjection()
		{
			var account = UserConnection
				.QuerySchema("Account")
				.First(item => 
					item.Column<string>("Notes") == "$UnitTest$"
					&& item.Column<Guid>("Type.Id") == Consts.Account.Type.Customer
					&& item.Column<Guid>("AccountCategory") == Consts.Account.Category.A
				);
			
			Assert.IsNotNull(account);
			Assert.AreNotEqual(Guid.Empty, account.PrimaryColumnValue);
			Assert.AreNotEqual(Guid.Empty, account.GetTypedColumnValue<Guid>("Id"));
			
			// all fields are selected, just check some of them
			Assert.AreEqual(Consts.Account.Type.Customer, account.GetTypedColumnValue<Guid>("TypeId"));
			Assert.AreEqual(Consts.Account.Category.A, account.GetTypedColumnValue<Guid>("AccountCategoryId"));
			Assert.AreEqual(Consts.Country.Russia, account.GetTypedColumnValue<Guid>("CountryId"));
		}

		[TestMethod]
		public void ShouldCreateAnonymousClassWithProjectedFields()
		{
			// in this test we first filter records and then create
			// anonymous class for result collection
			
			var account = UserConnection
				.QuerySchema("Account")
				.Where(item =>
					item.Column<string>("Notes") == "$UnitTest$"
					&& item.Column<Guid>("Type.Id") == Consts.Account.Type.Customer
					&& item.Column<Guid>("AccountCategory") == Consts.Account.Category.A
				)
				.Select(item => new
				{
					Name = item.Column<string>("Name"),
					TypeId = item.Column<Guid>("Type"),								// if lookup column is defined its identifier will be returned
					TypeName = item.Column<string>("Type.Name"),					// or you may use explicit columns
					AccountCategoryId = item.Column<Guid>("AccountCategory.Id"),	// for identifiers too
					CountryName = item.Column<string>("Country.Name")
				})
				.First();
			
			Assert.IsNotNull(account);
			Assert.AreEqual(Consts.Account.Type.Customer, account.TypeId);
			Assert.AreEqual(Consts.Account.Category.A, account.AccountCategoryId);
			Assert.IsNotNull(account.TypeName);
			Assert.IsNotNull(account.CountryName);
		}

		[TestMethod]
		public void ShouldFilterByAnonymousClassFields()
		{
			// same as above but first select required columns to anonymous
			// class and then apply filtering to its columns.
			// less flexible because only projected columns can be used for filtering

			var account = UserConnection
				.QuerySchema("Account")
				.Select(item => new
				{
					Notes = item.Column<string>("Notes"),
					TypeId = item.Column<Guid>("Type"),
					TypeName = item.Column<string>("Type.Name"),
					AccountCategoryId = item.Column<Guid>("AccountCategory.Id"),
					CountryName = item.Column<string>("Country.Name")
				})
				.First(item =>
					item.Notes == "$UnitTest$"
					&& item.TypeId == Consts.Account.Type.Customer
					&& item.AccountCategoryId == Consts.Account.Category.A
				);

			Assert.IsNotNull(account);
			Assert.AreEqual(Consts.Account.Type.Customer, account.TypeId);
			Assert.AreEqual(Consts.Account.Category.A, account.AccountCategoryId);
			Assert.IsNotNull(account.TypeName);
			Assert.IsNotNull(account.CountryName);
		}

		[TestMethod]
		public void ShouldAddLinkedEntityFields()
		{
			var account = UserConnection
				.QuerySchema("Account")
				.Where(item => item.Column<string>("Notes") == "$UnitTest$")
				.Select(item => new
				{
					Name = item.Column<string>("Name"),
					CommunicationTypeValue = item.Column<string>("[AccountCommunication:Account:Id].Number"),
					CommunicationTypeId = item.Column<Guid>("[AccountCommunication:Account:Id].CommunicationType")
				})
				.Where(item => 
					item.CommunicationTypeValue != null
					&& item.CommunicationTypeId == Consts.CommunicationType.PrimaryPhone
				)
				.ToArray();
			
			Assert.AreEqual(1, account.Length);
		}
	}
}