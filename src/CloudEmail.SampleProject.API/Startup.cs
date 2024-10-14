using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using CloudEmail.SampleProject.API.Extensions;
using CloudEmail.SampleProject.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Amazon;


namespace CloudEmail.SampleProject.API
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        private static string ApiTitle => "Send Email API";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAWSService<IAmazonDynamoDB>(new AWSOptions
            {
                Region = RegionEndpoint.USWest2 // Change this to your table's region
            });
            services.AddScoped<SqsService>();
            services.AddSingleton<DynamoDbService>();
            services.AddEndpointsApiExplorer();
            services.AddControllers();
            services.ConfigureApiServices(Configuration, ApiTitle);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            string pathPrefix = "/api/v1/send-email";
            app.UsePathBase(new PathString(pathPrefix));
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swagger, httpReq) =>
                {
                    swagger.Servers.Add(new OpenApiServer { Url = pathPrefix });
                });
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint(Configuration["Swagger:Endpoint"], ApiTitle + " v1");
                c.RoutePrefix = string.Empty;
            });

            if (env.EnvironmentName.Contains("Development"))
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();
            
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            app.UseHealthChecksPrometheusExporter("/metrics", options => options.ResultStatusCodes[HealthStatus.Unhealthy] = (int)HttpStatusCode.OK);
        }
    }
}
