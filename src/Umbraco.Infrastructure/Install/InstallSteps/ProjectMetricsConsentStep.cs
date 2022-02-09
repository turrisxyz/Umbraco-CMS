using System.Threading.Tasks;
using Umbraco.Cms.Core.Install.Models;

namespace Umbraco.Cms.Infrastructure.Install.InstallSteps
{
    [InstallSetupStep(InstallationType.Upgrade | InstallationType.NewInstall, "ProjectMetricsConsent", 30, "")]
    public class ProjectMetricsConsentStep : InstallSetupStep<ConsentModel>
    {
        public override string View => "metricsconsent";

        public override async Task<InstallSetupResult> ExecuteAsync(ConsentModel model)
        {
            return null;
        }

        public override bool RequiresExecution(ConsentModel model) => true;

    }
}
