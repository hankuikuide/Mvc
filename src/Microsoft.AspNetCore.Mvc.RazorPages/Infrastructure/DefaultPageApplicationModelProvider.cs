using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Internal;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class DefaultPageApplicationModelProvider : IPageApplicationModelProvider
    {
        private const string ModelPropertyName = "Model";
        private readonly MvcOptions _mvcOptions;

        public DefaultPageApplicationModelProvider(IOptions<MvcOptions> mvcOptions)
        {
            _mvcOptions = mvcOptions.Value;
        }

        public int Order => -1000;

        public void OnProvidersExecuting(PageApplicationModelProviderContext context)
        {
            context.PageModel = CreateModel(context.ActionDescriptor, context.PageType);
        }

        public void OnProvidersExecuted(PageApplicationModelProviderContext context)
        {
        }

        public virtual PageApplicationModel CreateModel(
            PageActionDescriptor actionDescriptor,
            TypeInfo pageTypeInfo)
        {
            // Pages always have a model type. If it's not set explicitly by the developer using
            // @model, it will be the same as the page type.
            var modelTypeInfo = pageTypeInfo.GetProperty(ModelPropertyName)?.PropertyType?.GetTypeInfo();

            // Now we want to find the handler methods. If the model defines any handlers, then we'll use those,
            // otherwise look at the page itself (unless the page IS the model, in which case we already looked).
            TypeInfo handlerType;

            var handlerModels = modelTypeInfo == null ? null : CreateHandlerModels(modelTypeInfo);
            if (handlerModels?.Count > 0)
            {
                handlerType = modelTypeInfo;
            }
            else
            {
                handlerType = pageTypeInfo.GetTypeInfo();
                handlerModels = CreateHandlerModels(pageTypeInfo);
            }

            var attributes = handlerType.GetCustomAttributes(inherit: true);
            var pageModel = new PageApplicationModel(
                actionDescriptor,
                pageTypeInfo,
                handlerType,
                modelTypeInfo,
                attributes);

            for (var i = 0; i < handlerModels.Count; i++)
            {
                var handlerModel = handlerModels[i];
                handlerModel.Page = pageModel;
                pageModel.Handlers.Add(handlerModel);
            }

            var properties = PropertyHelper.GetVisibleProperties(handlerType.AsType());
            for (var i = 0; i < properties.Length; i++)
            {
                var propertyModel = CreatePropertyModel(properties[i].Property);
                if (propertyModel != null)
                {
                    propertyModel.Page = pageModel;
                    pageModel.HandlerProperties.Add(propertyModel);
                }
            }

            for (var i = 0; i < _mvcOptions.Filters.Count; i++)
            {
                pageModel.Filters.Add(_mvcOptions.Filters[i]);
            }

            // Support for [TempData] on properties
            pageModel.Filters.Add(new PageSaveTempDataPropertyFilterFactory());

            // Always require an antiforgery token on post
            pageModel.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

            if (handlerType != pageTypeInfo)
            {
                for (var i = 0; i < attributes.Length; i++)
                {
                    if (attributes[i] is IFilterMetadata filter)
                    {
                        pageModel.Filters.Add(filter);
                    }
                }
            }

            return pageModel;
        }

        private IList<PageHandlerModel> CreateHandlerModels(TypeInfo handlerTypeInfo)
        {
            var methods = handlerTypeInfo.GetMethods();
            var results = new List<PageHandlerModel>();

            for (var i = 0; i < methods.Length; i++)
            {
                var handler = CreateHandlerModel(methods[i]);
                if (handler != null)
                {
                    results.Add(handler);
                }
            }

            return results;
        }

        protected virtual PageHandlerModel CreateHandlerModel(MethodInfo method)
        {
            if (!IsHandler(method))
            {
                return null;
            }

            if (method.IsDefined(typeof(NonHandlerAttribute)))
            {
                return null;
            }

            if (method.DeclaringType.GetTypeInfo().IsDefined(typeof(PagesBaseClassAttribute)))
            {
                return null;
            }

            if (!TryParseHandlerMethod(method.Name, out var httpMethod, out var handler))
            {
                return null;
            }

            var handlerModel = new PageHandlerModel(
                method,
                method.GetCustomAttributes(inherit: false))
            {
                Name = handler,
                HttpMethod = httpMethod,
            };

            var methodParameters = handlerModel.HandlerMethod.GetParameters();

            for (var i = 0; i < methodParameters.Length; i++)
            {
                var parameter = methodParameters[i];
                var parameterModel = CreateParameterModel(parameter);
                parameterModel.Handler = handlerModel;

                handlerModel.Parameters.Add(parameterModel);
            }

            return handlerModel;
        }

        protected virtual PageParameterModel CreateParameterModel(ParameterInfo parameter)
        {
            return new PageParameterModel(parameter, parameter.GetCustomAttributes(inherit: true))
            {
                BindingInfo = BindingInfo.GetBindingInfo(parameter.GetCustomAttributes()),
                ParameterName = parameter.Name,
            };
        }

        protected virtual PagePropertyModel CreatePropertyModel(PropertyInfo property)
        {
            var bindingInfo = BindingInfo.GetBindingInfo(property.GetCustomAttributes());
            var bindPropertyOnType = property.DeclaringType.GetCustomAttribute<BindPropertyAttribute>();
            if (bindingInfo == null && bindPropertyOnType == null)
            {
                return null;
            }

            if (property.DeclaringType.GetTypeInfo().IsDefined(typeof(PagesBaseClassAttribute)))
            {
                return null;
            }

            var bindPropertyOnProperty = property.GetCustomAttribute<BindPropertyAttribute>();
            var supportsGet = bindPropertyOnProperty?.SupportsGet ?? bindPropertyOnType?.SupportsGet ?? false;

            var descriptor = new PagePropertyModel(property, property.GetCustomAttributes(inherit: true))
            {
                PropertyName = property.Name,
                BindingInfo = bindingInfo ?? new BindingInfo(),
                SupportsGets = supportsGet, 
            };
                
            return descriptor;
        }

        protected virtual bool IsHandler(MethodInfo methodInfo)
        {
            // The SpecialName bit is set to flag members that are treated in a special way by some compilers
            // (such as property accessors and operator overloading methods).
            if (methodInfo.IsSpecialName)
            {
                return false;
            }

            // Overriden methods from Object class, e.g. Equals(Object), GetHashCode(), etc., are not valid.
            if (methodInfo.GetBaseDefinition().DeclaringType == typeof(object))
            {
                return false;
            }

            if (methodInfo.IsStatic)
            {
                return false;
            }

            if (methodInfo.IsAbstract)
            {
                return false;
            }

            if (methodInfo.IsConstructor)
            {
                return false;
            }

            if (methodInfo.IsGenericMethod)
            {
                return false;
            }

            return methodInfo.IsPublic;
        }

        internal static bool TryParseHandlerMethod(string methodName, out string httpMethod, out string handler)
        {
            httpMethod = null;
            handler = null;

            // Handler method names always start with "On"
            if (!methodName.StartsWith("On") || methodName.Length <= "On".Length)
            {
                return false;
            }

            // Now we parse the method name according to our conventions to determine the required HTTP method
            // and optional 'handler name'.
            //
            // Valid names look like:
            //  - OnGet
            //  - OnPost
            //  - OnFooBar
            //  - OnTraceAsync
            //  - OnPostEditAsync

            var start = "On".Length;
            var length = methodName.Length;
            if (methodName.EndsWith("Async", StringComparison.Ordinal))
            {
                length -= "Async".Length;
            }

            if (start == length)
            {
                // There are no additional characters. This is "On" or "OnAsync".
                return false;
            }

            // The http method follows "On" and is required to be at least one character. We use casing
            // to determine where it ends.
            var handlerNameStart = start + 1;
            for (; handlerNameStart < length; handlerNameStart++)
            {
                if (char.IsUpper(methodName[handlerNameStart]))
                {
                    break;
                }
            }

            httpMethod = methodName.Substring(start, handlerNameStart - start);

            // The handler name follows the http method and is optional. It includes everything up to the end
            // excluding the "Async" suffix (if present).
            handler = handlerNameStart == length ? null : methodName.Substring(handlerNameStart, length - handlerNameStart);
            return true;
        }
    }
}
