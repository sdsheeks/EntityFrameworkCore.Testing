using System;
using EntityFrameworkCore.Testing.Common;
using EntityFrameworkCore.Testing.Moq.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.Testing.Moq
{
    /// <summary>Factory for creating mocked instances.</summary>
    public static partial class Create
    {
        /// <summary>Creates a mocked db query.</summary>
        /// <typeparam name="TQuery">The query type.</typeparam>
        /// <param name="dbQueryToMock">The db query to mock.</param>
        /// <returns>A mocked db query.</returns>
        [Obsolete("This method will remain until EntityFrameworkCore no longer supports the DbQuery<TQuery> type. Use Create.MockedReadOnlyDbSetFor instead.")]
        public static DbQuery<TQuery> MockedDbQueryFor<TQuery>(DbQuery<TQuery> dbQueryToMock)
            where TQuery : class
        {
            EnsureArgument.IsNotNull(dbQueryToMock, nameof(dbQueryToMock));

            return dbQueryToMock.CreateMockedDbQuery();
        }
    }
}