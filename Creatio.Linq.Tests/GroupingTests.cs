using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Norbit.TRS;
using Terrasoft.Common;
using Terrasoft.Core.Entities;

namespace Creatio.Linq.Tests
{
	[TestClass]
	public class GroupingTests: CreatioTestBase
	{
		[TestInitialize]
		public void BeforeTest()
		{
			TestDataGenerator.GenerateAccounts(UserConnection);
		}

		[TestMethod]
		public void ShouldGroupBySingleField()
		{
			var accountGroups = UserConnection.QuerySchema("Account")
				.Where(item => item.Column<string>("Notes") == "$UnitTest$")
				.GroupBy(item => item.Column<Guid>("AccountCategory"))
				.Select(group => new
				{
					AccountCategory = group.Key,
					Count = group.Count()
				})
				.ToArray();

			Assert.AreEqual(2, accountGroups.Length);
			Assert.AreEqual(2, accountGroups.Single(group => group.AccountCategory == Consts.Account.Category.A).Count);
			Assert.AreEqual(1, accountGroups.Single(group => group.AccountCategory == Consts.Account.Category.B).Count);
		}

		[TestMethod]
		public void ShouldGroupBySingleColumnInAnonymousClass()
		{
			var accountGroups = UserConnection.QuerySchema("Account")
				.Where(item => item.Column<string>("Notes") == "$UnitTest$")
				.GroupBy(item => new { AccountCategoryId = item.Column<Guid>("AccountCategory")})
				.Select(group => new
				{
					AccountCategory = group.Key.AccountCategoryId,
					Count = group.Count()
				})
				.ToArray();

			Assert.AreEqual(2, accountGroups.Length);
			Assert.AreEqual(2, accountGroups.Single(group => group.AccountCategory == Consts.Account.Category.A).Count);
			Assert.AreEqual(1, accountGroups.Single(group => group.AccountCategory == Consts.Account.Category.B).Count);
		}

		[TestMethod]
		public void ShouldGroupByMultipleColumnsInAnonymousClass()
		{
			var accountGroups = UserConnection.QuerySchema("Account")
				.Where(item => item.Column<string>("Notes") == "$UnitTest$")
				.GroupBy(item => new
				{
					AccountCategoryId = item.Column<Guid>("AccountCategory.Id"),
					CountryId = item.Column<Guid>("Country.Id")
				})
				.Select(group => new
				{
					AccountCategory = group.Key.AccountCategoryId,
					CountryId = group.Key.CountryId,
					Count = group.Count()
				})
				.ToArray();

			Assert.AreEqual(2, accountGroups.Length);
			Assert.AreEqual(2, accountGroups.Single(group => group.AccountCategory == Consts.Account.Category.A).Count);
			Assert.AreEqual(1, accountGroups.Single(group => group.AccountCategory == Consts.Account.Category.B).Count);
			Assert.IsTrue(accountGroups.All(item => item.CountryId == Consts.Country.Russia));
		}

		[TestMethod]
		public void ShouldGroupByMultipleColumnsInObjectArray()
		{
			var accountGroups = UserConnection.QuerySchema("Account")
				.Where(item => item.Column<string>("Notes") == "$UnitTest$")
				.GroupBy(item => new object[]
				{
					item.Column<Guid>("AccountCategory.Id"),
					item.Column<Guid>("Country.Id")
				})
				.Select(group => new
				{
					AccountCategory = (Guid)group.Key[0],
					CountryId = (Guid)group.Key[1],
					Count = group.Count()
				})
				.ToArray();

			Assert.AreEqual(2, accountGroups.Length);
			Assert.AreEqual(2, accountGroups.Single(group => group.AccountCategory == Consts.Account.Category.A).Count);
			Assert.AreEqual(1, accountGroups.Single(group => group.AccountCategory == Consts.Account.Category.B).Count);
			Assert.IsTrue(accountGroups.All(item => item.CountryId == Consts.Country.Russia));
		}

		[TestMethod]
		public void ShouldAggregateMultipleColumns()
		{
			var accountGroups = UserConnection.QuerySchema("Account")
				.Where(item => item.Column<string>("Notes") == "$UnitTest$")
				.GroupBy(item => item.Column<Guid>("AccountCategory.Id"))
				.Select(group => new
				{
					AccountCategory = group.Key,
					Count = group.Count(),
					Min = group.Min(item => item.Column<DateTime>("CreatedOn"))
				})
				.ToArray();

			Assert.AreEqual(2, accountGroups.Length);
			Assert.IsTrue(accountGroups.First().Min < DateTime.Now);
		}

	}
}