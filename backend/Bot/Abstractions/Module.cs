﻿using Bot.Models;
using Bot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bot.Abstractions;

public abstract class Module
{
	public abstract string Creator { get; }

	public abstract string[] Contributors { get; }

	public abstract string[] Translators { get; }

	public virtual void AddLogging(ILoggingBuilder loggingBuilder)
	{
	}

	public virtual void AddPreServices(IServiceCollection services, CachedServices cachedServices,
		Action<DbContextOptionsBuilder> dbOption)
	{
	}

	public virtual void AddServices(IServiceCollection services, CachedServices cachedServices, AppSettings appSettings)
	{
	}

	public virtual void ConfigureServices(ConfigurationManager configuration, IServiceCollection services)
	{
	}

	public virtual void PostBuild(IServiceProvider services, CachedServices cachedServices)
	{
	}
}