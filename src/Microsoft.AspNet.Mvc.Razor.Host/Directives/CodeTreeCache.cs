// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.Framework.Expiration.Interfaces;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    /// <summary>
    /// Default implementation of <see cref="ICodeTreeCache"/>.
    /// </summary>
    public class CodeTreeCache : ICodeTreeCache
    {
        private readonly IFileProvider _fileProvider;
        private readonly ConcurrentDictionary<string, CodeTreeCacheEntry> _codeTreeCache;

        /// <summary>
        /// Initializes a new instance of <see cref="CodeTreeCache"/>.
        /// </summary>
        /// <param name="fileProvider">The application's <see cref="IFileProvider"/>.</param>
        public CodeTreeCache(IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
            _codeTreeCache = new ConcurrentDictionary<string, CodeTreeCacheEntry>(StringComparer.Ordinal);
        }

        /// <inheritdoc />
        public CodeTree GetOrAdd([NotNull] string pagePath,
                                 [NotNull] Func<IFileInfo, CodeTree> getCodeTree)
        {
            CodeTreeCacheEntry cacheEntry;
            if (_codeTreeCache.TryGetValue(pagePath, out cacheEntry))
            {
                var expirationTrigger = cacheEntry.ExpirationTrigger;
                if (!expirationTrigger.IsExpired)
                {
                    return cacheEntry.CodeTree;
                }
            }

            var file = _fileProvider.GetFileInfo(pagePath);
            // GetOrAdd is invoked for each _ViewStart that might potentially exist in the path.
            // We can avoid performing file system lookups for files that do not exist by caching
            // negative results and adding a Watch for that file.
            var codeTree = file.Exists ? getCodeTree(file) : null;
            var entry = new CodeTreeCacheEntry
            {
                ExpirationTrigger = _fileProvider.Watch(pagePath),
                CodeTree = codeTree
            };

            _codeTreeCache[pagePath] = entry;
            return codeTree;
        }

        private class CodeTreeCacheEntry
        {
            public CodeTree CodeTree { get; set; }

            public IExpirationTrigger ExpirationTrigger { get; set; }
        }
    }
}