using PropertyAdmin.Logic.V1.Reports.Models;

namespace PropertyAdmin.Logic.V1.Reports.GetOwnerOverview;

public sealed record Result(IReadOnlyList<OwnerOverviewModel> Owners);
