﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Core.Testing
{
    public static class TaskExtensions
    {
        public static TimeSpan DefaultTimeout { get; } = TimeSpan.FromSeconds(10);

        public static Task<T> TimeoutAfterDefault<T>(this Task<T> task,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = default)
        {
            return task.TimeoutAfter(DefaultTimeout, filePath, lineNumber);
        }

        public static Task TimeoutAfterDefault(this Task task,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = default)
        {
            return task.TimeoutAfter(DefaultTimeout, filePath, lineNumber);
        }

        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = default)
        {
            // Don't create a timer if the task is already completed
            // or the debugger is attached
            if (task.IsCompleted || Debugger.IsAttached)
            {
                return await task;
            }

            var cts = new CancellationTokenSource();
            if (task == await Task.WhenAny(task, Task.Delay(timeout, cts.Token)))
            {
                cts.Cancel();
                return await task;
            }
            else
            {
                throw new TimeoutException(CreateMessage(timeout, filePath, lineNumber));
            }
        }

        public static async Task TimeoutAfter(this Task task, TimeSpan timeout,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = default)
        {
            // Don't create a timer if the task is already completed
            // or the debugger is attached
            if (task.IsCompleted || Debugger.IsAttached)
            {
                await task;
                return;
            }

            var cts = new CancellationTokenSource();
            if (task == await Task.WhenAny(task, Task.Delay(timeout, cts.Token)))
            {
                cts.Cancel();
                await task;
            }
            else
            {
                throw new TimeoutException(CreateMessage(timeout, filePath, lineNumber));
            }
        }

        private static string CreateMessage(TimeSpan timeout, string filePath, int lineNumber)
            => string.IsNullOrEmpty(filePath)
                ? $"The operation timed out after reaching the limit of {timeout.TotalMilliseconds}ms."
                : $"The operation at {filePath}:{lineNumber} timed out after reaching the limit of {timeout.TotalMilliseconds}ms.";
    }
}
