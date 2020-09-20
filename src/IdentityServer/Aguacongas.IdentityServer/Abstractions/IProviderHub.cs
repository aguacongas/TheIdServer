﻿// Project: Aguafrommars/TheIdServer
// Copyright (c) 2020 @Olivier Lefebvre
using System.Threading.Tasks;

namespace Aguacongas.IdentityServer.Abstractions
{
    public interface IProviderHub
    {
        Task ProviderAdded(string scheme);

        Task ProviderUpdated(string scheme);

        Task ProviderRemoved(string scheme);
    }
}