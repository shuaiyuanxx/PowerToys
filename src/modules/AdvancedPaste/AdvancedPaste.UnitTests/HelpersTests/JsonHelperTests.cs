// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using AdvancedPaste.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.DataTransfer;

namespace AdvancedPaste.UnitTests.HelpersTests
{
    [TestClass]
    public sealed class JsonHelperTests
    {
        // Method to access the private ProcessExistingJson method via reflection
        private string InvokeProcessExistingJson(string jsonText)
        {
            var jsonHelperType = typeof(JsonHelper);
            var method = jsonHelperType.GetMethod("ProcessExistingJson", BindingFlags.NonPublic | BindingFlags.Static);
            return method.Invoke(null, new object[] { jsonText }) as string;
        }

        [TestMethod]
        public void ProcessExistingJson_WithJsonArray_ReturnsProperlyFormattedJson()
        {
            // Arrange
            string jsonText = @"[
  ""Incorrect format when pasting the same data multiple times""
]";
            
            // Act
            string result = InvokeProcessExistingJson(jsonText);
            
            // Assert - should still be properly formatted JSON and not double-serialized
            string expected = @"[
  ""Incorrect format when pasting the same data multiple times""
]";
            Assert.AreEqual(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
        }

        [TestMethod]
        public void ProcessExistingJson_WithDoubleSerializedJson_ReturnsProperlyFormattedJson()
        {
            // Arrange - this simulates what would happen with double-serialized JSON
            string jsonText = @"[
  ""["",
  ""  \""Incorrect format when pasting the same data multiple times\"""",
  ""]""
]";
            
            // Act
            string result = InvokeProcessExistingJson(jsonText);
            
            // Assert - should extract the string values from the array
            string expected = @"[
  ""["",
  ""  \""Incorrect format when pasting the same data multiple times\"""",
  ""]""
]";
            Assert.AreEqual(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
        }

        [TestMethod]
        public void ProcessExistingJson_WithNestedJsonObject_ReturnsSameJson()
        {
            // Arrange
            string jsonText = @"{
  ""name"": ""Test"",
  ""value"": 123
}";
            
            // Act
            string result = InvokeProcessExistingJson(jsonText);
            
            // Assert - for objects, should return the same JSON
            Assert.AreEqual(jsonText, result);
        }
    }
}