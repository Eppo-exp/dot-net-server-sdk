using eppo_sdk.dto;

namespace eppo_sdk.logger
{
    public interface IAssignmentLogger
    {
        void LogAssignment(AssignmentLogData assignmentLogData);
    }
}