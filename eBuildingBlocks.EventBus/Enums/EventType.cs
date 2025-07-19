namespace BuildingBlocks.EventBus.Enums
{
    public enum EventType
    {
        UserProjectAssigned = 1,
        UserProjectUnassigned = 2,
        ProjectCreated = 3,
        ProjectUpdated = 4,
        ProjectDeleted = 5,
        UserAddedToProject = 6,
        UserRemovedFromProject = 7,
        TaskCreated = 8,
        TaskUpdated = 9,
        TaskDeleted = 10,
        TaskAssignedToUser = 11,
        TaskUnassignedFromUser = 12,
        UserCreated = 13,
        UserUpdated = 14,
        UserDeleted = 15,
        WorkspaceInvitationSent = 16,
    }
}
