# Creatio.Linq

Project adds a bit of Linq to your ESQ.  
[Re-linq](https://github.com/re-motion/Relinq) library is used to parse Linq query trees.  

## Creatio versions support
Tested against Terrasoft Creatio 7.15.2, should work fine with more recent versions.  
If you need to use it with older versions you'll have to build library by your own.  

Library is compiled for .NET Standard, so you should have no problems using it with Creatio running on .NET Core.

## Installation
There are two options here:  
1. If you prefer to write serverside code in VS, use nuget as usual, then add Creatio.Linq.dll
and Remotion.Linq.dll to your package via Asseblies tab.
2. Otherwise just add CreatioLinq package to your Creatio configuration and start using Linq 
for ESQ queries (this is also a good option for multiple dev teams - you won't have dll duplicates
in various packages).

### Installing via nuget
Just as usual:
```
Install-Package Creatio.Linq
```

### Installing Creatio package CreatioLinq
Go to [Releases](https://github.com/alt-shift-dev/Creatio.Linq/releases) page and grab latest version.

## Usage
For using Linq with ESQ you have to add Creatio.Linq and System.Linq namespaces and use extension method QuerySchema,
after that you can use all standard Linq methods like Select, Where, OrderBy, etc:
```csharp
using Creatio.Linq;
using System.Linq;
///...

public int GetContactsCount()
{
    return UserConnection
        .QuerySchema("Contact")
        .Count();
}

```

### Accessing entity fields
Since ESQ-queries work with untyped objects, [DynamicEntity](https://github.com/alt-shift-dev/Creatio.Linq/blob/main/Creatio.Linq/DynamicEntity.cs)
class is initally forwarded to Select/Where/GroupBy/OrderBy methods.  
DynamicEntity is derived from Entity class with only added method ```Column<TValue>("Column.Path.Expression")```, 
which should be used to access entity columns.  
Example:
```csharp
var supervisor = UserConnection
    .QuerySchema("Contact")
    .Where(item => item.Column<string>("Name") == "Supervisor")
    .Single();
```

You are not limited to only entity columns, use any column path expressions supported by ESQ:
```csharp
var supervisorContact = UserConnection
    .QuerySchema("Contact")
    .Where(item => item.Column<string>("[ContactCommunication:Contact:Id].Number") == "79001234567")
    .ToArray();
```

## Executing query
An important notice for those who doesn't understand well how Linq works. This applies to any Linq expression, not only this provider.  
So, Linq expressions are lazy, just as you are. This means that Linq expression will not be executed until there is an attempt to access 
it's result.  
This can be done in two ways:
1. Call aggregate function after expression, if you need scalar result.
2. Call method ToArray()/ToList()/First()/Single(), if you need objects.

Example:
```csharp
var activityQueryable = UserConnection
    .QuerySchema("Activity")
    .Where(item => item.Column("EndDate") > DateTime.Now);

// at this step no requests are sent to DB, since there was no attempt to access results.
// activityQueryable is just some IQueryable<DymaicEntity> where you can add extra filters.

// below are the ways how to send requests to database

// for example, one may count quantity
var pendingActivities = activityQueryable.Count();                  // gets aggregation result

// or take last 10 created items
var latestTen = activityQueryable
    .OrderByDescending(item => item.Column<DateTime>("CreatedOn"))  // request still not sent
    .Take(10)                                                       // same here
    .ToArray();                                                     // at this point query is executed in DB
```

Now you are warned.

## Projections
Projections are used when you only need a subset of entity columns, Linq mehtod Select should be used:

```csharp
var activeUsers = UserConnection
    .QuerySchema("SysAdminUnit")
    .Where(item => item.Column<bool>("Active") 
        && item.Column<Guid>("ContactId") != null)
    .Select(item => new 
    {
        Id = item.Column<Guid>("Id")
        Name = item.Column<string>("Name"), 
        ContactId = item.Column<Guid>("Contact"),
        ContactName = item.Column<string>("Contact.Name"),
    })
    .ToArray(); // hope you remember that ToArray() should be called to execute query?

// activeUsers is a strongly typed anonymous class, its properties may be  
// accessed conveniently:
Assert.IsNotNull(activeUsers.First().Name)
```

In the example above you may notice one peculiarity: by default, the identifier column is returned for lookup fields,
and it's name is assumed to be "Id". Unfortunately, no quick ways to determine the name of identifier columns were found
(we don't want transforming of Linq queries to ESQ to be time-consuming, right?), so if you are experiencing problems
just access lookup identifier explicitly: ```item.Column<Guid>("Contact.Id")``` or whatever you id column is.  

Anonymous classes are not mandatory if you only need one entity column:
```csharp
var activeUserIds = UserConnection
    .QuerySchema("SysAdminUnit")
    .Where(item => item.Column<bool>("Active") 
        && item.Column<Guid>("ContactId") != null)
    .Select(item => item.Column<Guid>("Id"))
    .ToArray();

// activeUserIds - simply a Guid[] with identifiers.
```

What if you don't use Select()? In this case ESQ method AddAllSchemaColumns() will be called
prior to executing query, and you will have to access results like regular Entity fields via
GetColumnValue()/GetTypedColumnValue(). No example, I'm lazy.

## Filtering
More examples can be seen in tests, say, [this](https://github.com/alt-shift-dev/Creatio.Linq/blob/main/Creatio.Linq.Tests/FilteringTests.cs) file.  
Simple examples were shown above, let's repeat:

#### Null/NotNull filter
```csharp
// select * from Contact where AccountId is null
UserConnection
    .QuerySchema("Contact")
    .Where(item => item.Column<Guid>("Account") == null)
    .ToArray();

// select * from Contact where AccountId is not null
UserConnection
    .QuerySchema("Contact")
    .Where(item => item.Column<Guid>("Account") != null)
    .ToArray();

// select * from Contact where not AccountId is null
UserConnection
    .QuerySchema("Contact")
    .Where(item => !(item.Column<Guid>("Account") == null))
    .ToArray();
```

#### LIKE filter for strings:
```csharp
// select * from SysAdminUnit where Name like 'Super%'
UserConnection
    .QuerySchema("SysAdminUnit")
    .Where(item => item.Column<string>("Name").StartsWith("Super"))
    .ToArray();

// select * from SysAdminUnit where Name like '%visor'
UserConnection
    .QuerySchema("SysAdminUnit")
    .Where(item => item.Column<string>("Name").EndsWith("visor"))
    .ToArray();

// select * from SysAdminUnit where Name like '%Rumpelstilzchen%'
UserConnection
    .QuerySchema("SysAdminUnit")
    .Where(item => item.Column<string>("Name").Contains("Rumpelstilzchen"))
    .ToArray();
```

#### Filter by set of values:
```csharp
// select * from SysAdminUnit where SysAdminUnitTypeValue in (0,6)
UserConnection
    .QuerySchema("SysAdminUnit")
    .Where(item => new[]{0, 6}.Contains(item.Column<string>("SysAdminUnitTypeValue")))
    .ToArray();

// you should not use magic ^^ numbers, the right way is to initialize  
// a set of desired values into variable

var roleTypes = new []
{
    Consts.SysAdminUnitType.Oragnization,
    Consts.SysAdminUnitType.FunctionalRole,
};

UserConnection
    .QuerySchema("SysAdminUnit")
    .Where(item => roleTypes.Contains(item.Column<string>("SysAdminUnitTypeValue")))
    .ToArray();
```

#### Filter by logical field:
```csharp
// select * from SysAdminUnit where Active = 1
UserConnection
    .QuerySchema("SysAdminUnit")
    .Where(item => item.Column<bool>("Active")) // no need to write == true
    .ToArray();

// select * from SysAdminUnit where not Active = 1
UserConnection
    .QuerySchema("SysAdminUnit")
    .Where(item => !item.Column<bool>("Active")) 
    .ToArray();
```

### Combining logical operations
Use braces and standard logical operators to specify priority:

```csharp
UserConnection
    .QuerySchema("Contact")
    .Where(item => item.Column<bool>("[SysAdminUnit:Contact:Id].Active") 
        && (item.Column<Guid>("Account") == null || item.Column<string>("Email").EndsWith("@company.com")))
    .ToArray();
```

You can also use subsequent Where() methods, these conditions are combined with AND operation:
```csharp
UserConnection
    .QuerySchema("Actvity")
    .Where(item => item.Column<DateTime>("StartDate") > DateTime.Now)
    .Where(item => item.Column<DateTime>("StartDate") < DateTime.Now + TimeSpan.FromDays(5)) 
    .Where(item => item.Column<string>("Title").Contains("cumpleaños"))
    .ToArray();
```

### If you are not like others
and like writing conditions in reverse order, like ```if(null == someValue){}```, go on using this style in Where() 
filters, it should work fine:  

```csharp
var portalUsers = UserConnection
    .QuerySchema("SysAdminUnit")
    // I don't mind using this style, but remember what I said about magic numbers?
    .Where(item => 5 == item.Column<int>("SysAdminUnitTypeValue")) 
    .ToArray();
```

### Filtering by anonymous class fields
Where() method can be called after Select(), in this case projection fields will be used for filtering,
but you should remember 3 things:
1. You'll have no access to Column\<TValue>("Column.Path") method.
2. You can only filter by fields, which were added to projection.
3. Brush your teeth every morning and evening.

Example:
```csharp
UserConnection
    .QuerySchema("Contact")
    .Select(item => new
    {
        Name = item.Column<string>("Name"),
        TypeId = item.Column<string>("Type"),
        UserId = item.Column<string>("[SysAdminUnit:Contact:Id].Id"),
        UserIsActive = item.Column<bool>("[SysAdminUnit:Contact:Id].Active"),
    })
    // no DynamicEntity at this point, only anonymous class
    .Where(contact => contact.TypeId != Consts.Contact.Type.ContactPerson)
    .ToArray();
```

## Groups and aggregates
Let's start with aggregates. We have the following aggregate functions:
- Count
- Min
- Max
- Average
- Sum

Linq has methods with same names for them. For example, you need to find create date of the first Activity:
```csharp
var oldestActivityDate = UserConnection
    .QuerySchema("Activity")
    .Min(item => item.Column<DateTime>("CreatedOn"))
```
But you are unable to apply Average and Sum functions to DateTime field, c'est la vie.

Grouping field is added in GroupBy() method, which can then be accessed in Select block via Key field:
```csharp
var contactsByAccounts = UserConnection
	.QuerySchema("Contact")
	.GroupBy(item => item.Column<Guid>("Account"))
	.Select(group => new
	{
		AccountId = group.Key,
		ContactsCount = group.Count(),
	})
	.ToArray();
```
If lookup field is specified as a grouping filed, ESQ will generate query which groups by Id and Name, so you'd
better specify a column path to required identifier.

Grouping by multiple fields is also supported:
```csharp
var leadStats = UserConnection
    .QuerySchema("Lead")
    .GroupBy(item => new 
    {
        LeadStatusId = item.Column<Guid>("LeadStatus.Id"),
        QualifyStatusId = item.Column<Guid>("QualifyStatus.Id")
    })
    .Select(group => new 
    {
        LeadStatusId = group.Key.LeadStatusId,
        QualifyStatusId = group.Key.QualifyStatusId,
        Count = group.Count()
    })
    .ToArray();
```

Multiple aggregate fields are also supported:
```csharp
var leadStats = UserConnection
    .QuerySchema("Lead")
    .GroupBy(item => new 
    {
        LeadSourceId = item.Column<Guid>("LeadSource.Id"),
    })
    .Select(group => new 
    {
        LeadStatusId = group.Key.LeadSourceId,
        Count = group.Count(),
        FirstCreated = group.Min(item => item.Column<DateTime>("CreatedOn")),
        LastCreated = group.Max(item => item.Column<DateTime>("CreatedOn")),
    })
    .ToArray();
```

## Sorting
Sorting is easy: use methods OrderyBy/OrderByDescending for first field and ThenBy/ThenByDescending for subsequent ones.

Aggregate fields can also be used for sorting:
```csharp
var mostActivitiesOnDate = UserConnection
    .QuerySchema("Activity", LogOptions.ToTrace)
    .GroupBy(item => item.Column<DateTime>("StartDate"))
    .Select(group => new
    {
        StartDate = group.Key,
        Count = group.Count(),
    })
    .OrderByDescending(result => result.Count)
    .ThenBy(result => result.StartDate)
    .ToArray();
```
BTW, current Creatio versions have a bug, which can be reproduced the following way:
- Add grouping and aggregate field
- Add sorting by aggregate field
- Enable paging

ESQ will generate incorrect SQL query. Exampe of Linq query which fails:
```csharp
 var mostActivitiesOnDate = UserConnection
    .QuerySchema("Activity", LogOptions.ToTrace)
    .GroupBy(item => item.Column<DateTime>("StartDate"))
    .Select(group => new
    {
        StartDate = group.Key,
        Count = group.Count(),
    })
    .OrderByDescending(result => result.Count)
    .First();	// throws exception that no items were retrieved
```

## Paging
Linq has explicit ways to specify paging (Skip/Take) and implicit ones (First).
There's not much to say about it (except well-known fact that if paging is specified without sort field,
ESQ will add sort by Id column), just in case, here's the example:
```csharp
var secondAndThirdActivities = UserConnection
    .QuerySchema("Activity")
    .OrderBy(item => item.Column<DateTime>("CreatedOn"))
    .Skip(1)
    .Take(2)
    .ToArray();
```

Skip/Take methods can be used individually.

## Unsupported features
1. Subqueries not supported, you'll have to construct ESQ by your own.
2. No JOINs (use column expresisons).
3. No Macros.  
4. Linq methods not supported: Last(), LastOrDefault().

If you find anything else - let me know, I'll extend the list.

## Debugging
If you find a bug and would like to report it, you'll have to specify Linq expression and debug logs.  
For collecting logs you just need to pass an instance of [LogOptions](https://github.com/alt-shift-dev/Creatio.Linq/blob/main/Creatio.Linq/LogOptions.cs)
class to QuerySchema() method.

The easiest way - if you are able to collect standard logs from Trace Output (in unit test, for example):
```csharp
UserConnection
    .QuerySchema("Contact", LogOptions.ToTrace)
    .Count();
```

Or you can collect everything to StringBuilder, like here:
```csharp
var logBuilder = new StringBuilder();

UserConnection
    .QuerySchema("Contact", LogOptions.ToAnywhere(message => logBuilder.AppendLine(message)))
    .ToArray();

// now logBuilder'е contants lots of interesting stuff
```

## Performance
Using LogOptions you can enable performance measurement, if you'd like to repeat it at home.  
Average numbers I've seen while running tests:  
**First run**
```
*** LINQ parsing time: 44 ms.
*** ESQ generation time: 93 ms.
*** Query execution time: 42 ms.
```

**Subsequent runs** (any Linq expression)
```
*** LINQ parsing time: 0 ms.
*** ESQ generation time: 0 ms.
*** Query execution time: 50 ms.
```

## Contacts
Email: mike@creatio.me  
Telegram: @khrebtoff