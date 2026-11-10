using PropertyAdmin.Logic.V1.Activities.Models;

namespace PropertyAdmin.Logic.V1.Activities.Update;

public sealed record Command(int Id, ActivitySaveModel Activity);
