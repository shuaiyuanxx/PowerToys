// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AllExperiments;
using Microsoft.PowerToys.Settings.UI.Helpers;
using Microsoft.PowerToys.Settings.UI.Library.Helpers;

namespace Microsoft.PowerToys.Settings.UI.Helpers
{
    internal sealed class LocalizationHelper
    {
        /// <summary>
        /// Gets a localized string from the resource file using ResourceLoaderInstance
        /// Tries multiple variations of the resource key to handle different naming conventions
        /// </summary>
        /// <param name="resourceKey">The resource key</param>
        /// <returns>The localized string, or null if not found</returns>
        public static string GetLocalizedStringFromResource(string resourceKey)
        {
            if (string.IsNullOrEmpty(resourceKey))
            {
                return null;
            }

            try
            {
                var resourceLoader = ResourceLoaderInstance.ResourceLoader;
                var result = resourceLoader.GetString($"{resourceKey}/Header");
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}
