using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public static class CompiledPageActionDescriptorBuilder
    {
        public static CompiledPageActionDescriptor Build(
            PageActionDescriptor actionDescriptor,
            PageApplicationModel applicationModel)
        {
            var boundProperties = CreateBoundProperties(applicationModel);
            var filters = applicationModel.Filters
                .Select(f => new FilterDescriptor(f, FilterScope.Action))
                .ToArray();
            var handlerMethods = CreateHandlerMethods(applicationModel);

            return new CompiledPageActionDescriptor(actionDescriptor)
            {
                ActionConstraints = actionDescriptor.ActionConstraints,
                AttributeRouteInfo = actionDescriptor.AttributeRouteInfo,
                BoundProperties = boundProperties,
                FilterDescriptors = filters,
                HandlerMethods = handlerMethods,
                HandlerTypeInfo = applicationModel.HandlerType,
                ModelTypeInfo = applicationModel.ModelType,
                RouteValues = actionDescriptor.RouteValues,
                PageTypeInfo = applicationModel.PageType,
                Properties = actionDescriptor.Properties,
            };
        }

        internal static HandlerMethodDescriptor[] CreateHandlerMethods(PageApplicationModel applicationModel)
        {
            var handlerModels = applicationModel.Handlers;
            var handlerDescriptors = new HandlerMethodDescriptor[handlerModels.Count];

            for (var i = 0; i < handlerDescriptors.Length; i++)
            {
                var handlerModel = handlerModels[i];

                handlerDescriptors[i] = new HandlerMethodDescriptor
                {
                    HttpMethod = handlerModel.HttpMethod,
                    Name = handlerModel.Name,
                    MethodInfo = handlerModel.HandlerMethod,
                    Parameters = CreateHandlerParameters(handlerModel),
                };
            }

            return handlerDescriptors;
        }

        internal static HandlerParameterDescriptor[] CreateHandlerParameters(PageHandlerModel handlerModel)
        {
            var methodParameters = handlerModel.Parameters;
            var parameters = new HandlerParameterDescriptor[methodParameters.Count];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterModel = methodParameters[i];

                parameters[i] = new HandlerParameterDescriptor
                {
                    BindingInfo = parameterModel.BindingInfo,
                    Name = parameterModel.ParameterName,
                    ParameterInfo = parameterModel.ParameterInfo,
                    ParameterType = parameterModel.ParameterInfo.ParameterType,
                };
            }

            return parameters;
        }

        internal static PageBoundPropertyDescriptor[] CreateBoundProperties(PageApplicationModel applicationModel)
        {
            var results = new List<PageBoundPropertyDescriptor>();
            for (var i = 0; i < applicationModel.HandlerProperties.Count; i++)
            {
                var propertyModel = applicationModel.HandlerProperties[i];

                if (propertyModel.BindingInfo == null)
                {
                    continue;
                }

                var descriptor = new PageBoundPropertyDescriptor
                {
                    Name = propertyModel.PropertyName,
                    BindingInfo = propertyModel.BindingInfo,
                    ParameterType = propertyModel.PropertyInfo.PropertyType,
                    SupportsGet = propertyModel.SupportsGets,
                };

                results.Add(descriptor);
            }

            return results.ToArray();
        }
    }
}
