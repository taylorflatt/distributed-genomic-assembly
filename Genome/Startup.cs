using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Genome.Startup))]
namespace Genome
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
