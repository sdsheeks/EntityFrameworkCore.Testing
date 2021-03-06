# EntityFrameworkCore.Testing

[![Codacy Badge](https://app.codacy.com/project/badge/Grade/6d641ce391264f45b99acee8694a88d6?branch=2.x)](https://www.codacy.com/manual/rgvlee/EntityFrameworkCore.Testing?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=rgvlee/EntityFrameworkCore.Testing&amp;utm_campaign=Badge_Grade) [![Codacy Badge](https://app.codacy.com/project/badge/Coverage/6d641ce391264f45b99acee8694a88d6?branch=2.x)](https://www.codacy.com/manual/rgvlee/EntityFrameworkCore.Testing?utm_source=github.com&utm_medium=referral&utm_content=rgvlee/EntityFrameworkCore.Testing&utm_campaign=Badge_Coverage)

## Overview

EntityFrameworkCore.Testing is a mocking library that creates EntityFrameworkCore DbContext system mocks. It's easy to use (usually just a single line of code) with implementations for both Moq and NSubstitute.

It includes support for queries/keyless db sets, FromSql/FromSqlRaw/FromSqlInterpolated, ExecuteSqlCommand/ExecuteSqlRaw/ExecuteSqlInterpolated, and async LINQ operations.

## Resources

-   [Source repository](https://github.com/rgvlee/EntityFrameworkCore.Testing)

### EntityFrameworkCore 3.\*

-   [EntityFrameworkCore.Testing.Moq - NuGet](https://www.nuget.org/packages/EntityFrameworkCore.Testing.Moq/2.3.1)
-   [EntityFrameworkCore.Testing.NSubstitute - NuGet](https://www.nuget.org/packages/EntityFrameworkCore.Testing.NSubstitute/2.3.1)

### EntityFrameworkCore 2.1.0-2.2.6

-   [EntityFrameworkCore.Testing.Moq - NuGet](https://www.nuget.org/packages/EntityFrameworkCore.Testing.Moq/1.2.0)
-   [EntityFrameworkCore.Testing.NSubstitute - NuGet](https://www.nuget.org/packages/EntityFrameworkCore.Testing.NSubstitute/1.2.0)

## Prerequisites

### Virtual sets/queries

Your context set/query properties must be overridable:

```c#
public virtual DbSet<TestEntity> TestEntities { get; set; }
```

## Creating a mocked DbContext

If your context has constructor with a single `DbContextOptions` or `DbContextOptions<TDbContext>` parameter, creating a mocked context is as easy as:

```c#
var mockedDbContext = Create.MockedDbContextFor<TestDbContext>();
```

Any accessible constructor can be used provided it has a `DbContextOptions` or `DbContextOptions<TDbContext>` parameter:

```c#
var mockedLogger = Mock.Of<ILogger<TestDbContext>>();
var dbContextOptions = new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
var mockedDbContext = Create.MockedDbContextFor<TestDbContext>(mockedLogger, dbContextOptions);
```

Both of the above examples automatically create and use a Microsoft in-memory provider instance for the EntityFrameworkCore provider. If you want more control e.g., to specify the EntityFrameworkCore provider instance, use the builder:

```c#
var options = new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
var dbContextToMock = new TestDbContext(options);
var mockedDbContext = new MockedDbContextBuilder<TestDbContext>().UseDbContext(dbContextToMock).UseConstructorWithParameters(options).MockedDbContext;
```

There is no requirement to use the Microsoft in-memory provider. The following example uses the SQLite in-memory provider for a context with a parameterless constructor:

```c#
using (var connection = new SqliteConnection("Filename=:memory:"))
{
    connection.Open();
    var testEntity = Fixture.Create<TestEntity>();
    var dbContextToMock = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options);
    dbContextToMock.Database.EnsureCreated();
    var mockedDbContext = new MockedDbContextBuilder<TestDbContext>().UseDbContext(dbContextToMock).MockedDbContext;

    mockedDbContext.Set<TestEntity>().Add(testEntity);
    mockedDbContext.SaveChanges();

    Assert.Multiple(() =>
    {
        Assert.AreNotEqual(default(Guid), testEntity.Guid);
        Assert.DoesNotThrow(() => mockedDbContext.Set<TestEntity>().Single());
        Assert.AreEqual(testEntity, mockedDbContext.Find<TestEntity>(testEntity.Guid));
    });
}
```

## Usage

Start by creating a mocked context and, if the SUT requires, populate it as if you were using the real thing:

```c#
var testEntity = Fixture.Create<TestEntity>();
var mockedDbContext = Create.MockedDbContextFor<TestDbContext>();
mockedDbContext.Set<TestEntity>().Add(testEntity);
mockedDbContext.SaveChanges();

Assert.Multiple(() =>
{
    Assert.AreNotEqual(default(Guid), testEntity.Guid);
    Assert.DoesNotThrow(() => mockedDbContext.Set<TestEntity>().Single());
    Assert.AreEqual(testEntity, mockedDbContext.Find<TestEntity>(testEntity.Guid));
});
```

The Moq implementation of `Create.MockedDbContextFor<T>()` returns the mocked context. If you need the mock itself (e.g., to verify an invocation) use `Mock.Get(mockedDbSet)`:

```c#
var mockedDbContext = Create.MockedDbContextFor<TestDbContext>();
mockedDbContext.Set<TestEntity>().AddRange(Fixture.CreateMany<TestEntity>().ToList());
mockedDbContext.SaveChanges();

Assert.Multiple(() =>
{
    var dbContextMock = Mock.Get(mockedDbContext);
    dbContextMock.Verify(m => m.SaveChanges(), Times.Once);
});
```

### FromSql

Use `AddFromSqlResult` to add a from SQL result to the mock. The following will return `expectedResult` for any `FromSql<TestEntity>` invocation:

```c#
var expectedResult = Fixture.CreateMany<TestEntity>().ToList();
var mockedDbContext = Create.MockedDbContextFor<TestDbContext>();
mockedDbContext.Set<TestEntity>().AddFromSqlResult(expectedResult);

var actualResult = mockedDbContext.Set<TestEntity>().FromSql("[dbo].[USP_StoredProcedureWithNoParameters]").ToList();

Assert.Multiple(() =>
{
    Assert.IsNotNull(actualResult);
    Assert.IsTrue(actualResult.Any());
    CollectionAssert.AreEquivalent(expectedResult, actualResult);
});
```

The following will return `expectedResult` if the `FromSql` SQL query text contains `usp_StoredProcedureWithParameters` and a `@Parameter2` SQL parameter with a value of `Value2` has been provided:

```c#
var expectedResult = Fixture.CreateMany<TestEntity>().ToList();
var sqlParameters = new List<SqlParameter> { new SqlParameter("@Parameter2", "Value2") };
var mockedDbContext = Create.MockedDbContextFor<TestDbContext>();
mockedDbContext.Set<TestEntity>().AddFromSqlResult("usp_StoredProcedureWithParameters", sqlParameters, expectedResult);

var actualResult = mockedDbContext.Set<TestEntity>()
    .FromSql("[dbo].[USP_StoredProcedureWithParameters] @Parameter1 @Parameter2", new SqlParameter("@parameter1", "Value1"), new SqlParameter("@parameter2", "value2"))
    .ToList();

Assert.Multiple(() =>
{
    Assert.IsNotNull(actualResult);
    Assert.IsTrue(actualResult.Any());
    CollectionAssert.AreEquivalent(expectedResult, actualResult);
});
```

SQL query text matching supports partial, case insensitive matches. Individual parameter name and value matching is also case insentive. Case insensitive interpolated strings are also supported:

```c#
var expectedResult = Fixture.CreateMany<TestEntity>().ToList();
var parameter1 = Fixture.Create<DateTime>();
var parameter2 = Fixture.Create<string>();
var mockedDbContext = Create.MockedDbContextFor<TestDbContext>();
mockedDbContext.Set<TestEntity>().AddFromSqlResult($"usp_StoredProcedureWithParameters {parameter1}, {parameter2.ToUpper()}", expectedResult);

var actualResult = mockedDbContext.Set<TestEntity>().FromSql($"USP_StoredProcedureWithParameters {parameter1}, {parameter2.ToLower()}").ToList();

Assert.Multiple(() =>
{
    Assert.IsNotNull(actualResult);
    Assert.IsTrue(actualResult.Any());
    CollectionAssert.AreEquivalent(expectedResult, actualResult);
});
```

### FromSqlRaw/FromSqlInterpolated (EntityFrameworkCore 3.\*)

Use `AddFromSqlRawResult` and `AddFromSqlInterpolatedResult` to add results for `FromSqlRaw` and `FromSqlInterpolated` invocations. Refer to the FromSql section above for usage.

### Queries

Use `AddToReadOnlySource`, `AddRangeToReadOnlySource` and `ClearReadOnlySource` to manage a query source.

```c#
var expectedResult = Fixture.CreateMany<TestQuery>().ToList();
var mockedDbContext = Create.MockedDbContextFor<TestDbContext>();

mockedDbContext.Query<TestQuery>().AddRangeToReadOnlySource(expectedResult);

Assert.Multiple(() =>
{
    CollectionAssert.AreEquivalent(expectedResult, mockedDbContext.Query<TestQuery>().ToList());
    CollectionAssert.AreEquivalent(mockedDbContext.Query<TestQuery>().ToList(), mockedDbContext.TestView.ToList());
});
```

Specifying a from SQL result for a query is exactly the same as for a set.

### Keyless Sets (EntityFrameworkCore 3.\*)

Refer to the Queries section above for usage.

### ExecuteSqlCommand

Adding an execute SQL command result is similar to adding a from SQL result with the main difference being the return type. `ExecuteSqlCommand` returns an `int` (the number of rows affected by executing the SQL command text).

```c#
var commandText = "usp_StoredProcedureWithNoParameters";
var expectedResult = 1;
var mockedDbContext = Create.MockedDbContextFor<TestDbContext>();
mockedDbContext.AddExecuteSqlCommandResult(commandText, new List<SqlParameter>(), expectedResult);

var result = mockedDbContext.Database.ExecuteSqlCommand("USP_StoredProcedureWithNoParameters");

Assert.AreEqual(expectedResult, result);
```

All of the overloads have an optional `Action<string, IEnumerable<object>>` parameter which allows you to perform operations post invocation. The following provides a basic example where invoking `ExecuteSqlCommand` deletes a specified number of rows from a set:

```c#
//Arrange
var mockedDbContext = Create.MockedDbContextFor<TestDbContext>();

var itemsToCreate = 100;
mockedDbContext.Set<TestEntity>().AddRange(Fixture.CreateMany<TestEntity>(itemsToCreate).ToList());
mockedDbContext.SaveChanges();

var numberOfRowsToDelete = itemsToCreate / 2;
var rowsToDelete = mockedDbContext.Set<TestEntity>().Take(numberOfRowsToDelete).ToList();
var remainingRows = mockedDbContext.Set<TestEntity>().Skip(numberOfRowsToDelete).ToList();

mockedDbContext.AddExecuteSqlCommandResult("usp_MyStoredProc",
    numberOfRowsToDelete,
    (providedSql, providedParameters) =>
    {
        mockedDbContext.Set<TestEntity>().RemoveRange(rowsToDelete);
        mockedDbContext.SaveChanges();
    });

//Act
var actualResult = mockedDbContext.Database.ExecuteSqlCommand($"usp_MyStoredProc {numberOfRowsToDelete}");

//Assert
Assert.Multiple(() =>
{
    Assert.That(actualResult, Is.EqualTo(numberOfRowsToDelete));
    Assert.That(mockedDbContext.Set<TestEntity>().Count(), Is.EqualTo(itemsToCreate - numberOfRowsToDelete));
    Assert.That(mockedDbContext.Set<TestEntity>().ToList(), Is.EquivalentTo(remainingRows));
});
```

### ExecuteSqlRaw/ExecuteSqlInterpolated (EntityFrameworkCore 3.\*)

Use `AddExecuteSqlRawResult` and `AddExecuteSqlInterpolatedResult` to add results for `ExecuteSqlRaw` and `ExecuteSqlInterpolated` invocations. Refer to the ExecuteSqlCommand section above for usage.

### Async and LINQ queryable operations

Whenever you add a from SQL or execute SQL command result, EntityFrameworkCore.Testing sets up both the sync and async methods. It also automatically provides support for all sync and async LINQ queryable operations that are not supported by the Microsoft in-memory provider.

### Asserting mock invocations

The context and each set, query and their respective query providers are separate mocks. The following Moq example asserts that the `DbContext.SaveChanges` and `DbSet<TestEntity>.AddRange` methods were both invoked once.

```c#
var mockedDbContext = Create.MockedDbContextFor<TestDbContext>();
mockedDbContext.Set<TestEntity>().AddRange(Fixture.CreateMany<TestEntity>().ToList());
mockedDbContext.SaveChanges();

var dbContextMock = Mock.Get(mockedDbContext);
dbContextMock.Verify(m => m.SaveChanges(), Times.Once);

var byTypeDbSetMock = Mock.Get(mockedDbContext.Set<TestEntity>());
byTypeDbSetMock.Verify(m => m.AddRange(It.IsAny<IEnumerable<TestEntity>>()), Times.Once);

var byPropertyDbSetMock = Mock.Get(mockedDbContext.TestEntities);
byPropertyDbSetMock.Verify(m => m.AddRange(It.IsAny<IEnumerable<TestEntity>>()), Times.Once);
```
