﻿// Project: Aguafrommars/TheIdServer
// Copyright (c) 2020 @Olivier Lefebvre
using Aguacongas.IdentityServer.Store;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using System;
using System.Threading.Tasks;
using Entity = Aguacongas.IdentityServer.Store.Entity;

namespace Aguacongas.IdentityServer.Http.Store
{
    public class ClientStore : IClientStore
    {
        private readonly IAdminStore<Entity.Client> _store;

        public ClientStore(IAdminStore<Entity.Client> store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }
        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            var entity = await _store.GetAsync(clientId, new GetRequest
            {
                Expand = $"{nameof(Entity.Client.IdentityProviderRestrictions)},{nameof(Entity.Client.ClientClaims)},{nameof(Entity.Client.ClientSecrets)},{nameof(Entity.Client.AllowedGrantTypes)},{nameof(Entity.Client.RedirectUris)},{nameof(Entity.Client.AllowedScopes)},{nameof(Entity.Client.Properties)},{nameof(Entity.Client.Resources)}"
            }).ConfigureAwait(false);
            return entity.ToClient();
        }
    }
}