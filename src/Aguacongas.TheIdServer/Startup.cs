// Project: Aguafrommars/TheIdServer
// Copyright (c) 2021 @Olivier Lefebvre
using Aguacongas.IdentityServer;
using Aguacongas.IdentityServer.Abstractions;
using Aguacongas.IdentityServer.Admin.Http.Store;
using Aguacongas.IdentityServer.Admin.Options;
using Aguacongas.IdentityServer.Admin.Services;
using Aguacongas.IdentityServer.EntityFramework.Store;
using Aguacongas.IdentityServer.Store;
using Aguacongas.TheIdServer.Admin.Hubs;
using Aguacongas.TheIdServer.Authentication;
using Aguacongas.TheIdServer.BlazorApp.Infrastructure.Services;
using Aguacongas.TheIdServer.BlazorApp.Models;
using Aguacongas.TheIdServer.BlazorApp.Services;
using Aguacongas.TheIdServer.Data;
using Aguacongas.TheIdServer.Models;
using Aguacongas.TheIdServer.Services;
using IdentityModel.AspNetCore.OAuth2Introspection;
using IdentityServer4.Quickstart.UI;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using ConfigurationModel = Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

namespace Aguacongas.TheIdServer
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public DbTypes DbType { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            DbType = Configuration.GetValue<DbTypes>("DbType");
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var isProxy = Configuration.GetValue<bool>("Proxy");

            void configureOptions(IdentityServerOptions options)
                => Configuration.GetSection("PrivateServerAuthentication").Bind(options);

            services.AddTransient<ISchemeChangeSubscriber, SchemeChangeSubscriber<SchemeDefinition>>()
                .AddIdentityProviderStore()
                .AddOperationalStores()
                .AddIdentity<ApplicationUser, IdentityRole>(
                    options => Configuration.GetSection(nameof(IdentityOptions)).Bind(options))
                .AddTheIdServerStores()
                .AddDefaultTokenProviders();

            if (isProxy)
            {
                services.AddConfigurationHttpStores(configureOptions);
            }
            else
            {
                AddDefaultServices(services);
            }

            ConfigureDataProtection(services);

            var identityServerBuilder = services.AddClaimsProviders(Configuration)
                .Configure<ForwardedHeadersOptions>(Configuration.GetSection(nameof(ForwardedHeadersOptions)))
                .Configure<AccountOptions>(Configuration.GetSection(nameof(AccountOptions)))
                .Configure<DynamicClientRegistrationOptions>(Configuration.GetSection(nameof(DynamicClientRegistrationOptions)))
                .Configure<TokenValidationParameters>(Configuration.GetSection(nameof(TokenValidationParameters)))
                .Configure<SiteOptions>(Configuration.GetSection(nameof(SiteOptions)))
                .ConfigureNonBreakingSameSiteCookies()
                .AddOidcStateDataFormatterCache()
                .AddIdentityServer(Configuration.GetSection(nameof(IdentityServerOptions)))
                .AddAspNetIdentity<ApplicationUser>()
                .AddDynamicClientRegistration()
                .ConfigureKey(Configuration.GetSection("IdentityServer:Key"));

            identityServerBuilder.AddJwtRequestUriHttpClient();

            if (isProxy)
            {
                identityServerBuilder.Services.AddTransient<IProfileService>(p =>
                {
                    var options = p.GetRequiredService<IOptions<IdentityServerOptions>>().Value;
                    var httpClient = p.GetRequiredService<IHttpClientFactory>().CreateClient(options.HttpClientName);
                    return new ProxyProfilService<ApplicationUser>(httpClient,
                        p.GetRequiredService<UserManager<ApplicationUser>>(),
                        p.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                        p.GetRequiredService<IEnumerable<IProvideClaims>>(),
                        p.GetRequiredService<ILogger<ProxyProfilService<ApplicationUser>>>());
                });
            }
            else
            {
                identityServerBuilder.AddProfileService<ProfileService<ApplicationUser>>();
                if (!Configuration.GetValue<bool>("DisableTokenCleanup"))
                {
                    identityServerBuilder.AddTokenCleaner(Configuration.GetValue<TimeSpan?>("TokenCleanupInterval") ?? TimeSpan.FromMinutes(1));
                }
            }

            services.AddTransient(p =>
            {
                var handler = new HttpClientHandler();
                if (Configuration.GetValue<bool>("DisableStrictSsl"))
                {
#pragma warning disable S4830 // Server certificates should be verified during SSL/TLS connections
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, policy) => true;
#pragma warning restore S4830 // Server certificates should be verified during SSL/TLS connections
                }
                return handler;
            })
                .AddHttpClient(OAuth2IntrospectionDefaults.BackChannelHttpClientName)
                .ConfigurePrimaryHttpMessageHandler(p => p.GetRequiredService<HttpClientHandler>());

            services.Configure<ExternalLoginOptions>(Configuration.GetSection("Google"))
                .AddAuthorization(options =>
                    options.AddIdentityServerPolicies())
                .AddAuthentication()
                .AddJwtBearer("Bearer", options => ConfigureIdentityServerAuthenticationOptions(options))
                // reference tokens
                .AddOAuth2Introspection("introspection", options => ConfigureIdentityServerAuthenticationOptions(options));


            var mvcBuilder = services.Configure<SendGridOptions>(Configuration)
                .AddLocalization()
                .AddControllersWithViews(options =>
                    options.AddIdentityServerAdminFilters())
                .AddViewLocalization()
                .AddDataAnnotationsLocalization()
                .AddNewtonsoftJson(options =>
                {
                    var settings = options.SerializerSettings;
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                })
                .AddIdentityServerWsFederation();

            ConfigureDynamicProviderManager(mvcBuilder, isProxy);

            services.AddRemoteAuthentication<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>();
            services.Configure<HostModelOptions>(Configuration.GetSection(nameof(HostModelOptions)))
                 .AddScoped<LazyAssemblyLoader>()
                 .AddScoped<AuthenticationStateProvider, RemoteAuthenticationService>()
                 .AddScoped<SignOutSessionStateManager>()
                 .AddScoped<ISharedStringLocalizerAsync, BlazorApp.Infrastructure.Services.StringLocalizer>()
                 .AddTransient<IReadOnlyCultureStore, PreRenderCultureStore>()
                 .AddTransient<IReadOnlyLocalizedResourceStore, PreRenderLocalizedResourceStore>()
                 .AddTransient<IAccessTokenProvider, AccessTokenProvider>()
                 .AddTransient<Microsoft.JSInterop.IJSRuntime, JSRuntime>()
                 .AddTransient<IKeyStore<RsaEncryptorDescriptor>, KeyStore<RsaEncryptorDescriptor, IdentityServer.KeysRotation.RsaEncryptorDescriptor>>()
                 .AddTransient<IKeyStore<IAuthenticatedEncryptorDescriptor>, KeyStore<IAuthenticatedEncryptorDescriptor, ConfigurationModel.IAuthenticatedEncryptorDescriptor>>()
                 .AddAdminApplication(new Settings())
                 .AddDatabaseDeveloperPageExceptionFilter()
                 .AddRazorPages(options => options.Conventions.AuthorizeAreaFolder("Identity", "/Account"));
        }       

        [SuppressMessage("Usage", "ASP0001:Authorization middleware is incorrectly configured.", Justification = "<Pending>")]
        public void Configure(IApplicationBuilder app)
        {
            var isProxy = Configuration.GetValue<bool>("Proxy");
            var disableHttps = Configuration.GetValue<bool>("DisableHttps");

            app.UseForwardedHeaders();
            AddForceHttpsSchemeMiddleware(app);

            if (!isProxy)
            {
                ConfigureInitialData(app);
            }

            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage()
                    .UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                if (!disableHttps)
                {
                    app.UseHsts();
                }
            }

            var scope = app.ApplicationServices.CreateScope();
            var scopedProvider = scope.ServiceProvider;
            var supportedCulture = scopedProvider.GetRequiredService<ISupportCultures>().CulturesNames.ToArray();


            app.UseRequestLocalization(options =>
            {
                options.DefaultRequestCulture = new RequestCulture("en");
                options.SupportedCultures = supportedCulture.Select(c => new CultureInfo(c)).ToList();
                options.SupportedUICultures = options.SupportedCultures;
                options.FallBackToParentCultures = true;
                options.AddInitialRequestCultureProvider(new SetCookieFromQueryStringRequestCultureProvider());
            })
                .UseSerilogRequestLogging();

            if (!disableHttps)
            {
                app.UseHttpsRedirection();
            }

            app.UseIdentityServerAdminApi("/api", child =>
            {
                if (Configuration.GetValue<bool>("EnableOpenApiDoc"))
                {
                    child.UseOpenApi()
                        .UseSwaggerUi3(options =>
                        {
                            var settings = Configuration.GetSection("SwaggerUiSettings").Get<NSwag.AspNetCore.SwaggerUiSettings>();
                            options.OAuth2Client = settings.OAuth2Client;
                        });
                }
                var allowedOrigin = Configuration.GetSection("CorsAllowedOrigin").Get<IEnumerable<string>>();
                if (allowedOrigin != null)
                {
                    child.UseCors(configure =>
                    {
                        configure.SetIsOriginAllowed(origin => allowedOrigin.Any(o => o == origin))
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
                    });
                }
            })
                .UseBlazorFrameworkFiles()
                .UseStaticFiles()
                .UseRouting()
                .UseIdentityServer();

            if (!isProxy)
            {
                app.UseIdentityServerAdminAuthentication("/providerhub", JwtBearerDefaults.AuthenticationScheme);
            }

            app
                .UseAuthorization()
                .Use((context, next) =>
                {
                    var service = context.RequestServices;
                    var settings = service.GetRequiredService<Settings>();
                    var request = context.Request;
                    settings.WelcomeContenUrl = $"{request.Scheme}://{request.Host}/api/welcomefragment";
                    var remotePathOptions = service.GetRequiredService<IOptions<RemoteAuthenticationApplicationPathsOptions>>().Value;
                    remotePathOptions.RemoteProfilePath = $"{request.Scheme}://{request.Host}/identity/account/manage";
                    remotePathOptions.RemoteRegisterPath = $"{request.Scheme}://{request.Host}/identity/account/register";
                    return next();
                })
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapRazorPages();
                    endpoints.MapDefaultControllerRoute();
                    if (!isProxy)
                    {
                        endpoints.MapHub<ProviderHub>("/providerhub");
                    }

                    endpoints.MapFallbackToPage("/_Host");
                });

            app.LoadDynamicAuthenticationConfiguration<SchemeDefinition>();
        }

        private void ConfigureDynamicProviderManager(IMvcBuilder mvcBuilder, bool isProxy)
        {
            var dynamicBuilder = mvcBuilder.AddIdentityServerAdmin<ApplicationUser, SchemeDefinition>();
            if (isProxy)
            {
                dynamicBuilder.AddTheIdServerStore();
            }
            else if (DbType == DbTypes.MongoDb)
            {
                dynamicBuilder.AddTheIdServerEntityMongoDbStore();
            }
            else if (DbType == DbTypes.RavenDb)
            {
                dynamicBuilder.AddTheIdServerStoreRavenDbStore();
            }
            else
            {
                dynamicBuilder.AddTheIdServerEntityFrameworkStore();
            }
        }
        private void AddForceHttpsSchemeMiddleware(IApplicationBuilder app)
        {
            var forceHttpsScheme = Configuration.GetValue<bool>("ForceHttpsScheme");

            if (forceHttpsScheme)
            {
                app.Use((context, next) =>
                {
                    context.Request.Scheme = "https";
                    return next();
                });
            }
        }

        private void ConfigureIdentityServerAuthenticationOptions(JwtBearerOptions options)
        {
            Configuration.GetSection("ApiAuthentication").Bind(options);
            if (Configuration.GetValue<bool>("DisableStrictSsl"))
            {
                options.BackchannelHttpHandler = new HttpClientHandler
                {
#pragma warning disable S4830 // Server certificates should be verified during SSL/TLS connections
                    ServerCertificateCustomValidationCallback = (message, cert, chain, policy) => true
#pragma warning restore S4830 // Server certificates should be verified during SSL/TLS connections
                };
            }
            options.Audience = Configuration["ApiAuthentication:ApiName"];
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var request = context.HttpContext.Request;
                    var path = request.Path;
                    var accessToken = TokenRetrieval.FromQueryString()(request);
                    if (path.StartsWithSegments("/providerhub") && !string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                        return Task.CompletedTask;
                    }
                    var oneTimeToken = TokenRetrieval.FromQueryString("otk")(request);
                    if (!string.IsNullOrEmpty(oneTimeToken))
                    {
                        context.Token = request.HttpContext
                            .RequestServices
                            .GetRequiredService<IRetrieveOneTimeToken>()
                            .GetOneTimeToken(oneTimeToken);
                        return Task.CompletedTask;
                    }
                    context.Token = TokenRetrieval.FromAuthorizationHeader()(request);
                    return Task.CompletedTask;
                }
            };

            options.ForwardDefaultSelector = context =>
            {
                var authHeader = context.Request.Headers[HttpRequestHeader.Authorization.ToString()];
                if (string.IsNullOrEmpty(authHeader))
                {
                    return null;
                }

                var parts = authHeader.First().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    return null;
                }

                if (!parts[1].Contains("."))
                {
                    return "introspection";
                }

                return null;
            };
        }

        private void ConfigureIdentityServerAuthenticationOptions(OAuth2IntrospectionOptions options)
        {
            Configuration.GetSection("ApiAuthentication").Bind(options);
            options.ClientId = Configuration.GetValue<string>("ApiAuthentication:ApiName");
            options.ClientSecret = Configuration.GetValue<string>("ApiAuthentication:ApiSecret");
            static string tokenRetriever(HttpRequest request)
            {
                var path = request.Path;
                var accessToken = TokenRetrieval.FromQueryString()(request);
                if (path.StartsWithSegments("/providerhub") && !string.IsNullOrEmpty(accessToken))
                {
                    return accessToken;
                }
                var oneTimeToken = TokenRetrieval.FromQueryString("otk")(request);
                if (!string.IsNullOrEmpty(oneTimeToken))
                {
                    return request.HttpContext
                        .RequestServices
                        .GetRequiredService<IRetrieveOneTimeToken>()
                        .GetOneTimeToken(oneTimeToken);
                }
                return TokenRetrieval.FromAuthorizationHeader()(request);
            }

            options.TokenRetriever = tokenRetriever;
        }

        private void AddDefaultServices(IServiceCollection services)
        {
            services.Configure<IdentityServerOptions>(options => Configuration.GetSection("ApiAuthentication").Bind(options))
                .AddIdentityProviderStore();

            if (DbType == DbTypes.RavenDb)
            {
                services.Configure<RavenDbOptions>(options => Configuration.GetSection(nameof(RavenDbOptions)).Bind(options))
                    .AddSingleton(p =>
                    {
                        var options = p.GetRequiredService<IOptions<RavenDbOptions>>().Value;
                        var documentStore = new DocumentStore
                        {
                            Urls = options.Urls,
                            Database = options.Database,
                        };
                        if (!string.IsNullOrWhiteSpace(options.CertificatePath))
                        {
                            documentStore.Certificate = new X509Certificate2(options.CertificatePath, options.CertificatePassword);
                        }
                        documentStore.SetFindIdentityPropertyForIdentityServerStores();
                        return documentStore.Initialize();
                    })                    
                    .AddIdentityServer4AdminRavenDbStores();

            }
            else if (DbType == DbTypes.MongoDb)
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                services.AddIdentityServer4AdminMongoDbStores(connectionString);
            }
            else
            {
                services.AddIdentityServer4AdminEntityFrameworkStores(options => options.UseDatabaseFromConfiguration(Configuration))
                    .AddConfigurationEntityFrameworkStores(options => options.UseDatabaseFromConfiguration(Configuration))
                    .AddOperationalEntityFrameworkStores(options => options.UseDatabaseFromConfiguration(Configuration));
            }

            var signalRBuilder = services.AddSignalR(options => Configuration.GetSection("SignalR:HubOptions").Bind(options));
            if (Configuration.GetValue<bool>("SignalR:UseMessagePack"))
            {
                signalRBuilder.AddMessagePackProtocol();
            }

            var redisConnectionString = Configuration.GetValue<string>("SignalR:RedisConnectionString");
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                signalRBuilder.AddStackExchangeRedis(redisConnectionString, options => Configuration.GetSection("SignalR:RedisOptions").Bind(options));
            }
        }

        private void ConfigureInitialData(IApplicationBuilder app)
        {
            var dbType = Configuration.GetValue<DbTypes>("DbType");
            if (Configuration.GetValue<bool>("Migrate") &&
                dbType != DbTypes.InMemory && dbType != DbTypes.RavenDb && dbType != DbTypes.MongoDb)
            {
                using var scope = app.ApplicationServices.CreateScope();
                var configContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                configContext.Database.Migrate();

                var opContext = scope.ServiceProvider.GetRequiredService<OperationalDbContext>();
                opContext.Database.Migrate();

                var appcontext = scope.ServiceProvider.GetService<ApplicationDbContext>();
                appcontext.Database.Migrate();
            }

            if (Configuration.GetValue<bool>("Seed"))
            {
                using var scope = app.ApplicationServices.CreateScope();
                SeedData.SeedConfiguration(scope, Configuration);
                SeedData.SeedUsers(scope, Configuration);
            }

        }
        private void ConfigureDataProtection(IServiceCollection services)
        {
            var dataprotectionSection = Configuration.GetSection(nameof(DataProtectionOptions));
            if (dataprotectionSection != null)
            {
                services.AddDataProtection(options => dataprotectionSection.Bind(options)).ConfigureDataProtection(Configuration.GetSection(nameof(DataProtectionOptions)));
            }
        }
    }
}
