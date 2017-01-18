using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;

namespace ModuleBased.WebHost.Extensions
{
    public class ModuleViewLocationExpander : IViewLocationExpander
    {
        private const string ModuleKey = "module";

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if (context.Values.ContainsKey(ModuleKey))
            {
                var module = context.Values[ModuleKey];
                if (!string.IsNullOrWhiteSpace(module))
                {
                    var moduleViewLocations = new string[]
                    {
                        "/Modules/ModuleBased.Module." + module + "/Views/{1}/{0}.cshtml",
                        "/Modules/ModuleBased.Module." + module + "/Views/Shared/{0}.cshtml"
                    };

                    viewLocations = moduleViewLocations.Concat(viewLocations);
                }
            }
            return viewLocations;
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            var assemblyName = (context.ActionContext.ActionDescriptor as ControllerActionDescriptor).ControllerTypeInfo.Assembly.GetName().Name;
            var moduleName = assemblyName.Split('.').Last();
            if (moduleName != "WebHost")
            {
                context.Values[ModuleKey] = moduleName;
            }
        }
    }
}
