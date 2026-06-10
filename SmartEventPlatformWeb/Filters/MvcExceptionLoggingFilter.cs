using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace SmartEventPlatformWeb.Filters;

public sealed class MvcExceptionLoggingFilter : IExceptionFilter
{
    private readonly ILogger<MvcExceptionLoggingFilter> _logger;
    private readonly ITempDataDictionaryFactory _tempDataDictionaryFactory;

    public MvcExceptionLoggingFilter(
        ILogger<MvcExceptionLoggingFilter> logger,
        ITempDataDictionaryFactory tempDataDictionaryFactory)
    {
        _logger = logger;
        _tempDataDictionaryFactory = tempDataDictionaryFactory;
    }

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(
            context.Exception,
            "Unhandled MVC exception on {Method} {Path}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path);

        var tempData = _tempDataDictionaryFactory.GetTempData(context.HttpContext);
        tempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";

        context.Result = new RedirectToActionResult("Error", "Home", null);
        context.ExceptionHandled = true;
    }
}