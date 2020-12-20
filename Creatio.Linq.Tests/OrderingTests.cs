using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Norbit.TRS;

namespace Creatio.Linq.Tests
{
	[TestClass]
	public class OrderingTests: CreatioTestBase
	{
		[TestInitialize]
		public void GenerateTestData()
		{
			TestDataGenerator.GenerateActivities(UserConnection);
		}

		[TestMethod]
		public void ShouldPerformSimpleOrdering()
		{
			var activities = UserConnection
				.QuerySchema("Activity", LogOptions.ToTracePerformanceOnly)
				.Where(item => item.Column<string>("DetailedResult") == "$UnitTest$")
				.OrderBy(item => item.Column<DateTime>("StartDate"))
				.Select(item => new
				{
					Title = item.Column<string>("Title"),
					StartDate = item.Column<DateTime>("StartDate"),
					DueDate = item.Column<DateTime>("DueDate")
				})
				.ToArray();

			Assert.AreEqual(3, activities.Length);

			activities.Aggregate((a, e) =>
			{
				if (a.StartDate > e.StartDate) throw new InvalidOperationException("StartDate");
				return e;
			});
		}

		[TestMethod]
		public void ShouldPerformDescendingOrdering()
		{
			var activities = UserConnection
				.QuerySchema("Activity", LogOptions.ToTracePerformanceOnly)
				.Where(item => item.Column<string>("DetailedResult") == "$UnitTest$")
				.OrderByDescending(item => item.Column<DateTime>("StartDate"))
				.Select(item => new
				{
					Title = item.Column<string>("Title"),
					StartDate = item.Column<DateTime>("StartDate"),
					DueDate = item.Column<DateTime>("DueDate")
				})
				.ToArray();

			Assert.AreEqual(3, activities.Length);

			activities.Aggregate((a, e) =>
			{
				if (a.StartDate < e.StartDate) throw new InvalidOperationException("StartDate");
				return e;
			});
		}

		[TestMethod]
		public void ShouldPerformComplexOrdering()
		{
			var activities = UserConnection
				.QuerySchema("Activity", LogOptions.ToTracePerformanceOnly)
				.Where(item => item.Column<string>("DetailedResult") == "$UnitTest$")
				.OrderBy(item => item.Column<DateTime>("StartDate"))
				.ThenBy(item => item.Column<int>("DurationInMinutes"))
				.Select(item => new
				{
					Title = item.Column<string>("Title"),
					StartDate = item.Column<DateTime>("StartDate"),
					DueDate = item.Column<DateTime>("DueDate"),
					TypeName = item.Column<string>("Type.Name"),
					Duration = item.Column<int>("DurationInMinutes")
				})
				.ToArray();

			Assert.AreEqual(3, activities.Length);

			var first = activities[0];
			var second = activities[1];
			var third = activities[2];

			Assert.IsTrue(first.StartDate < second.StartDate);
			Assert.IsTrue(second.StartDate == third.StartDate);
			Assert.IsTrue(second.Duration < third.Duration);
		}

		[TestMethod]
		public void ShouldOrderByColumnAlias()
		{
			// same as one of the previous one, but .OrderBy() is performed after .Select()
			// and uses column alias.
			var activities = UserConnection
				.QuerySchema("Activity", LogOptions.ToTracePerformanceOnly)
				.Where(item => item.Column<string>("DetailedResult") == "$UnitTest$")
				.Select(item => new
				{
					Title = item.Column<string>("Title"),
					StartDate = item.Column<DateTime>("StartDate"),
					DueDate = item.Column<DateTime>("DueDate")
				})
				.OrderBy(item => item.StartDate)
				.ToArray();

			Assert.AreEqual(3, activities.Length);

			activities.Aggregate((a, e) =>
			{
				if (a.StartDate > e.StartDate) throw new InvalidOperationException("StartDate");
				return e;
			});
		}

		[TestMethod]
		public void ShouldOrderByAggregatedFields()
		{
			var mostActivitiesOnDate = UserConnection
				.QuerySchema("Activity", LogOptions.ToTrace)
				.GroupBy(item => item.Column<DateTime>("StartDate"))
				.Select(group => new
				{
					StartDate = group.Key,
					Count = group.Count(),
				})
				.OrderByDescending(result => result.Count)
				//.First();	// there is a bug in ESQ generation currently, it generates incorrect filtering with aggregated columns if paging enabled
				.ToArray()	// execute query
				.First();	// use linq-to-objects to get first item.
			
			Assert.IsNotNull(mostActivitiesOnDate);
			
		}

	}
}