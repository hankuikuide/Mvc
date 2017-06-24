// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// Application model component for RazorPages.
    /// </summary>
    public class PageApplicationModel
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PageApplicationModel"/>.
        /// </summary>
        public PageApplicationModel(
            PageActionDescriptor actionDescriptor,
            TypeInfo pageType,
            TypeInfo modelType,
            TypeInfo handlerType,
            IReadOnlyList<object> handlerAttributes)
        {
            ActionDescriptor = actionDescriptor ?? throw new ArgumentNullException(nameof(actionDescriptor));
            PageType = pageType;
            ModelType = modelType;
            HandlerType = handlerType;

            Filters = new List<IFilterMetadata>();
            Properties = new CopyOnWriteDictionary<object, object>(
                actionDescriptor.Properties, 
                EqualityComparer<object>.Default);
            Handlers = new List<PageHandlerModel>();
            HandlerProperties = new List<PagePropertyModel>();
            HandlerAttributes = handlerAttributes;
        }

        /// <summary>
        /// A copy constructor for <see cref="PageApplicationModel"/>.
        /// </summary>
        /// <param name="other">The <see cref="PageApplicationModel"/> to copy from.</param>
        public PageApplicationModel(PageApplicationModel other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            ActionDescriptor = other.ActionDescriptor;

            Filters = new List<IFilterMetadata>(other.Filters);
            Properties = new Dictionary<object, object>(other.Properties);

            Handlers = new List<PageHandlerModel>(other.Handlers.Select(m => new PageHandlerModel(m)));
            HandlerProperties  = new List<PagePropertyModel>(other.HandlerProperties.Select(p => new PagePropertyModel(p)));
            HandlerAttributes = other.HandlerAttributes;
        }

        private PageActionDescriptor ActionDescriptor { get; }

        /// <summary>
        /// Gets the application root relative path for the page.
        /// </summary>
        public string RelativePath => ActionDescriptor.RelativePath;

        /// <summary>
        /// Gets the path relative to the base path for page discovery.
        /// </summary>
        public string ViewEnginePath => ActionDescriptor.ViewEnginePath;

        /// <summary>
        /// Gets the applicable <see cref="IFilterMetadata"/> instances.
        /// </summary>
        public IList<IFilterMetadata> Filters { get; }

        /// <summary>
        /// Stores arbitrary metadata properties associated with the <see cref="PageApplicationModel"/>.
        /// </summary>
        public IDictionary<object, object> Properties { get; }

        /// <summary>
        /// Gets the <see cref="PageHandlerModel"/> instances.
        /// </summary>
        public IList<PageHandlerModel> Handlers { get; }

        public IList<PagePropertyModel> HandlerProperties { get; }

        public TypeInfo PageType { get; }

        public TypeInfo ModelType { get; }

        public TypeInfo HandlerType { get; }

        public IReadOnlyList<object> HandlerAttributes { get; }


    }
}