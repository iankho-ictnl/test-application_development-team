using PropertyAdmin.Logic.V1.Activities.Models;

namespace PropertyAdmin.Logic.V1.Activities.GetAll;

public sealed record Result(IReadOnlyList<ActivityModel> Activities);
