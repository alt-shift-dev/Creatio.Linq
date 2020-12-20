using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Norbit.TRS;
using Terrasoft.Core.DB;

namespace Creatio.Linq.Tests
{
	[TestClass]
	public class AggregationTests: CreatioTestBase
	{
		[TestInitialize]
		public void GenerateTestData()
		{
			TestDataGenerator.GenerateAccounts(UserConnection);
			TestDataGenerator.GenerateActivities(UserConnection);
		}

		[TestMethod]
		public void ShouldCalculateCountWithoutColumnName()
		{
			var accountCount = UserConnection
				.QuerySchema("Account", LogOptions.ToTracePerformanceOnly)
				.Where(item => item.Column<string>("Notes") == "$UnitTest$")
				.Count();
			
			Assert.AreEqual(3, accountCount);
		}

		[TestMethod]
		public void ShouldCalculateCountWithInnerFilter()
		{
			var accountCount = UserConnection
				.QuerySchema("Account", LogOptions.ToTracePerformanceOnly)
				.Count(item => item.Column<string>("Notes") == "$UnitTest$");

			Assert.AreEqual(3, accountCount);
		}

		[TestMethod]
		public void ShouldCalculateMinDuration()
		{
			var minDuration = UserConnection
				.QuerySchema("Activity", LogOptions.ToTracePerformanceOnly)
				.Where(item => item.Column<string>("DetailedResult") == "$UnitTest$")
				.Min(item => item.Column<int>("DurationInMinutes"));
			
			Assert.AreEqual(60*24, minDuration);
		}

		[TestMethod]
		public void ShouldCalculateMaxDuration()
		{
			var maxDuration = UserConnection
				.QuerySchema("Activity", LogOptions.ToTracePerformanceOnly)
				.Where(item => item.Column<string>("DetailedResult") == "$UnitTest$")
				.Max(item => item.Column<int>("DurationInMinutes"));

			Assert.AreEqual(60 * 24*2, maxDuration);
		}

		[TestMethod]
		public void ShouldCalculateAvgDuration()
		{
			var maxDuration = UserConnection
				.QuerySchema("Activity", LogOptions.ToTracePerformanceOnly)
				.Where(item => item.Column<string>("DetailedResult") == "$UnitTest$")
				.Average(item => item.Column<decimal>("DurationInMinutes"));

			Assert.AreEqual(60 * 24 * (1+2+1)/3, maxDuration);
		}

		[TestMethod]
		public void ShouldApplyAggregationWithGrouping()
		{
			var durations = UserConnection
				.QuerySchema("Activity", LogOptions.ToTracePerformanceOnly)
				.Where(item => item.Column<string>("DetailedResult") == "$UnitTest$")
				.GroupBy(item => item.Column<DateTime>("StartDate"))
				.Select(group => new
				{
					StartDate = group.Key,
					Count = group.Count(),
					Avg = group.Average(item => item.Column<int>("DurationInMinutes")),
					Min = group.Min(item => item.Column<int>("DurationInMinutes")),
					Max = group.Max(item => item.Column<int>("DurationInMinutes"))
				})
				.OrderBy(item => item.StartDate)
				.ThenByDescending(item => item.Min)
				.ToArray();
			
			Assert.AreEqual(2, durations.Length);

			var first = durations.First();
			var last = durations.Last();

			var today = DateTime.Today;
			var allMyTroublesSeemedSoFarAway = today - TimeSpan.FromDays(1);
			
			Assert.AreEqual(allMyTroublesSeemedSoFarAway, first.StartDate);
			Assert.AreEqual(today, last.StartDate);

			Assert.AreEqual(1, first.Count);
			Assert.AreEqual(24 * 60, first.Min);
			Assert.AreEqual(24 * 60, first.Max);
			Assert.AreEqual(24 * 60, first.Avg);

			Assert.AreEqual(2, last.Count);
			Assert.AreEqual(24 * 60, last.Min);
			Assert.AreEqual(24 * 60 * 2, last.Max);
			Assert.AreEqual(24 * 60 * 3 / 2, last.Avg);
		}
	}
}