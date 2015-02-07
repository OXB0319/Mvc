// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Razor.Directives;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Host.Directives
{
    public class CodeTreeCacheTest
    {
        [Fact]
        public void GetOrAdd_ReturnsCachedEntriesOnSubsequentCalls()
        {
            // Arrange
            var path = @"Views\_ViewStart.cshtml";
            var mockFileProvider = new Mock<TestFileProvider> { CallBase = true };
            var fileProvider = mockFileProvider.Object;
            fileProvider.AddFile(path, "test content");
            var codeTreeCache = new CodeTreeCache(fileProvider);
            var expected = new CodeTree();

            // Act
            var result1 = codeTreeCache.GetOrAdd(path, fileInfo => expected);
            var result2 = codeTreeCache.GetOrAdd(path, fileInfo => { throw new Exception("Shouldn't be called."); });

            // Assert
            Assert.Same(expected, result1);
            Assert.Same(expected, result2);
            mockFileProvider.Verify(f => f.GetFileInfo(It.IsAny<string>()), Times.Once());
        }

        [Fact]
        public void GetOrAdd_ReturnsNullValues_IfFileDoesNotExistInFileProvider()
        {
            // Arrange
            var path = @"Views\_ViewStart.cshtml";
            var mockFileProvider = new Mock<TestFileProvider> { CallBase = true };
            var fileProvider = mockFileProvider.Object;
            var codeTreeCache = new CodeTreeCache(fileProvider);
            var expected = new CodeTree();

            // Act
            var result1 = codeTreeCache.GetOrAdd(path, fileInfo => expected);
            var result2 = codeTreeCache.GetOrAdd(path, fileInfo => { throw new Exception("Shouldn't be called."); });

            // Assert
            Assert.Null(result1);
            Assert.Null(result2);
            mockFileProvider.Verify(f => f.GetFileInfo(It.IsAny<string>()), Times.Once());
        }

        [Fact]
        public void GetOrAdd_UpdatesCache_IfFileExpirationTriggerExpires()
        {
            // Arrange
            var path = @"Views\Home\_ViewStart.cshtml";
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(path, "test content");
            var codeTreeCache = new CodeTreeCache(fileProvider);
            var expected1 = new CodeTree();
            var expected2 = new CodeTree();

            // Act 1
            var result1 = codeTreeCache.GetOrAdd(path, fileInfo => expected1);

            // Assert 1
            Assert.Same(expected1, result1);

            // Act 2
            fileProvider.GetTrigger(path).IsExpired = true;
            var result2 = codeTreeCache.GetOrAdd(path, fileInfo => expected2);

            // Assert 2
            Assert.Same(expected2, result2);
        }

        [Fact]
        public void GetOrAdd_UpdatesCacheWithNullValue_IfFileWasDeleted()
        {
            // Arrange
            var path = @"Views\Home\_ViewStart.cshtml";
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(path, "test content");
            var codeTreeCache = new CodeTreeCache(fileProvider);
            var expected1 = new CodeTree();

            // Act 1
            var result1 = codeTreeCache.GetOrAdd(path, fileInfo => expected1);

            // Assert 1
            Assert.Same(expected1, result1);

            // Act 2
            fileProvider.DeleteFile(path);
            fileProvider.GetTrigger(path).IsExpired = true;
            var result2 = codeTreeCache.GetOrAdd(path, fileInfo => { throw new Exception("Shouldn't be called."); });

            // Assert 2
            Assert.Null(result2);
        }

        [Fact]
        public void GetOrAdd_UpdatesCacheWithValue_IfFileWasAdded()
        {
            // Arrange
            var path = @"Views\Home\_ViewStart.cshtml";
            var fileProvider = new TestFileProvider();
            var codeTreeCache = new CodeTreeCache(fileProvider);
            var expected = new CodeTree();

            // Act 1
            var result1 = codeTreeCache.GetOrAdd(path, fileInfo => { throw new Exception("Shouldn't be called."); });

            // Assert 1
            Assert.Null(result1);

            // Act 2
            fileProvider.AddFile(path, "test content");
            fileProvider.GetTrigger(path).IsExpired = true;
            var result2 = codeTreeCache.GetOrAdd(path, fileInfo => expected);

            // Assert 2
            Assert.Same(expected, result2);
        }
    }
}