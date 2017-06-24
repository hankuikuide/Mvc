using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// A type which is used to represent a property in a <see cref="PageApplicationModel"/>.
    /// </summary>
    [DebuggerDisplay("PageHandlerModel: Name={MethodName}")]
    public class PageHandlerModel : ICommonModel
    {
        public PageHandlerModel(
            MethodInfo handlerMethod,
            IReadOnlyList<object> attributes)
        {
            HandlerMethod = handlerMethod ?? throw new ArgumentNullException(nameof(handlerMethod));

            Attributes = attributes;
            Parameters = new List<PageParameterModel>();
            Properties = new Dictionary<object, object>();
        }

        public PageHandlerModel(PageHandlerModel other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            HandlerMethod = other.HandlerMethod;
            Name = other.Name;

            Page = other.Page;

            // These are just metadata, safe to create new collections
            Attributes = new List<object>(other.Attributes);
            Properties = new Dictionary<object, object>(other.Properties);

            // Make a deep copy of other 'model' types.
            Parameters = new List<PageParameterModel>(other.Parameters.Select(p => new PageParameterModel(p) { Handler = this }));
        }

        public MethodInfo HandlerMethod { get; }

        public string HttpMethod { get; set; }

        public string Name { get; set; }

        public IList<PageParameterModel> Parameters { get; }

        public PageApplicationModel Page { get; set; }

        public IReadOnlyList<object> Attributes { get; }

        MemberInfo ICommonModel.MemberInfo => HandlerMethod;

        public IDictionary<object, object> Properties { get; }
    }
}
