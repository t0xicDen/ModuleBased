using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using ModuleBased.Core.Infrastructure;
using ModuleBased.WebHost.Extensions;

namespace ModuleBased.WebHost
{
    public class Startup
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IList<ModuleInfo> _modules = new List<ModuleInfo>();

        public Startup(IHostingEnvironment env)
        {
            _hostingEnvironment = env;

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            LoadModulesAssembly();

            ConfigureRazorViewEngine(services);

            // Add framework services.
            var mvcBuilder = services.AddMvc();

            AddApplicationParts(services, mvcBuilder);
        }

        private void LoadModulesAssembly()
        {
            var moduleRootFolder = _hostingEnvironment.ContentRootFileProvider.GetDirectoryContents("/Modules");
            foreach (var moduleFolder in moduleRootFolder.Where(x => x.IsDirectory))
            {
                LoadModule(moduleFolder);
            }
        }

        private bool LoadModule(IFileInfo moduleFolder)
        {
            var binFolder = new DirectoryInfo(Path.Combine(moduleFolder.PhysicalPath, "bin"));
            if (!binFolder.Exists)
            {
                return false;
            }

            foreach (var file in binFolder.GetFileSystemInfos("*.dll", SearchOption.AllDirectories))
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(file.FullName);
                }
                catch (FileLoadException ex)
                {
                    throw;
                }

                if (assembly.FullName.Contains(moduleFolder.Name))
                {
                    _modules.Add(new ModuleInfo { Name = moduleFolder.Name, Assembly = assembly, Path = moduleFolder.PhysicalPath });
                }
            }

            return true;
        }

        private void ConfigureRazorViewEngine(IServiceCollection services)
        {
            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.ViewLocationExpanders.Add(new ModuleViewLocationExpander());

                var metadataReferences = _modules.Select(m => MetadataReference.CreateFromFile(m.Assembly.Location)).ToList();

                foreach (var reference in metadataReferences)
                {
                    options.AdditionalCompilationReferences.Add(reference);
                }

                options.CompilationCallback = (context) =>
                {                 
                    context.Compilation = context.Compilation.AddReferences(metadataReferences);
                };
            });
        }

        private void AddApplicationParts(IServiceCollection services, IMvcBuilder mvcBuilder)
        {
            foreach (var module in _modules)
            {
                // Register controller from modules
                mvcBuilder.AddApplicationPart(module.Assembly);

                // Register dependency in modules
                var moduleInitializerType = module.Assembly.GetTypes().FirstOrDefault(x => typeof(IModuleInitializer).IsAssignableFrom(x));
                if (moduleInitializerType != null && moduleInitializerType != typeof(IModuleInitializer))
                {
                    var moduleInitializer = (IModuleInitializer)Activator.CreateInstance(moduleInitializerType);
                    moduleInitializer.Init(services);
                }
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
