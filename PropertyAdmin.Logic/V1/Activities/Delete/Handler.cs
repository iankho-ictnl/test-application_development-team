namespace PropertyAdmin.Logic.V1.Activities.Delete;

public sealed class DeleteHandler(IActivityService activities)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await activities.DeleteAsync(command.Id, cancellationToken));
}
