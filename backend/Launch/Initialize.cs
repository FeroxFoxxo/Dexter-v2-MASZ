using Bot.Abstractions;
using Bot.Data;
using Bot.Models;
using Bot.Services;
using Launch.Helpers;
using Launch.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Launch;

public class Initialize
{
    public static async Task Main()
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("============ Launching =============");

            var builder = WebApplication.CreateBuilder();

            ConsoleHelper.AddHeading("Get Modules");
            var modules = GetModules();

            var cachedServices = new CachedServices();

            builder.Services.AddSingleton(cachedServices);

            ShouldEdit(builder.Environment);

            ConsoleHelper.AddHeading("Getting Client Id");
            var clientId = GetClientId();

            ConsoleHelper.AddHeading("Getting Database Info");
            var database = GetDatabaseOptions();

            AppSettings appSettings;

            try
            {
                ConsoleHelper.AddHeading("Getting App Settings");
                appSettings = await GetAppSettings(clientId, database);

                builder.Services.AddSingleton(appSettings);
            }
            catch (Exception e)
            {
                Console.WriteLine();

                ConsoleHelper.AddSubHeading("Failed to get app settings", e.ToString());

                ConsoleHelper.AddHeading("Trying to add migrations, in the case of an error!");

                await TryAddMigrations(cachedServices, builder, database);
                return;
            }

            ConsoleHelper.AddHeading("Importing Modules");
            InitializeModules(modules, database, cachedServices, appSettings, builder);

            ConsoleHelper.AddHeading("Get Authorization Policies");
            var authorizationPolicies = GetAuthorizationPolicies(modules);

            ConsoleHelper.AddHeading("Initializing Web Modules");
            InitializeWeb(cachedServices, builder, authorizationPolicies, appSettings);

            ConsoleHelper.AddHeading("Building Application");
            var app = builder.Build();
            ConsoleHelper.AddSubHeading("Successfully built", app.GetType().Name);

            ConsoleHelper.AddHeading("Adding Migrations");
            await AddMigrations(cachedServices, app);

            ConsoleHelper.AddHeading("Configuring Modules");
            foreach (var startup in modules)
                startup.ConfigureModules(modules, app);

            ConsoleHelper.AddHeading("Configuring App");
            ConfigureApp(modules, cachedServices, appSettings, app);

            ConsoleHelper.AddHeading("Running App");

            await app.RunAsync();
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.ToString());
        }

        Console.ResetColor();
    }

    private static void ShouldEdit(IHostEnvironment env)
    {
        try
        {
            if (env.IsDevelopment())
                return;

            if (ConsoleHelper.WaitForUser("edit settings", 10))
                ConsoleHelper.ShouldEdit = true;
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static ulong GetClientId()
    {
        while (true)
        {
            var client = ConsoleHelper.AskAndSet("Discord OAuth Client ID", "DISCORD_OAUTH_CLIENT_ID");

            if (!ulong.TryParse(client.Key, out var clientId))
                continue;

            ConsoleHelper.AddSubHeading("Found Client ID", clientId.ToString());

            return clientId;
        }
    }

    private static async Task TryAddMigrations(CachedServices cachedServices,
        WebApplicationBuilder builder, Action<DbContextOptionsBuilder> database)
    {
        foreach (var type in cachedServices.GetClassTypes<IDataContextInitialize>())
            type.GetMethod("AddContextToServiceProvider")?.Invoke(null, [database, builder.Services]);

        var app = builder.Build();

        Console.WriteLine();

        await AddMigrations(cachedServices, app);

        ConsoleHelper.AddHeading("Exiting Application");
    }

    private static Action<DbContextOptionsBuilder> GetDatabaseOptions()
    {
        var (databaseSettings, hasUpdatedDbSettings) = ConsoleHelper.CreateDatabaseSettings();

        if (hasUpdatedDbSettings)
            ConsoleHelper.AddSubHeading("You are finished creating the database settings for", databaseSettings.User);
        else
            ConsoleHelper.AddSubHeading("Found database settings for",
                $"{databaseSettings.User} // {databaseSettings.Database}");

        ConsoleHelper.AddSubHeading("Successfully created", "MySQL database provider");

        var connectionString = $"Server={databaseSettings.Host};Port={databaseSettings.Port};" +
                               $"Database={databaseSettings.Database};Uid={databaseSettings.User};Pwd={databaseSettings.Pass};";
        
        return x => x.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString),
            o =>
            {
                o.SchemaBehavior(MySqlSchemaBehavior.Translate, (schema, table) => $"{schema}_{table}");
                o.EnableRetryOnFailure();
            }
        );
    }

    private static async Task<AppSettings> GetAppSettings(ulong clientId, Action<DbContextOptionsBuilder> dbOptions)
    {
        ConsoleHelper.AddSubHeading("Querying database for", nameof(AppSettings));

        var dbBuilder = new DbContextOptionsBuilder<BotDatabase>();

        dbOptions.Invoke(dbBuilder);

        await using var dataContext = new BotDatabase(dbBuilder.Options);

        await dataContext.Database.MigrateAsync();

        var appSettingRepo = new SettingsRepository(dataContext, new AppSettings { ClientId = clientId }, null);

        var settings = await appSettingRepo.GetAppSettings();

        if (settings is null)
        {
            ConsoleHelper.AddHeading("Running First Time Setup");

            ConsoleHelper.AddSubHeading("Welcome to", "Dexter!");
            ConsoleHelper.AddSubHeading("Support Discord", "https://discord.gg/DBS664yjWN");

            settings = ConsoleHelper.CreateAppSettings(clientId);

            await appSettingRepo.AddAppSetting(settings);

            ConsoleHelper.AddSubHeading("You are finished creating the app settings for client",
                clientId.ToString());

            ConsoleHelper.AddSubHeading("You can now access the panel at", settings.GetServiceUrl());

            ConsoleHelper.AddSubHeading("You can always change these settings",
                "by pressing any key on next reboot");
        }
        else
        {
            ConsoleHelper.AddSubHeading("Found app settings for client", clientId.ToString());

            if (!ConsoleHelper.ShouldEdit)
                return settings;

            settings = ConsoleHelper.CreateAppSettings(clientId);
            await appSettingRepo.UpdateAppSetting(settings);
            Console.WriteLine();
        }

        return settings;
    }

    private static List<Module> GetModules()
    {
        var modules = ImportModules.GetModules();

        foreach (var module in modules)
        {
            ConsoleHelper.AddSubHeading("Imported",
                $"{module.GetType().Namespace}{(module is WebModule ? " (WEB)" : "")}");

            if (module.Contributors.Length > 0)
            {
                Console.Write("   Contributed By:      ");

                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine(string.Join(", ", module.Contributors));
            }

            Console.ResetColor();
        }

        return modules;
    }

    private static List<string> GetAuthorizationPolicies(List<Module> modules)
    {
        var authorizationPolicies = new List<string>();

        foreach (var startup in modules)
        {
            if (startup is not WebModule module)
                continue;

            var authorizationPolicy = module.AddAuthorizationPolicy();

            if (authorizationPolicy.Length > 0)
                authorizationPolicies = authorizationPolicies.Union(authorizationPolicy).ToList();
        }

        ConsoleHelper.AddSubHeading("Successfully added", "authentication policies");

        return authorizationPolicies;
    }

    private static void InitializeModules(List<Module> modules, Action<DbContextOptionsBuilder> dbOptions,
        CachedServices cachedServices, AppSettings appSettings, WebApplicationBuilder builder)
    {
        try
        {
            foreach (var startup in modules)
                startup.AddLogging(builder.Logging);

            ConsoleHelper.AddSubHeading("Successfully initialized", "logging");

            foreach (var startup in modules)
                startup.AddPreServices(builder.Services, cachedServices, dbOptions);

            ConsoleHelper.AddSubHeading("Successfully initialized", "pre-services");

            foreach (var startup in modules)
                startup.AddServices(builder.Services, cachedServices, appSettings);

            ConsoleHelper.AddSubHeading("Successfully initialized", "services");

            foreach (var startup in modules)
                startup.ConfigureServices(builder.Configuration, builder.Services);

            ConsoleHelper.AddSubHeading("Successfully configured", "services");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.ToString());
        }
    }

    private static void InitializeWeb(CachedServices cachedServices, WebApplicationBuilder builder,
        List<string> authorizationPolicies, AppSettings settings)
    {
        builder.WebHost.CaptureStartupErrors(true);

        builder.WebHost.UseUrls($"http://{settings.ServiceDomain}");

        builder.Services.AddMemoryCache();

        builder.Services.AddDataProtection().UseCryptographicAlgorithms(
            new AuthenticatedEncryptorConfiguration
            {
                EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
            }
        );

        var controller = builder.Services.AddControllers()
            .AddNewtonsoftJson(x =>
            {
                x.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                x.SerializerSettings.Converters.Add(new UlongConverter());
            });

        foreach (var assembly in cachedServices.Dependents)
            controller.AddApplicationPart(assembly);

        builder.Services.AddAuthorizationBuilder()
            .SetDefaultPolicy(
                new AuthorizationPolicyBuilder([.. authorizationPolicies])
                .RequireAuthenticatedUser()
                .Build()
            );

        ConsoleHelper.AddSubHeading("Started authorization policies for", string.Join(',', authorizationPolicies));
    }

    private static async Task AddMigrations(CachedServices cachedServices, WebApplication app)
    {
        ConsoleHelper.AddSubHeading("Heads up!", "This might take a while on a first install...\n");

        using (var scope = app.Services.CreateScope())
        {
            foreach (var dataContext in cachedServices.GetInitializedClasses<DbContext>(scope.ServiceProvider))
            {
                ConsoleHelper.AddSubHeading("Adding migrations for", dataContext.GetType().Name);

                await dataContext.Database.MigrateAsync();
            }
        }

        Console.WriteLine();

        ConsoleHelper.AddSubHeading("Successfully added", "migrations to databases");
    }

    private static void ConfigureApp(List<Module> modules, CachedServices cachedServices, AppSettings appSettings,
        WebApplication app)
    {
        foreach (var startup in modules)
        {
            startup.PostBuild(app.Services, cachedServices);

            if (startup is WebModule module)
                module.PostWebBuild(app, appSettings);
        }

        ConsoleHelper.AddSubHeading("Successfully post built", "application");

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
    }
}
