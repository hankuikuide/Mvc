using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class AuthorizationPageApplicationModelProvider : IPageApplicationModelProvider
    {
        private readonly IAuthorizationPolicyProvider _policyProvider;

        public AuthorizationPageApplicationModelProvider(IAuthorizationPolicyProvider policyProvider)
        {
            _policyProvider = policyProvider;
        }

        public int Order => -1000 + 10;

        public void OnProvidersExecuting(PageApplicationModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var pageModel = context.PageModel;
            if (pageModel.HandlerType == pageModel.PageType)
            {
                // Ignore filter lookup if the handler is a page.
                return;
            }

            var authorizeData = pageModel.HandlerAttributes.OfType<IAuthorizeData>().ToArray();
            if (authorizeData.Length > 0)
            {
                pageModel.Filters.Add(AuthorizationApplicationModelProvider.GetFilter(_policyProvider, authorizeData));
            }
            foreach (var attribute in pageModel.HandlerAttributes.OfType<IAllowAnonymous>())
            {
                pageModel.Filters.Add(new AllowAnonymousFilter());
            }
        }

        public void OnProvidersExecuted(PageApplicationModelProviderContext context)
        {
        }
    }
}
