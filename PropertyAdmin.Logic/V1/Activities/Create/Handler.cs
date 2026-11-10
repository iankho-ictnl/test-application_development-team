namespace PropertyAdmin.Logic.V1.Activities.Create;

public sealed class CreateHandler(IActivityService activities)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await activities.CreateAsync(command.Activity, cancellationToken));
}
