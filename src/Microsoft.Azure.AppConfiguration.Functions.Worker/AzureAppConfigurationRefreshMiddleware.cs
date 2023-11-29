﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.AppConfiguration.Functions.Worker
{
    /// <summary>
    /// Middleware for Azure App Configuration to use activity-based refresh for key-values registered in the provider.
    /// </summary>
    internal class AzureAppConfigurationRefreshMiddleware : IFunctionsWorkerMiddleware
    {
        // The minimum refresh interval on the configuration provider is 1 second, so refreshing more often is unnecessary
        private static readonly long MinimumRefreshInterval = TimeSpan.FromSeconds(1).Ticks;
        private long _refreshReadyTime = DateTimeOffset.UtcNow.Ticks;
        private readonly bool _isConfigureAwaitAllowed;

        private IEnumerable<IConfigurationRefresher> Refreshers { get; }

        public AzureAppConfigurationRefreshMiddleware(IConfigurationRefresherProvider refresherProvider)
        {
            Refreshers = refresherProvider.Refreshers;
            _isConfigureAwaitAllowed = IsConfigureAwaitAllowed();
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            long utcNow = DateTimeOffset.UtcNow.Ticks;

            long refreshReadyTime = Interlocked.Read(ref _refreshReadyTime);

            if (refreshReadyTime <= utcNow &&
                Interlocked.CompareExchange(ref _refreshReadyTime, utcNow + MinimumRefreshInterval, refreshReadyTime) == refreshReadyTime)
            {
                //
                // Configuration refresh is meant to execute as an isolated background task.
                // To prevent access of request-based resources, such as HttpContext, we suppress the execution context within the refresh operation.
                using (AsyncFlowControl flowControl = ExecutionContext.SuppressFlow())
                {
                    foreach (IConfigurationRefresher refresher in Refreshers)
                    {
                        _ = Task.Run(() => refresher.TryRefreshAsync());
                    }
                }
            }

            if (_isConfigureAwaitAllowed)
            {
                await next(context).ConfigureAwait(false);
            }
            else
            {
                #pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                await next(context);
                #pragma warning restore CA2007
            }
        }

        private static bool IsConfigureAwaitAllowed()
        {
            // Returns false if OrchestrationTriggerAttribute is loaded, true otherwise
            //return !AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetType("Microsoft.Azure.Functions.Worker.OrchestrationTriggerAttribute") != null);

            //var assembly = Assembly.GetExecutingAssembly();
            //return assembly.GetTypes()
            //    .Any(t => t.GetMethods()
            //        .Any(m => m.GetCustomAttributes(typeof(OrchestrationTriggerAttribute), false).Length > 0));

            return !AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetTypes()
                .Any(t => t.GetMethods()
                    .Any(m => m.GetCustomAttributes(typeof(OrchestrationTriggerAttribute), false).Length > 0)));
        }
    }
}
