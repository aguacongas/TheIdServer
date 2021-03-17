﻿// Project: Aguafrommars/TheIdServer
// Copyright (c) 2021 @Olivier Lefebvre
using Aguacongas.IdentityServer.Store;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Entity = Aguacongas.IdentityServer.Store.Entity;

namespace Aguacongas.IdentityServer.RavenDb.Store.Test.IdentityAdminStores
{
    public class IdentityUserLoginStoreTest
    {
        [Fact]
        public void Constuctor_should_check_parameters()
        {
            Assert.Throws<ArgumentNullException>(() => new IdentityUserLoginStore<IdentityUser>(null, null, null));
            using var documentStore = new RavenDbTestDriverWrapper().GetDocumentStore();
            var provider = new ServiceCollection()
                .AddLogging()
                .AddIdentity<IdentityUser, IdentityRole>()
                .AddRavenDbStores(p => documentStore)
                .Services.BuildServiceProvider();
            var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();

            Assert.Throws<ArgumentNullException>(() => new IdentityUserLoginStore<IdentityUser>(userManager, null, null));
            Assert.Throws<ArgumentNullException>(() => new IdentityUserLoginStore<IdentityUser>(userManager, documentStore.OpenAsyncSession(), null));
        }

        [Fact]
        public async Task CreateAsync_should_return_Login_id()
        {
            using var documentStore = new RavenDbTestDriverWrapper().GetDocumentStore();
            var provider = new ServiceCollection()
                .AddLogging()
                .AddIdentity<IdentityUser, IdentityRole>()
                .AddRavenDbStores(p => documentStore)
                .Services.BuildServiceProvider();
            var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();
            var user = new IdentityUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "exemple@exemple.com",
                EmailConfirmed = true
            };
            var roleResult = await userManager.CreateAsync(user);
            Assert.True(roleResult.Succeeded);

            var sut = new IdentityUserLoginStore<IdentityUser>(userManager, documentStore.OpenAsyncSession(), provider.GetRequiredService<ILogger<IdentityUserLoginStore<IdentityUser>>>());
            var result = await sut.CreateAsync(new Entity.UserLogin
            {
                UserId = user.Id,
                LoginProvider = Guid.NewGuid().ToString(),
                ProviderDisplayName = Guid.NewGuid().ToString(),
                ProviderKey = Guid.NewGuid().ToString()
            } as object);


            Assert.NotNull(result);
            Assert.NotNull(((Entity.UserLogin)result).Id);
        }

        [Fact]
        public async Task DeleteAsync_should_delete_Login()
        {
            using var documentStore = new RavenDbTestDriverWrapper().GetDocumentStore();
            var provider = new ServiceCollection()
                .AddLogging()
                .AddIdentity<IdentityUser, IdentityRole>()
                .AddRavenDbStores(p => documentStore)
                .Services.BuildServiceProvider();
            var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();
            var user = new IdentityUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "exemple@exemple.com",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user);
            Assert.True(result.Succeeded);
            var providerName = Guid.NewGuid().ToString();
            var key = Guid.NewGuid().ToString();
            result = await userManager.AddLoginAsync(user, new UserLoginInfo(providerName, key, providerName));
            Assert.True(result.Succeeded);

            var sut = new IdentityUserLoginStore<IdentityUser>(userManager, documentStore.OpenAsyncSession(), provider.GetRequiredService<ILogger<IdentityUserLoginStore<IdentityUser>>>());
            await sut.DeleteAsync($"{providerName}@{key}");

            var Logins = await userManager.GetLoginsAsync(user);
            Assert.Empty(Logins);
        }

        [Fact]
        public async Task UdpateAsync_should_update_Login()
        {
            using var documentStore = new RavenDbTestDriverWrapper().GetDocumentStore();
            var provider = new ServiceCollection()
                .AddLogging()
                .AddIdentity<IdentityUser, IdentityRole>()
                .AddRavenDbStores(p => documentStore)
                .Services.BuildServiceProvider();
            var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();
            var user = new IdentityUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "exemple@exemple.com",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user);
            Assert.True(result.Succeeded);
            var providerName = Guid.NewGuid().ToString();
            var key = Guid.NewGuid().ToString();
            result = await userManager.AddLoginAsync(user, new UserLoginInfo(providerName, key, providerName));
            Assert.True(result.Succeeded);

            var sut = new IdentityUserLoginStore<IdentityUser>(userManager, documentStore.OpenAsyncSession(), provider.GetRequiredService<ILogger<IdentityUserLoginStore<IdentityUser>>>());
            await sut.UpdateAsync(new Entity.UserLogin
            {
                Id = $"{providerName}@{key}",
                LoginProvider = providerName,
                ProviderKey = key,
                ProviderDisplayName = "test"
            } as object);

            var logins = await userManager.GetLoginsAsync(user);
            Assert.Single(logins);
            Assert.NotEqual("test", logins.First().ProviderDisplayName);
        }


        [Fact]
        public async Task GetAsync_by_page_request_should_find_role_Logins()
        {
            using var documentStore = new RavenDbTestDriverWrapper().GetDocumentStore();
            var provider = new ServiceCollection()
                .AddLogging()
                .AddIdentity<IdentityUser, IdentityRole>()
                .AddRavenDbStores(p => documentStore)
                .Services.BuildServiceProvider();
            var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();
            var user = new IdentityUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "exemple@exemple.com",
                EmailConfirmed = true
            };
            var roleResult = await userManager.CreateAsync(user);
            Assert.True(roleResult.Succeeded);
            await userManager.AddLoginAsync(user, new UserLoginInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            await userManager.AddLoginAsync(user, new UserLoginInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            await userManager.AddLoginAsync(user, new UserLoginInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));

            var sut = new IdentityUserLoginStore<IdentityUser>(userManager, documentStore.OpenAsyncSession(), provider.GetRequiredService<ILogger<IdentityUserLoginStore<IdentityUser>>>());

            var LoginsResult = await sut.GetAsync(new PageRequest
            {
                Filter = $"UserId eq '{user.Id}'",
                Take = 1
            });

            Assert.NotNull(LoginsResult);
            Assert.Equal(3, LoginsResult.Count);
            Assert.Single(LoginsResult.Items);
        }

        [Fact]
        public async Task GetAsync_by_id_should_return_Login()
        {
            using var documentStore = new RavenDbTestDriverWrapper().GetDocumentStore();
            var provider = new ServiceCollection()
                .AddLogging()
                .AddIdentity<IdentityUser, IdentityRole>()
                .AddRavenDbStores(p => documentStore)
                .Services.BuildServiceProvider();
            var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();
            var user = new IdentityUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "exemple@exemple.com",
                EmailConfirmed = true
            };
            var roleResult = await userManager.CreateAsync(user);
            Assert.True(roleResult.Succeeded);
            var providerName = Guid.NewGuid().ToString();
            var key = Guid.NewGuid().ToString();
            await userManager.AddLoginAsync(user, new UserLoginInfo(providerName, key, providerName));
            await userManager.AddLoginAsync(user, new UserLoginInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            await userManager.AddLoginAsync(user, new UserLoginInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));

            var sut = new IdentityUserLoginStore<IdentityUser>(userManager, documentStore.OpenAsyncSession(), provider.GetRequiredService<ILogger<IdentityUserLoginStore<IdentityUser>>>());

            var result = await sut.GetAsync($"{providerName}@{key}", null);

            Assert.NotNull(result);
        }
    }
}