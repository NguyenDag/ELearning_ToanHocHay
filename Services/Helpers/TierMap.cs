using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>
    /// A2-05 — the single place that maps a package to a tier. Replaces the
    /// name-string matching (<c>Contains("premium")</c>) and the PackageId-as-tier hacks.
    /// </summary>
    public static class TierMap
    {
        public static PackageType ToDashboardType(PackageTier tier) => tier switch
        {
            PackageTier.Premium => PackageType.Premium,
            PackageTier.Standard => PackageType.Standard,
            _ => PackageType.Free
        };

        public static PackageType ToDashboardType(Package? package)
            => package == null ? PackageType.Free : ToDashboardType(package.Tier);

        public static int ToInt(PackageTier tier) => (int)ToDashboardType(tier);
    }
}
