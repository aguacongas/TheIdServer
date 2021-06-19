// Project: Aguafrommars/TheIdServer
// Copyright (c) 2021 @Olivier Lefebvre
using Aguacongas.IdentityServer;
using Aguacongas.IdentityServer.Abstractions;
using Aguacongas.IdentityServer.Admin.Options;
using Aguacongas.IdentityServer.Admin.Services;
using Aguacongas.TheIdServer.Authentication;
using Aguacongas.TheIdServer.Models;
using IdentityModel.AspNetCore.OAuth2Introspection;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Services;
using IdentityServerHost.Quickstart.UI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Aguacongas.TheIdServer
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            void configureOptions(IdentityServerOptions options) 
                => Configuration.GetSection("PrivateServerAuthentication").Bind(options);

            services.AddTransient<HttpClientHandler>();

            services.AddIdentityProviderStore()
                .AddConfigurationHttpStores(configureOptions)
                .AddOperationalStores()
                .AddIdentity<ApplicationUser, IdentityRole>(
                    options => options.SignIn.RequireConfirmedAccount = Configuration.GetValue<bool>("SignInOptions:RequireConfirmedAccount"))
                .AddTheIdServerStores()
                .AddDefaultTokenProviders();

            var identityBuilder = services.AddClaimsProviders(Configuration)
                 .Configure<ForwardedHeadersOptions>(Configuration.GetSection(nameof(ForwardedHeadersOptions)))
                 .Configure<AccountOptions>(Configuration.GetSection(nameof(AccountOptions)))
                 .Configure<DynamicClientRegistrationOptions>(Configuration.GetSection(nameof(DynamicClientRegistrationOptions)))
                 .Configure<TokenValidationParameters>(Configuration.GetSection(nameof(TokenValidationParameters)))
                 .ConfigureNonBreakingSameSiteCookies()
                 .AddOidcStateDataFormatterCache()
                 .AddIdentityServer(Configuration.GetSection(nameof(IdentityServerOptions)))
                 .AddAspNetIdentity<ApplicationUser>()
                 .AddSigningCredentials()
                 .AddDynamicClientRegistration();

            identityBuilder.Services.AddTransient<IProfileService>(p =>
            {
                var options = p.GetRequiredService<IOptions<IdentityServerOptions>>().Value;
                var httpClient = p.GetRequiredService<IHttpClientFactory>().CreateClient(options.HttpClientName);
                return new ProxyProfilService<ApplicationUser>(httpClient,
                    p.GetRequiredService<UserManager<ApplicationUser>>(),
                    p.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                    p.GetRequiredService<IEnumerable<IProvideClaims>>(),
                    p.GetRequiredService<ILogger<ProxyProfilService<ApplicationUser>>>());
            });

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

            services.AddAuthorization(options =>
                    options.AddIdentityServerPolicies())
                .AddAuthentication()
                .AddIdentityServerAuthentication(JwtBearerDefaults.AuthenticationScheme, ConfigureIdentityServerAuthenticationOptions());

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
                });

            mvcBuilder.AddIdentityServerAdmin<ApplicationUser, SchemeDefinition>()
                    .AddTheIdServerStore();

            services.AddDatabaseDeveloperPageExceptionFilter()
                .AddRazorPages(options => options.Conventions.AuthorizeAreaFolder("Identity", "/Account"));
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage()
                    .UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error")
                    .UseHsts();
            }

            app.UseSerilogRequestLogging()
                .UseHttpsRedirection()
                .UseIdentityServerAdminApi("/api", child =>
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
                .UseAuthentication()
                .UseIdentityServer()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapRazorPages();
                    endpoints.MapDefaultControllerRoute();
                    endpoints.MapFallbackToFile("index.html");
                })
                .LoadDynamicAuthenticationConfiguration<SchemeDefinition>();
        }

        private Action<IdentityServerAuthenticationOptions> ConfigureIdentityServerAuthenticationOptions()
        {
            return options =>
            {
                Configuration.GetSection("ApiAuthentication").Bind(options);
                if (Configuration.GetValue<bool>("DisableStrictSsl"))
                {
                    options.JwtBackChannelHandler = new HttpClientHandler
                    {
#pragma warning disable S4830 // Server certificates should be verified during SSL/TLS connections
                        ServerCertificateCustomValidationCallback = (message, cert, chain, policy) => true
#pragma warning restore S4830 // Server certificates should be verified during SSL/TLS connections
                    };
                }

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
            };
        }
    }
}