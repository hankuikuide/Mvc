// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public struct PageLoaderResult
    {
        public PageLoaderResult(
            CompiledPageActionDescriptor actionDescriptor,
            IList<IChangeToken> changeTokens)
        {
            ActionDescriptor = actionDescriptor;
            ExpirationTokens = changeTokens;
        }

        public CompiledPageActionDescriptor ActionDescriptor { get; }

        public IList<IChangeToken> ExpirationTokens { get; }
    }
}
