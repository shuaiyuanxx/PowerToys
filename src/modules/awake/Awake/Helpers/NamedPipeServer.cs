// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Awake.Core;
using Awake.Core.Native;
using Awake.Properties;
using ManagedCommon;

namespace Awake.Helpers
{
    public static class NamedPipeServer
    {
        private const string PipeName = "PowerToysAwakeCommand";
        private static readonly CancellationTokenSource _cancellationTokenSource = new();

        private static bool intelligentAwakeOn;

        public static void StartServer()
        {
            Logger.LogInfo("Starting named pipe server for external commands");
            Task.Run(() => ListenForCommands(_cancellationTokenSource.Token));
        }

        public static void StopServer()
        {
            _cancellationTokenSource.Cancel();
        }

        public static void EnableIntelligentAwake()
        {
            intelligentAwakeOn = true;
        }

        public static void DisableIntelligentAwake()
        {
            intelligentAwakeOn = false;
        }

        private static async Task ListenForCommands(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using NamedPipeServerStream pipeServer = new(
                        PipeName,
                        PipeDirection.In,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous);

                    // Logger.LogInfo("Named pipe server waiting for connection");
                    await pipeServer.WaitForConnectionAsync(cancellationToken);

                    // Logger.LogInfo("Client connected to named pipe");
                    using StreamReader reader = new(pipeServer, Encoding.UTF8);

                    string? command = await reader.ReadLineAsync(cancellationToken);
                    if (!string.IsNullOrEmpty(command))
                    {
                        // Logger.LogInfo($"Received command: {command}");
                        ProcessCommand(command.Trim());
                    }
                }
                catch (OperationCanceledException)
                {
                    // Server is stopping, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error in named pipe server: {ex.Message}");

                    // Short delay before retrying to avoid CPU spinning on repeated errors
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        private static void ProcessCommand(string command)
        {
            if (!intelligentAwakeOn)
            {
                Logger.LogInfo("Intelligent awake is not enabled. No action will be made.");
                return;
            }

            switch (command.ToLowerInvariant())
            {
                case "keepawake":
                    Logger.LogInfo("Executing 'keepawake' command");

                    // Manager.SetIndefiniteKeepAwake(true);
                    break;

                case "lock":
                    Logger.LogInfo("Executing 'lock' command");

                    // Manager.SetPassiveKeepAwake(); // Stop keep-awake mode
                    // Bridge.LockWorkStation();      // Lock the computer
                    break;

                default:
                    Logger.LogWarning($"Unknown command received: {command}");
                    break;
            }
        }
    }
}
