// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Services;

namespace AdvancedPaste.Services;

public sealed class ChatCompletionServiceSelector : IAIServiceSelector
{
    public bool TrySelectAIService<T>(
        Kernel kernel, KernelFunction function, KernelArguments arguments, [NotNullWhen(true)] out T? service, out PromptExecutionSettings? serviceSettings)
        where T : class, IAIService
    {
        foreach (var serviceToCheck in kernel.GetAllServices<T>())
        {
            var serviceModelId = serviceToCheck.GetModelId();
            var endpoint = serviceToCheck.GetEndpoint();

            if (!string.IsNullOrEmpty(serviceModelId) && serviceModelId.EndsWith("mini", StringComparison.OrdinalIgnoreCase))
            {
                service = serviceToCheck;
                serviceSettings = new OpenAIPromptExecutionSettings();
                return true;
            }
        }

        service = null;
        serviceSettings = null;
        return false;
    }
}
