﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Azure.Data.AppConfiguration;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.AzureAppConfiguration
{
    internal interface IConfigurationClientManager
    {
        IEnumerable<ConfigurationClientWrapper> GetClientWrappers();

        void UpdateClientStatus(ConfigurationClient client, bool successful);

        bool UpdateSyncToken(Uri endpoint, String syncToken);

        Uri GetEndpointForClient(ConfigurationClient client);
    }
}
