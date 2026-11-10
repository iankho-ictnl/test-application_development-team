namespace PropertyAdmin.Logic.V1.Activities.Update;

public sealed class UpdateHandler(IActivityService activities)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await activities.UpdateAsync(command.Id, command.Activity, cancellationToken));
}
