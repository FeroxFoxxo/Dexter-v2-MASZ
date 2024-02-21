using Bot.DTOs;
using Bot.Enums;
using Bot.Services;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bot.Abstractions;

public class EnumController<TTranslator>(Translation translator, ILogger<EnumController<TTranslator>> logger) : BaseController
{
    private readonly Translation _translator = translator;
    private readonly ILogger<EnumController<TTranslator>> _logger = logger;

    public Task<ObjectResult> TranslateEnum<TEnumType>(Language? language)
        where TEnumType : struct, Enum
    {
        _translator.SetLanguage(language);

        var translator = _translator.Get<TTranslator>();

        var method = typeof(TTranslator).GetMethods()
            .FirstOrDefault(s =>
                {
                    var parameters = s.GetParameters();

                    return
                        parameters.Length == 1 &&
                        parameters.Any(a => a.ParameterType == typeof(TEnumType)) &&
                        s.ReturnParameter?.ParameterType == typeof(string);
                }
            );

        if (method == null)
        {
            _logger.LogError("Could not find a method to translate the enum of {Enum} for {Translator}",
                nameof(TEnumType), nameof(TTranslator));
            return Task.FromResult(Problem("Translator does not exist for this enum."));
        }

        var enums = Enum.GetValues<TEnumType>().Select(enumValue =>
            new EnumDto(
                enumValue.GetHashCode(),
                ((string)method.Invoke(translator, [enumValue])).Humanize()
            )
        ).ToList();

        return Task.FromResult(Ok(enums) as ObjectResult);
    }

}
