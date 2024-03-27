using Bot.Models;
using Microsoft.AspNetCore.Builder;

namespace Bot.Abstractions;

public abstract class WebModule : Module
{
    public virtual string[] AddAuthorizationPolicy() => [];

    public virtual void PostWebBuild(WebApplication application, AppSettings appSettings)
    {
    }
}
