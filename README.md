# Creatio.Linq

Проект добавляет немного Linq в ваш ESQ.  
Для разбора дерева выражений Linq используется библиотека [re-linq](https://github.com/re-motion/Relinq).

## Поддержка Creatio
Проверялось на версии 7.15.2, с более свежими проблем быть не должно.  
А вот под более древние версии придется собирать самостоятельно.

## Установка
Тут два варианта:  
1. вы разарабатываете серверный код в студии. В этом случае библиотека устанавливается через nuget,
далее самостоятельно добавляете сборки Creatio.Linq.dll и Remotion.Linq.dll в Assemblies своего пакета.
2. просто ставите в систему пакет CreatioLinq и начинаете использовать Linq для ESQ-запросов 
(вариант для нескольких команд разработки - чтобы не было дублей сборки в разных пакетах).

### Установка из nuget
Тут как всегда:
```
Install-Package Creatio.Linq
```

### Установка пакета CreatioLinq
Зайти на страничку [Releases](https://github.com/alt-shift-dev/Creatio.Linq/releases) и скачать последнюю версию.

## Использование
Для начала использования достаточно подключить пространства имен Creatio.Linq и System.Linq и воспользоваться методом-расширением QuerySchema,
после чего можно пользоваться стандартными Linq-методами Select, Where, OrderBy и так далее:
```csharp
using Creatio.Linq;
///...

public int GetContactsCount()
{
    return UserConnection
        .QuerySchema("Contact")
        .Count();
}

```

### Обращение к полям объектов
Поскольку ESQ-выражения работают с нетипизированными объектами, для запросов в методы Select/Where/GroupBy/OrderBy изначально
пробрасывается класс [DynamicEntity](https://github.com/alt-shift-dev/Creatio.Linq/blob/main/Creatio.Linq/DynamicEntity.cs), 
являющийся наследником Entity с единственным методом-расширением ```Column<TValue>("Column.Path.Expression")```, которое и нужно использовать для обращения к колонкам 
. Пример:
```csharp
var supervisor = UserConnection
    .QuerySchema("Contact")
    .Where(item => item.Column<string>("Name") == "Supervisor")
    .Single();
```

Разумеется, можно использовать не только название одного поля схемы, но и любые конструкции, которые допускает ESQ:
```csharp
var supervisorContact = UserConnection
    .QuerySchema("Contact")
    .Where(item => item.Column<string>("[ContactCommunication:Contact:Id].Number") == "79001234567")
    .ToArray();
```

## Выполнение запроса
Для тех, кто не очень хорошо понимает, как работает Linq, сейчас будет важный нюанс. Это относится к любому Linq-выражению, 
не только к данному провайдеру.  
Так вот, Linq-выражения ленивые, прямо как ты. А значит это одну простую штуку: Linq-запрос не будет выполняться,
пока не будет попытки обратиться к его результату.  
Сделать это можно двумя способами: 
1. Вызвав агрегирующую функцию после выражения, если интересует скалярный результат.
2. Вызвав метод ToArray()/ToList()/First()/Single(), если нужны объекты.

Пример:
```csharp
var activityQueryable = UserConnection
    .QuerySchema("Activity")
    .Where(item => item.Column("EndDate") > DateTime.Now);

// на данном шаге никакой запрос не уйдет в БД, потому что не было попытки получить доступ к результату.
// activityQueryable - просто некий IQueryable<DymaicEntity> который можно обвешивать еще фильтрами.

// ниже способы как все-таки отправить запросы в БД:

// например, можно посчитать количество:
var pendingActivities = activityQueryable.Count();                  // получить результат агрегации

// или выбрать последние 10 штук по дате создания:
var latestTen = activityQueryable
    .OrderByDescending(item => item.Column<DateTime>("CreatedOn"))  // запрос все еще не выполнился
    .Take(10)                                                       // и тут тоже
    .ToArray();                                                     // а вот в этом месте он уходит в БД
```

Короче, я предупредил, если будете в issue писать, что ничего не работает, я буду инкрементить счетчик тех, кто не умеет читать.

## Проекции
Когда нужно получить не все поля объекта, а только некоторые, нужно использовать т.н. проекции, для чего в Linq есть метод Select:

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
    .ToArray(); // все же помнят что для выполнения запроса надо ToArray() вызвать?

// activeUsers - строго типизированный анонимный класс, можно 
// обращаться к его свойствам по-человечески:
Assert.IsNotNull(activeUsers.First().Name)
```

В примере выше можно заметить одну особенность: для лукапных полей по умолчанию возвращается их идентификатор,
причем предполагается, что колонка называется Id. К сожалению, быстрых способов выяснить название поля с идентификатором 
(мы же не хотим чтобы преобразование Linq-выражения в ESQ отнимало время?) найти не удалось, так что если в этом месте падает - 
обращайтесь к идентификатору явно: ```item.Column<Guid>("Contact.Id")``` или как там у вас идентификатор зовется.

Можно получать значение одного поля без анонимных классов:
```csharp
var activeUserIds = UserConnection
    .QuerySchema("SysAdminUnit")
    .Where(item => item.Column<bool>("Active") 
        && item.Column<Guid>("ContactId") != null)
    .Select(item => item.Column<Guid>("Id"))
    .ToArray();

// activeUserIds - теперь просто Guid[] с идентификаторами.
```

Что будет если не использовать метод Select()? В этом случае перед отправкой запроса 
будет вызван метод AddAllSchemaColumns() и обращаться к ним надо как к обычным полям Entity 
через GetColumnValue()/GetTypedColumnValue(). Примера не будет, мне лень.

## Фильтрация
Больше можно почерпнуть в проекте с тестами, например [этот](https://github.com/alt-shift-dev/Creatio.Linq/blob/main/Creatio.Linq.Tests/FilteringTests.cs) файл.  
Простейшие примеры уже были выше, на всякий случай повторим:

#### Фильтр Null/NotNull
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

#### Фильтры LIKE для строк:
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

#### Фильтр по множеству значений:
```csharp
// select * from SysAdminUnit where SysAdminUnitTypeValue in (0,6)
UserConnection
    .QuerySchema("SysAdminUnit")
    .Where(item => new[]{0, 6}.Contains(item.Column<string>("SysAdminUnitTypeValue")))
    .ToArray();

// за использование magic ^^ numbers можно получить люлей от тимлида, правильно конечно же 
// заранее инициализировать множество интересующих значений:

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

#### Фильтр по логическому полю:
```csharp
// select * from SysAdminUnit where Active = 1
UserConnection
    .QuerySchema("SysAdminUnit")
    .Where(item => item.Column<bool>("Active")) // не нужно дописывать == true
    .ToArray();

// select * from SysAdminUnit where not Active = 1
UserConnection
    .QuerySchema("SysAdminUnit")
    .Where(item => !item.Column<bool>("Active")) 
    .ToArray();
```

### Комбинирование логических выражений 
Можно пользоваться стандартными логическими операторами и скобками для расставления приоритетов:

```csharp
UserConnecttion
    .QuerySchema("Contact")
    .Where(item => item.Column<bool>("[SysAdminUnit:Contact:Id].Active") 
        && (item.Column<Guid>("Account") == null || item.Column<string>("Email").EndsWith("@company.com")))
    .ToArray();
```

А еще можно использовать Where несколько раз подряд, такие условия комбинируются с операцией И:
```csharp
UserConnecttion
    .QuerySchema("Actvity")
    .Where(item => item.Column<DateTime>("StartDate") > DateTime.Now)
    .Where(item => item.Column<DateTime>("StartDate") < DateTime.Now + TimeSpan.FromDays(5)) 
    .Where(item => item.Column<string>("Title").Contains("cumpleaños"))
    .ToArray();
```

### Если вы большой оригинал
и любите записывать условия в обратном виде, например, ```if(null == someValue){}``` то можете продолжать так писать
в фильтрах Where, будет работать:
```csharp
var portalUsers = UserConnection
    .QuerySchema("SysAdminUnit")
    // ничего не имею против, но ты же помнишь про magic numbers?
    .Where(item => 5 == item.Column<int>("SysAdminUnitTypeValue")) 
    .ToArray();
```

### Фильтрация по полям анонимного класса
Метод Where можно вызывать после Select, и тогда для фильтрации уже будут использоваться поля проекции, но нужно помнить
3 вещи:
1. Доступа к методу Column\<TValue>("Column.Path") уже не будет.
2. Фильтровать можно только по тем полям, которые добавлены в проекцию.
3. Чистить зубы надо утром и вечером.

Пример:
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
    // здесь уже не DynamicEntity а анонимный класс
    .Where(contact => contact.TypeId != Consts.Contact.Type.ContactPerson)
    .ToArray();
```

## Группировка и агрегация
Начнем с агрегации. Агрегатных функций у нас всего ничего:
- Count
- Min
- Max
- Average
- Sum

В Linq для них есть одноименные методы. Например, найти дату создания самой первой активности можно так:
```csharp
var oldestActivityDate = UserConnection
    .QuerySchema("Activity")
    .Min(item => item.Column<DateTime>("CreatedOn"))
```
А вот применить функции Average и Sum к полю с типом DateTime не получится, се ля ви.

Поле группировки добавляется в методе GroupBy, доступ к нему в блоке Select через свойство Key:
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
Если указать лукапное поле для группировки, то ESQ сгеренит запрос, в котором группировка будет по Id и Name,
так что лучше строить путь до нужного идентификатора.

Можно группировать по нескольким полям сразу:
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

Агрегатных полей может быть и несколько за раз:
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

## Сортировка
Тут все просто: используются методы OrderyBy/OrderByDescending для первого поля, ThenBy/ThenByDescending для последующих.  
По агрегатным полям тоже можно сортировать:
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

Кстати, на данный момент в ESQ есть баг, который можно выбить следующим кейсом:
- Добавить группировку и агрегатное поле
- Добавить сортировку по агрегатному полю
- Включить пейджинг

ESQ в таком случае генерит некорректный SQL-запрос. Пример Linq-запроса, который падает:
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
    .First();	// выкинет исключение что ни одной записи не найдено
```

## Пейджинг
В Linq есть явные способы задать пейджинг (Skip/Take) и неявные (First).  
Рассказывать про них особо нечего (кроме всем известного факта что при задании параметров
пейджинга и отсутствии поля сортировки ESQ сам добавит сортировку по Id), на всякий случай пример:
```csharp
var secondAndThirdActivities = UserConnection
    .QuerySchema("Activity")
    .OrderBy(item => item.Column<DateTime>("CreatedOn"))
    .Skip(1)
    .Take(2)
    .ToArray();
```
Методы Skip/Take можно использовать и по отдельности.

## Неподдерживаемые функции
1. На данный момент не поддерживаются подзапросы, для этого придется конструировать ESQ вручную.  
2. Join'ов нет, но зачем они, когда есть column path expressions?  
3. Макросов нет.  
4. Не поддерживаются методы Linq: Last(), LastOrDefault().

Если найдете что-то еще - дайте знать, внесу в список.


## Отладка
Если обнаружите баг и захотите его зарепортить, помимо Linq-выражения надо будет приложить отладочные логи.  
Чтобы собрать логи достаточно передать экземляр класса [LogOptions](https://github.com/alt-shift-dev/Creatio.Linq/blob/main/Creatio.Linq/LogOptions.cs)
в метод QuerySchema().
Самый простой вариант - если есть возможность собрать логи из стандартного Trace Output (например, из unit-теста):
```csharp
UserConnection
    .QuerySchema("Contact", LogOptions.ToTrace)
    .Count();
```

Или можно собирать все в StringBuilder, например так:
```csharp
var logBuilder = new StringBuilder();

UserConnection
    .QuerySchema("Contact", LogOptions.ToAnywhere(message => logBuilder.AppendLine(message)))
    .ToArray();

// теперь в logBuilder'е много интересного
```

## Производительность
Через LogOptions можно включить замер производительности, если хочется повторить это дома.  
Средние цифры, которые были во время прогона тестов:  
**Первый запуск**
```
*** LINQ parsing time: 44 ms.
*** ESQ generation time: 93 ms.
*** Query execution time: 42 ms.
```

**Повторные запуски** (любой Linq-запрос)
```
*** LINQ parsing time: 0 ms.
*** ESQ generation time: 0 ms.
*** Query execution time: 50 ms.
```

## Запуск тестов
Возможно, при попытке собрать проект с тестами у вас вывалится ошибка, что пакет Norbit.TRS.TestExtensions 
не найден в локальном nuget-репозитории. Если столкнулись с этим, напишите на [hr@norbit.ru](mailto:hr@norbit.ru), будем разбираться.

## Контакты
Email: mike@creatio.me  
Telegram: @khrebtoff