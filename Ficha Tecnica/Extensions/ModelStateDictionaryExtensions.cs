using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Ficha_Tecnica.Extensions;

public static class ModelStateDictionaryExtensions
{
    public static IReadOnlyList<string> GetErrorMessages(this ModelStateDictionary modelState)
    {
        if (modelState is null)
        {
            throw new ArgumentNullException(nameof(modelState));
        }

        var errors = new List<string>();

        foreach (var entry in modelState)
        {
            if (entry.Value?.Errors.Count > 0)
            {
                foreach (var error in entry.Value.Errors)
                {
                    var message = string.IsNullOrWhiteSpace(error.ErrorMessage) && error.Exception != null
                        ? error.Exception.Message
                        : error.ErrorMessage;

                    if (string.IsNullOrWhiteSpace(entry.Key))
                    {
                        errors.Add(message);
                    }
                    else
                    {
                        errors.Add($"{entry.Key}: {message}");
                    }
                }
            }
        }

        return errors;
    }
}
