//using Blazor.Extensions.Logging;
using BlazorBootstrap.Modal;
using Ether.Reducers;
using Ether.Redux.Extensions;
using Ether.Types;
using Ether.Types.EditableTable;
using Ether.Types.State;
using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace Ether
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<EtherClient>();
            services.AddSingleton<EtherMenuService>();
            services.AddSingleton<ModelValidationService>();
            services.AddSingleton<TokenService>();
            services.AddSingleton<JsUtils>();
            services.AddSingleton<LocalStorage>();

            // State
            services.AddReduxStore<RootState>(cfg =>
            {
                cfg.Map(s => s.JobLogs, new LoadJobLogsReducer());
            });

            // Old State
            services.AddSingleton<ReportDescriptorStateService>();
            services.AddSingleton<ReportStateService>();
            services.AddSingleton<JobLogsStateService>();
            services.AddSingleton<VstsDataSourceStateService>();
            services.AddSingleton<IdentitiesStateService>();
            services.AddSingleton<ProjectsStateService>();
            services.AddSingleton<RepositoriesStateService>();
            services.AddSingleton<ProfilesStateService>();
            services.AddSingleton<TeamMembersStateService>();
            services.AddSingleton<AppState>();

            services.AddSingleton<EtherClientEditableTableDataProvider>();
            services.AddSingleton<NoOpEditableTableDataProvider>();
            services.AddBootstrapModal();

            //services.AddLogging(builder => builder
            //    .AddBrowserConsole()
            //    .SetMinimumLevel(LogLevel.Trace)
            //);
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.UseLocalTimeZone();
            app.AddComponent<App>("app");
        }
    }
}
