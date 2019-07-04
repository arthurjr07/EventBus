using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIM.BuildingBlocks.EventBus.Interfaces;
using SIM.BuildingBlocks.EventBus.Messages;
using SIM.Services.Clinic.EventHandlers;
using SMI.BuildingBlocks.EventBus.Interfaces;
using SMI.BuildingBlocks.EventBus.Subscription;
using SMI.BuildingBlocks.EventBusAzure;

namespace SIM.Services.Clinic
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSingleton<IEventBus, EventBusAzure>();
            services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();
            services.Scan(scan => scan
                    .FromAssemblyOf<RegisterNewCustomerEventHandler>()
                        .AddClasses(classes => classes.AssignableTo<IRegistrationHandler>())
                            .AsImplementedInterfaces()
                            .WithTransientLifetime());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
            SubscribeEvents(app);
        }


        private  void SubscribeEvents(IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

            eventBus.Subscribe<RegisterNewCustomerEvent, RegisterNewCustomerEventHandler>();
        }
    }
}
