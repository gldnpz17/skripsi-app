using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SkripsiAppBackend.Common.ModelBinder
{
    // Why do we need this? Because ASP.NET Core would always
    // convert the time to UTC+0 when parsing datetimes in query strings.
    public class RelativeDateTimeModelBinder : IModelBinder
    {
        public class Provider : IModelBinderProvider
        {
            public IModelBinder? GetBinder(ModelBinderProviderContext context)
            {
                var modelType = context.Metadata.ModelType;

                if (modelType != typeof(DateTime) && modelType != typeof(DateTime?))
                {
                    return null;
                }

                return new RelativeDateTimeModelBinder();
            }
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var rawValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;

            if (bindingContext.ModelType == typeof(DateTime) && string.IsNullOrEmpty(rawValue))
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

            var success = DateTime.TryParse(rawValue, out var dateTime);

            if (!success)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(dateTime);
            return Task.CompletedTask;
        }
    }
}
