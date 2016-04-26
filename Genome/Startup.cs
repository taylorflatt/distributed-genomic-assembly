using Microsoft.Owin;
using Owin;
using Hangfire;

[assembly: OwinStartupAttribute(typeof(Genome.Startup))]
namespace Genome
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //GlobalConfiguration.Configuration
            //// Use connection string name defined in `web.config` or `app.config`
            //.UseSqlServerStorage("Data Source=(LocalDb)\\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\aspnet-Genome-20160229104730.mdf");

            ConfigureAuth(app);
            //app.UseHangfireDashboard(); // Map Dashboard to the `http://<your-app>/hangfire` URL.
        }
    }
}
