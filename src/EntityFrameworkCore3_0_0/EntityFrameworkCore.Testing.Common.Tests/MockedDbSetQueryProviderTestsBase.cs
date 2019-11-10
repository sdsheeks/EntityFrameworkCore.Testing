﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using AutoFixture;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace EntityFrameworkCore.Testing.Common.Tests
{
    [TestFixture]
    public abstract class MockedDbSetQueryProviderTestsBase<TEntity> : QueryableTestsBase<TEntity>
        where TEntity : TestEntityBase
    {
        protected abstract void AddFromSqlRawResult(DbSet<TEntity> mockedDbSet, IEnumerable<TEntity> expectedResult);
        protected abstract void AddFromSqlRawResult(DbSet<TEntity> mockedDbSet, string sql, IEnumerable<TEntity> expectedResult);
        protected abstract void AddFromSqlRawResult(DbSet<TEntity> mockedDbSet, string sql, IEnumerable<object> parameters, IEnumerable<TEntity> expectedResult);

        protected abstract void AddFromSqlInterpolatedResult(DbSet<TEntity> mockedDbSet, IEnumerable<TEntity> expectedResult);
        protected abstract void AddFromSqlInterpolatedResult(DbSet<TEntity> mockedDbSet, string sql, IEnumerable<TEntity> expectedResult);
        protected abstract void AddFromSqlInterpolatedResult(DbSet<TEntity> mockedDbSet, FormattableString sql, IEnumerable<TEntity> expectedResult);
        protected abstract void AddFromSqlInterpolatedResult(DbSet<TEntity> mockedDbSet, string sql, IEnumerable<object> parameters, IEnumerable<TEntity> expectedResult);

        protected DbSet<TEntity> DbSet => (DbSet<TEntity>) Queryable;

        [Test]
        public virtual void FromSqlRaw_AnySql_ReturnsExpectedResult()
        {
            var expectedResult = Fixture.CreateMany<TEntity>().ToList();
            AddFromSqlRawResult(DbSet, expectedResult);

            var actualResult1 = DbSet.FromSqlRaw("sp_NoParams").ToList();
            var actualResult2 = DbSet.FromSqlRaw("sp_NoParams").ToList();

            Assert.Multiple(() =>
            {
                Assert.That(actualResult1, Is.EquivalentTo(expectedResult));
                Assert.That(actualResult2, Is.EquivalentTo(actualResult1));
            });
        }

        [Test]
        public virtual void FromSqlRaw_QueryProviderWithManyFromSqlResults_ReturnsExpectedResults()
        {
            var sql1 = "sp_NoParams";
            var expectedResult1 = Fixture.CreateMany<TEntity>().ToList();

            var sql2 = "sp_WithParams";
            var parameters2 = new List<SqlParameter> {new SqlParameter("@SomeParameter1", "Value1"), new SqlParameter("@SomeParameter2", "Value2")};
            var expectedResult2 = Fixture.CreateMany<TEntity>().ToList();

            AddFromSqlRawResult(DbSet, sql1, expectedResult1);
            AddFromSqlRawResult(DbSet, sql2, parameters2, expectedResult2);

            Logger.LogDebug("actualResult1");
            var actualResult1 = DbSet.FromSqlRaw("[dbo].[sp_NoParams]").ToList();
            Logger.LogDebug("actualResult2");
            var actualResult2 = DbSet.FromSqlRaw("sp_NoParams").ToList();

            Logger.LogDebug("actualResult3");
            var actualResult3 = DbSet.FromSqlRaw("[dbo].[sp_WithParams]").ToList();
            Logger.LogDebug("actualResult4");
            var actualResult4 = DbSet.FromSqlRaw("sp_WithParams @SomeParameter1 @SomeParameter2").ToList();

            Logger.LogDebug("actualResult5");
            var actualResult5 = DbSet.FromSqlRaw("[dbo].[sp_WithParams]", new List<SqlParameter> {new SqlParameter("@someparameter2", "value2")}.ToArray()).ToList();
            Logger.LogDebug("actualResult6");
            var actualResult6 = DbSet.FromSqlRaw("sp_WithParams @SomeParameter1 @SomeParameter2", new List<SqlParameter> {new SqlParameter("@someparameter2", "value2")}.ToArray()).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(actualResult1, Is.EquivalentTo(expectedResult1));
                Assert.That(actualResult2, Is.EquivalentTo(actualResult1));

                Assert.That(actualResult3, Is.EquivalentTo(expectedResult2));
                Assert.That(actualResult4, Is.EquivalentTo(actualResult3));

                Assert.That(actualResult5, Is.EquivalentTo(expectedResult2));
                Assert.That(actualResult6, Is.EquivalentTo(actualResult5));
            });
        }

        [Test]
        public virtual void FromSqlRaw_SpecifiedSql_ReturnsExpectedResult()
        {
            var sql = "sp_NoParams";
            var expectedResult = Fixture.CreateMany<TEntity>().ToList();
            AddFromSqlRawResult(DbSet, sql, expectedResult);

            var actualResult1 = DbSet.FromSqlRaw("[dbo].[sp_NoParams]").ToList();
            var actualResult2 = DbSet.FromSqlRaw("sp_NoParams").ToList();

            Assert.Multiple(() =>
            {
                Assert.That(actualResult1, Is.EquivalentTo(expectedResult));
                Assert.That(actualResult2, Is.EquivalentTo(actualResult1));
            });
        }

        [Test]
        public virtual void FromSqlRaw_SpecifiedSqlThatDoesNotMatchSetUp_ThrowsException()
        {
            var sql = "asdf";
            var expectedResult = Fixture.CreateMany<TEntity>().ToList();
            AddFromSqlRawResult(DbSet, sql, expectedResult);

            Assert.Throws<NotSupportedException>(() =>
            {
                var actualResult = DbSet.FromSqlRaw("sp_NoParams").ToList();
            });
        }

        [Test]
        public virtual void FromSqlRaw_SpecifiedSqlWithSqlParameterParameters_ReturnsExpectedResult()
        {
            var sql = "sp_WithParams";
            var parameters = new List<SqlParameter> {new SqlParameter("@SomeParameter2", "Value2")};
            var expectedResult = Fixture.CreateMany<TEntity>().ToList();
            AddFromSqlRawResult(DbSet, sql, parameters, expectedResult);

            var actualResult1 = DbSet.FromSqlRaw("[dbo].[sp_WithParams] @SomeParameter1 @SomeParameter2", parameters.ToArray()).ToList();
            var actualResult2 = DbSet.FromSqlRaw("sp_WithParams @SomeParameter1 @SomeParameter2", parameters.ToArray()).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(actualResult1, Is.EquivalentTo(expectedResult));
                Assert.That(actualResult2, Is.EquivalentTo(actualResult1));
            });
        }

        [Test]
        public virtual void FromSqlRaw_SpecifiedSqlWithStringParameterParameters_ReturnsExpectedResult()
        {
            var sql = "sp_WithParams";
            var parameters = new List<string> { "Value2" };
            var expectedResult = Fixture.CreateMany<TEntity>().ToList();
            AddFromSqlRawResult(DbSet, sql, parameters, expectedResult);

            var actualResult1 = DbSet.FromSqlRaw("[dbo].[sp_WithParams] @SomeParameter1 @SomeParameter2", parameters.ToArray()).ToList();
            var actualResult2 = DbSet.FromSqlRaw("sp_WithParams @SomeParameter1 @SomeParameter2", parameters.ToArray()).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(actualResult1, Is.EquivalentTo(expectedResult));
                Assert.That(actualResult2, Is.EquivalentTo(actualResult1));
            });
        }

        [Test]
        public virtual void FromSqlRaw_SpecifiedSqlWithParametersThatDoNotMatchSetUp_ThrowsException()
        {
            var sql = "sp_WithParams";
            var setUpParameters = new List<SqlParameter> { new SqlParameter("@SomeParameter3", "Value3") };
            var invocationParameters = new List<SqlParameter> { new SqlParameter("@SomeParameter1", "Value1"), new SqlParameter("@SomeParameter2", "Value2") };
            var expectedResult = Fixture.CreateMany<TEntity>().ToList();
            AddFromSqlRawResult(DbSet, sql, setUpParameters, expectedResult);

            Assert.Throws<NotSupportedException>(() =>
            {
                var actualResult1 = DbSet.FromSqlRaw("[dbo].[sp_WithParams] @SomeParameter1 @SomeParameter2", invocationParameters.ToArray()).ToList();
            });

            Assert.Throws<NotSupportedException>(() =>
            {
                var actualResult2 = DbSet.FromSqlRaw("sp_WithParams @SomeParameter1 @SomeParameter2", invocationParameters.ToArray()).ToList();
            });
        }

        [Test]
        public virtual void FromSqlRaw_ThrowsException()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                var actualResult = DbSet.FromSqlRaw("sp_NoParams").ToList();
            });
        }

        [Test]
        public virtual void FromSqlInterpolated_ThrowsException()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                var actualResult = DbSet.FromSqlInterpolated($"sp_NoParams").ToList();
            });
        }
    }
}