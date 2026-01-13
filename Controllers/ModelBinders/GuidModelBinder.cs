using Microsoft.AspNetCore.Mvc.ModelBinding;

public class GuidModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
        if (Guid.TryParse(value, out var guid))
        {
            bindingContext.Result = ModelBindingResult.Success(guid);
        }
        else
        {
            // Set to Guid.Empty to indicate invalid input
            bindingContext.Result = ModelBindingResult.Success(Guid.Empty);
        }
        return Task.CompletedTask;
    }
}