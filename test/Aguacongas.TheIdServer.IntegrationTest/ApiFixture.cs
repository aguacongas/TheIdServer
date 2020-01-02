﻿using Aguacongas.IdentityServer.EntityFramework.Store;
using Aguacongas.TheIdServer.Data;
using Aguacongas.TheIdServer.Models;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Aguacongas.TheIdServer.IntegrationTest
{
    public class ApiFixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly TestLoggerProvider _testLoggerProvider = new TestLoggerProvider();
        /// <summary>
        /// Gets the system under test
        /// </summary>
        public TestServer Sut { get; }

        public ITestOutputHelper TestOutputHelper 
        { 
            get { return _testLoggerProvider.TestOutputHelper; } 
            set { _testLoggerProvider.TestOutputHelper = value; }
        }

        public ApiFixture()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            Sut = TestUtils.CreateTestServer(
                // We use Sqlite in memory mode for tests. https://docs.microsoft.com/en-us/ef/core/miscellaneous/testing/sqlite
                services =>
                {
                    services.AddLogging(configure => configure.AddProvider(_testLoggerProvider))
                    .AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlite(_connection))
                    .AddIdentityServer4EntityFrameworkStores<ApplicationUser, ApplicationDbContext>(options =>
                        options.UseSqlite(_connection))
                    .AddIdentityProviderStore();
                });

            using var scope = Sut.Host.Services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<IdentityServerDbContext>();
            context.Database.EnsureCreated();
        }

        /// <summary>
        /// Performs a scopped action on <see cref="ApplicationDbContext"/>
        /// </summary>
        /// <param name="action">The action to perform</param>
        /// <returns></returns>
        public async Task DbActionAsync<T>(Func<T, Task> action) where T: DbContext
        {
            var services = Sut.Host.Services;
            using var scope = services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<T>();
            await action(context);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _connection.Dispose();
                    Sut?.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    [CollectionDefinition("api collection")]
    public class ApiCollection : ICollectionFixture<ApiFixture>
    {

    }
}