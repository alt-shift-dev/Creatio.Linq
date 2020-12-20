using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Norbit.TRS;

namespace Creatio.Linq.Tests
{
	[TestClass]
	public class FilteringTests: CreatioTestBase
	{
		[TestInitialize]
		public void GenerateTestData()
		{
			TestDataGenerator.GenerateAccounts(UserConnection);
			TestDataGenerator.GenerateActivities(UserConnection);
		}

		[TestMethod]
		public void ShouldApplyPaging()
		{
			var skip = 0;

			while (true)
			{
				var activities = UserConnection
					.QuerySchema("Activity", LogOptions.ToTracePerformanceOnly)
					.Where(item => item.Column<string>("DetailedResult") == "$UnitTest$")
					.Skip(skip)
					.Take(1)
					.ToArray();

				Assert.IsTrue(activities.Length <= 1);
				if (activities.Length == 0)
					break;

				skip++;
			}
			
			// 3 activities total
			Assert.AreEqual(3, skip);
		}

		[TestMethod]
		public void ShouldApplySimpleEqualsFilter()
		{
			var accounts = UserConnection
				.QuerySchema("Account", LogOptions.ToTracePerformanceOnly)
				.Where(item => item.Column<string>("Notes") == "$UnitTest$")
				.ToArray();
			
			Assert.IsNotNull(accounts);
			Assert.IsTrue(accounts.Length > 0);
		}

		[TestMethod]
		public void ShouldCombineFiltersWithLogicalOperations()
		{
			var accounts = UserConnection
				.QuerySchema("Account", LogOptions.ToTracePerformanceOnly)
				.Where(item =>
					item.Column<string>("Notes") == "$UnitTest$"
					&& item.Column<string>("Name") == "CustomerA"
				)
				.ToArray();
			
			Assert.AreEqual(1, accounts.Length);
		}
		

		[TestMethod]
		public void ShouldApplyNullNotNullFilter()
		{
			var accounts = UserConnection
				.QuerySchema("Account", LogOptions.ToTracePerformanceOnly)
				.Where(item =>
					item.Column<string>("Notes") == "$UnitTest$"
					&& item.Column<Guid>("Type") != null)
				.ToArray();

			var empty = UserConnection
				.QuerySchema("Account")
				.Where(item =>
					item.Column<string>("Notes") == "$UnitTest$"
					&& item.Column<Guid>("Type") == null)
				.ToArray();
			
			Assert.AreEqual(3, accounts.Length);
			Assert.AreEqual(0, empty.Length);
		}
		
		[TestMethod]
		public void ShouldDealWithAlternativelyGiftedProgrammers()
		{
			// comparison operands are reversed
			var accounts = UserConnection
				.QuerySchema("Account", LogOptions.ToTracePerformanceOnly)
				.Where(item =>
						"$UnitTest$" == item.Column<string>("Notes")
						&& null != item.Column<Guid>("Type") )
				.ToArray();
			
			Assert.AreEqual(3, accounts.Length);
		}

		[TestMethod]
		public void ShouldApplyStartsWithEndsWithFilter()
		{
			var accounts = UserConnection
				.QuerySchema("Account", LogOptions.ToTracePerformanceOnly)
				.Where(item =>
					item.Column<string>("Notes") == "$UnitTest$"
					&& item.Column<string>("Name").StartsWith("Partner"))
				.ToArray();

			var customerA = UserConnection
				.QuerySchema("Account", LogOptions.ToTracePerformanceOnly)
				.Where(item =>
					item.Column<string>("Notes") == "$UnitTest$"
					&& item.Column<string>("Name").EndsWith("A"))
				.ToArray();
			
			Assert.AreNotEqual(3, accounts.Length);
			Assert.AreEqual(1, customerA.Length);
			Assert.AreEqual("CustomerA", customerA.First().GetTypedColumnValue<string>("Name"));
		}

		[TestMethod]
		public void ShouldFilterByLinkedEntities()
		{
			var accounts = UserConnection
				.QuerySchema("Account", LogOptions.ToTracePerformanceOnly)
				.Where(item =>
					item.Column<string>("Notes") == "$UnitTest$"
					&& item.Column<string>("[AccountCommunication:Account:Id].Number") == "79001234567")
				.ToArray();
			
			Assert.AreEqual(1, accounts.Length);
			Assert.AreEqual("CustomerA", accounts.First().GetTypedColumnValue<string>("Name"));
		}


		[TestMethod]
		public void ShouldFilterByBoolFieldsWithoutComparison()
		{
			var activeUsersCount = UserConnection
				.QuerySchema("SysAdminUnit", LogOptions.ToTracePerformanceOnly)
				.Count(item => item.Column<bool>("Active"));
			
			Assert.IsTrue(activeUsersCount > 0);
		}

		[TestMethod]
		public void ShouldFilterByValueSet()
		{
			var userTypes = new[]
			{
				Consts.SysAdminUnitType.User,
				Consts.SysAdminUnitType.PortalUser
			};

			// pre-populated set
			var users = UserConnection
				.QuerySchema("SysAdminUnit", LogOptions.ToTracePerformanceOnly)
				.Where(item =>
					userTypes.Contains(item.Column<int>("SysAdminUnitTypeValue"))
					&& item.Column<bool>("Active"))
				.ToArray();

			// inline set
			var roles = UserConnection
				.QuerySchema("SysAdminUnit")
				.Where(item => new[]
					{
						Consts.SysAdminUnitType.Organization,
						Consts.SysAdminUnitType.FunctionalRole
					}.Contains(item.Column<int>("SysAdminUnitTypeValue")))
				.ToArray();
			
			Assert.IsTrue(users.Length > 0);
			Assert.IsTrue(roles.Length > 0);
		}

		[TestMethod]
		public void ShouldAllowSimpleExpressionsInFilters()
		{
			var activities = UserConnection
				.QuerySchema("Activity", LogOptions.ToTracePerformanceOnly)
				.Where(item => 
					item.Column<DateTime>("StartDate") > DateTime.Today - TimeSpan.FromDays(5)
					&& item.Column<Guid>("Status") == Consts.Activity.Status.Completed
				)
				.ToArray();
			
			Assert.AreEqual(1, activities.Length);
		}

		[TestMethod]
		public void ShouldInvertFilter()
		{
			var activities = UserConnection
				.QuerySchema("Activity", LogOptions.ToTracePerformanceOnly)
				.Where(item => item.Column<string>("DetailedResult") == "$UnitTest$")
				.Where(item => !(
					item.Column<Guid>("Type") == Consts.Activity.Type.Email
					|| item.Column<Guid>("Type") == Consts.Activity.Type.Call)
				)
				.Select(item => item.Column<Guid>("Type"))
				.ToArray();
			
			Assert.AreEqual(1, activities.Length);
			Assert.AreEqual(Consts.Activity.Type.Task, activities.First());
		}
	}
}