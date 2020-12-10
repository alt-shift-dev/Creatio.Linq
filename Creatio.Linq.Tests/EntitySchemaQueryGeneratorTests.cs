using System;
using System.Diagnostics;
using System.Linq;
using Creatio.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Norbit.TRS;
using Remotion.Linq.Parsing.Structure;
using Terrasoft.Common;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using Xunit;
using Xunit.Abstractions;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Creatio.Linq.Tests
{
	[TestClass]
	public class EntitySchemaQueryGeneratorTests: CreatioTestBase
	{
		[TestMethod]
		public void BigUglyEsq()
		{
			var colName = "CreatedOn";

			var contacts = UserConnection
				.QuerySchema("SysAdminUnit")
				.Where(item => (item.Column<string>("[Contact:Id:ContactId].Name").StartsWith("Super") 
				               && item.Column<DateTime>(colName) > DateTime.Now - TimeSpan.FromDays(4)
				               && item.Column<bool>("Active"))
						|| item.Column<string>("[Contact:Id:ContactId].Surname") == "Ivanov")
				.Where(item => !(item.Column<Guid>("Id") == Guid.Empty))
				.Where(item => item.Column<Guid>("CreatedBy") != null)
				.Where(item => item.Column<bool>("Active"))
				.Where(item => new[]{1,2,3}.Contains(item.Column<int>("SysAdminUnitTypeValue")))
				.OrderBy(item => item.Column<DateTime>("CreatedOn"))
				.ThenByDescending(item => item.Column<DateTime>("ModifiedOn"))
				.Select(item => new
				{
					CreatedOn = item.Column<DateTime>("CreatedOn"),
					ModifiedOn = item.Column<DateTime>("ModifiedOn"),
					Name = item.Column<string>("Name")
				})
				.ToArray();

			Assert.IsNotNull(contacts);
			Assert.AreEqual(1, contacts.Length);
		}

		[TestMethod]
		public void ShouldEvaluateInClause()
		{
			var validUnitTypes = new[] {1, 2, 3};

			var units = UserConnection
				.QuerySchema("SysAdminUnit")
				.Where(item => validUnitTypes.Contains(item.Column<int>("SysAdminUnitTypeValue")))
				.Where(item => new[] { 5, 4, 3 }.Contains(item.Column<int>("LoginAttemptCount")))
				.ToArray();

			Assert.IsNotNull(units);
		}
		
		[TestMethod]
		public void ShouldAddOrder()
		{
			var contacts = UserConnection
				.QuerySchema("Contact")
				.OrderBy(item => item.Column<DateTime>("CreatedOn"))
				.ThenBy(item => item.Column<DateTime>("ModifiedOn"))
				.ToArray();

			Assert.IsNotNull(contacts);
		}

		[TestMethod]
		public void ShouldLimitRecords()
		{
			var contacts = UserConnection
				.QuerySchema("Contact")
				.Take(1)
				.Skip(1)
				.ToArray();

			Assert.IsNotNull(contacts);
			Assert.AreEqual(1, contacts.Length);
		}

		[TestMethod]
		public void ShouldApplyProjection()
		{
			var contacts = UserConnection
				.QuerySchema("Contact")
				.Select(item => new
				{
					Name = item.Column<string>("Name"),
					CreatedOn = item.Column<DateTime>("CreatedOn")
				})
				.ToArray();

			Assert.IsNotNull(contacts);
		}

		[TestMethod]
		public void ShouldApplySimpleFilter()
		{
			var contacts = UserConnection
				.QuerySchema("Contact")
				.Where(item => item.Column<string>("Name").StartsWith("Super"))
				.Select(item => new
				{
					Id = item.Column<Guid>("Id"),
					Name = item.Column<string>("Name")
				})
				.ToArray();

			Assert.IsNotNull(contacts);
			Assert.AreEqual(1, contacts.Length);
			Assert.AreEqual("Supervisor", contacts[0].Name);
		}

		[TestMethod]
		public void ShouldApplyGrouping()
		{
			var contacts = UserConnection
				.QuerySchema("Contact")
				.GroupBy(item => new
				{
					CreatedBy = item.Column<Guid>("CreatedBy"), 
					SysAdminUnitTypeValue = item.Column<int>("[SysAdminUnit:ContactId:Id].SysAdminUnitTypeValue")
				})
				.Select(group => new
				{
					CreatedBy = group.Key.CreatedBy,
					SysAdminUnitTypeValue = group.Key.SysAdminUnitTypeValue,
					Count = group.Min(item => item.Column<DateTime>("CreatedOn"))
				})
				.ToArray();
		}

		[TestMethod]
		public void ShouldApplyAggregation()
		{
			var contacts = UserConnection
				.QuerySchema("Contact")
				.GroupBy(item => new object[] {
					item.Column<Guid>("CreatedBy"),
					item.Column<int>("[SysAdminUnit:ContactId:Id].SysAdminUnitTypeValue")
				})
				.Select(group => new
				{
					CreatedBy = group.Key[0],
					SysAdminUnitTypeValue = group.Key[1],
					Count = group.Min(item => item.Column<DateTime>("CreatedOn"))
				})
				.ToArray();
		}

		[TestMethod]
		public void TestParameterAndConst()
		{
			new Update(UserConnection, "Contact")
				.Set("FirstName", Column.Parameter("Don't mind"))
				.Set("LastName", Column.Const("Isn't it nice?"))
				.Where("Id").IsEqual(Column.Const(Guid.Empty))
				.Execute(UserConnection.EnsureDBConnection());
		}
	}
}
