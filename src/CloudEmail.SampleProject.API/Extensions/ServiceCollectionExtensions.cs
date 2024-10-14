using Amazon.S3;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmail;
using Amazon.SQS;
using AutoMapper;
using CloudEmail.Common.DependencyInjection.Extensions;
using CloudEmail.ApiAuthentication;
using CloudEmail.Data.Contexts;
using CloudEmail.Data.Interfaces;
using CloudEmail.Management.API.Client;
using CloudEmail.Management.API.Client.ClientInterfaces;
using CloudEmail.Mime.Libraries.Services;
using CloudEmail.Mime.Libraries.Services.Interfaces;
using CloudEmail.SampleProject.API.Clients;
using CloudEmail.SampleProject.API.Clients.Interfaces;
using CloudEmail.SampleProject.API.Configuration;
using CloudEmail.SampleProject.API.Data;
using CloudEmail.SampleProject.API.HealthChecks;
using CloudEmail.SampleProject.API.Mappings;
using CloudEmail.SampleProject.API.Mappings.Interfaces;
using CloudEmail.SampleProject.API.Services;
using CloudEmail.SampleProject.API.Services.Interface;
using CloudEmail.SampleProject.API.Validators;
using CloudEmail.SampleProject.API.Wrappers;
using CloudEmail.SampleProject.API.Wrappers.Interfaces;
using FluentValidation.AspNetCore;
using HealthChecks.Aws.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using CloudEmail.Metadata.Api.Client.Interfaces;
using CloudEmail.Metadata.Api.Client;
using Amazon.Runtime;
using Amazon;
using Channels.UH.Token.Services.Interfaces;
using Channels.UH.Token.Services;
using Channels.DFO.Api.Client.Services.Interfaces;
using Channels.DFO.Api.Client.Services;

namespace CloudEmail.SampleProject.API.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureApiServices(this IServiceCollection services, IConfiguration configuration, string apiTitle)
        {
            services
                .AddMvc(opt => { opt.Filters.Add(typeof(ValidatorActionFilter)); })
                .AddNewtonsoftJson(options => options.SerializerSettings.Formatting = Formatting.Indented)
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddFluentValidation(fvc => fvc.RegisterValidatorsFromAssemblyContaining<Startup>());

            // health check
            services.AddHealthChecks()
                .AddMySql(configuration.GetConnectionString(Constants.ReadApiDatabase), name: "Send-Email-Api RDS Read Database")
                .AddMySql(configuration.GetConnectionString(Constants.WriteApiDatabase), name: "Send-Email-Api RDS Write Database")
                .AddMySql(configuration.GetConnectionString(Constants.ReadEmailDatabase), name: "Email RDS Read Database")
                .AddCheck("S3 Outbound Storage", new S3ConnectionHealthCheck(new AmazonS3Config(), new S3BucketOptions() { BucketName = configuration["AmazonS3Configuration:BucketName"] }), HealthStatus.Unhealthy);

            return services
                .AddSwagger(apiTitle)
                .AddAutoMapper()
                .AddCustomAuthentication(configuration)
                .AddDbContexts(configuration)
                .AddHttpClient()
                .AddClients(configuration)
                .AddAWSServices(configuration)
                .AddServices()
                .AddMappings()
                .AddWrappers()
                .AddConfigValues(configuration);
        }

        public static IServiceCollection AddSwagger(this IServiceCollection services, string apiTitle)
        {
            services.AddSwaggerGen(options =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                options.IncludeXmlComments(xmlPath);
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = apiTitle,
                    Version = "v1"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please insert JWT (including 'Bearer ') into field",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                          new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new string[] {}
                    }
                });
            });

            return services;
        }

        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = configuration[Constants.ApiIssuer],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration[Constants.ApiIssuerSecret])),
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    ValidateAudience = false,
                    ValidateActor = false,
                    ValidateLifetime = true
                };
            });

            return services;
        }

        public static IServiceCollection AddDbContexts(this IServiceCollection services, IConfiguration configuration)
        {
            var readApiConnectionString = configuration.GetConnectionString(Constants.ReadApiDatabase);
            var writeApiConnectionString = configuration.GetConnectionString(Constants.WriteApiDatabase);
            var readEmailConnectionString = configuration.GetConnectionString(Constants.ReadEmailDatabase);

            services
                .AddDbContext<ReadApiDbContext>(options =>
                    options.UseMySql(readApiConnectionString, ServerVersion.AutoDetect(readApiConnectionString)))
                .AddDbContext<WriteApiDbContext>(options =>
                    options.UseMySql(writeApiConnectionString, ServerVersion.AutoDetect(writeApiConnectionString)))
                .AddDbContext<ReadEmailContext>(options =>
                    options.UseMySql(readEmailConnectionString, ServerVersion.AutoDetect(readEmailConnectionString)));

            return services
                .AddTransient<IReadEmailContext>(sp => sp.GetRequiredService<ReadEmailContext>());
        }

        public static IServiceCollection AddClients(this IServiceCollection services, IConfiguration configuration)
        {
            string baseUrl = configuration.GetValue<string>(Constants.EmailManagementApiBaseUrl);
            string basicToken = configuration.GetValue<string>(Constants.EmailManagementApiKey);
            string featureToggleBaseUrl = configuration.GetValue<string>(Constants.EmailFeatureToggleBaseUrl);

            return services
                .AddCloudwatchMetricsClient(configuration)
                .AddTransient<IPublishResultsClient, PublishResultsClient>()
                .AddSingleton<ISmtpServerClient>(s => new SmtpServerClient(baseUrl, basicToken, s.GetRequiredService<IHttpClientFactory>()))
                .AddSingleton<IBlackListClient>(s => new BlackListClient(baseUrl, basicToken))
                .AddSingleton<IEmailFeatureToggleClient>(s => new EmailFeatureToggleClient(baseUrl, basicToken, featureToggleBaseUrl))
                .AddSingleton<IServiceTokenService, ServiceTokenService>()
                .AddSingleton<IMessageService, MessageService>()
                .AddSingleton<IDFOMessageService,  DFOMessageService>()
                .AddMetadataClientFactory(configuration.GetSection(Constants.EmailMetadataApiSection));
        }

        public static IServiceCollection AddAWSServices(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                .AddSingleton<IAmazonSimpleEmailServiceV2>(s =>
                {
                    return new AmazonSimpleEmailServiceV2Client(new AmazonSimpleEmailServiceV2Config()
                    {
                        RegionEndpoint = RegionEndpoint.GetBySystemName(configuration["AmazonSESConfiguration:SesRegion"]),
                        HttpClientFactory = new AmazonHttpClientFactory(s.GetRequiredService<IHttpClientFactory>())
                    });
                })
                .AddSingleton<IAmazonSimpleEmailService>(s =>
                {
                    return new AmazonSimpleEmailServiceClient(new AmazonSimpleEmailServiceConfig()
                    {
                        RegionEndpoint = RegionEndpoint.GetBySystemName(configuration["AmazonSESConfiguration:SesRegion"]),
                        HttpClientFactory = new AmazonHttpClientFactory(s.GetRequiredService<IHttpClientFactory>())
                    });
                })
                .AddSingleton<IAmazonSQS>(s =>
                {
                    return new AmazonSQSClient(new AmazonSQSConfig()
                    {
                        HttpClientFactory = new AmazonHttpClientFactory(s.GetRequiredService<IHttpClientFactory>())
                    });
                })
                .AddSingleton<IAmazonS3>(s =>
                {
                    return new AmazonS3Client(new AmazonS3Config()
                    {
                        HttpClientFactory = new AmazonHttpClientFactory(s.GetRequiredService<IHttpClientFactory>())
                    });
                });
        }

        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            return services
                .AddTransient<IBlacklistService, BlacklistService>()
                .AddTransient<ICustomSmtpConfigurationService, CustomSmtpConfigurationService>()
                .AddTransient<IFeatureToggleService, FeatureToggleService>()
                .AddTransient<IEmailAuditService, EmailAuditService>()
                .AddTransient<IMimeService, MimeService>()
                .AddTransient<IPublishResultsService, PublishResultsService>()
                .AddTransient<IStorageService, StorageService>()
                .AddTransient<ISendEmailService, SendEmailService>()
                .AddTransient<ISerializationService, SerializationService>()
                .AddTransient<ISmtpService, SmtpService>()
                .AddTransient<ILogEmailQueueService, LogEmailQueueService>()
                .AddTransient<ICloudStorageQueueService, CloudStorageQueueService>()
                .AddTransient<IInvokeMimeBuilderLambdaService, InvokeMimeBuilderLambdaService>()
                .AddTransient<IDomainVerificationService, DomainVerificationService>()
                .AddTransient<IEmailStorageService, EmailStorageService>()
                .AddTransient<IExceptionService, ExceptionService>()
                .AddSingleton<IMemoryCache, MemoryCache>();
        }

        public static IServiceCollection AddMappings(this IServiceCollection services)
        {
            return services.AddTransient<ISecureSocketOptionsMapping, SecureSocketOptionsMapping>();
        }

        public static IServiceCollection AddAutoMapper(this IServiceCollection services)
        {
            var mappingConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new MappingProfile());
            });

            var mapper = mappingConfig.CreateMapper();

            return services.AddSingleton(mapper);
        }

        public static IServiceCollection AddWrappers(this IServiceCollection services)
        {
            return services
                .AddTransient<ISmtpClientWrapperFactory, SmtpClientWrapperFactory>();
        }

        public static IServiceCollection AddConfigValues(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                .Configure<AmazonS3Configuration>(configuration.GetSection(Constants.AmazonS3Configuration))
                .Configure<SmtpServiceConfiguration>(o => o.KerioHost = configuration[Constants.KerioHost])
                .Configure<RetryCountConfiguration>(configuration.GetSection(Constants.RetryCountConfiguration))
                .Configure<LogEmailSqsConfiguration>(configuration.GetSection(Constants.LogEmailSqsConfiguration))
                .AddSingleton(sp => sp.GetRequiredService<IOptions<LogEmailSqsConfiguration>>().Value)
                .Configure<CloudStorageSqsConfiguration>(configuration.GetSection(Constants.CloudStorageSqsConfiguration))
                .Configure<AmazonSESConfiguration>(configuration.GetSection(Constants.AmazonSESConfiguration))
                .AddSingleton(sp => sp.GetRequiredService<IOptions<CloudStorageSqsConfiguration>>().Value)
                .Configure<EmailSqsConfiguration>(configuration.GetSection(Constants.EmailSqsConfiguration))
                .AddSingleton(sp => sp.GetRequiredService<IOptions<EmailSqsConfiguration>>().Value);
        }
    }
}