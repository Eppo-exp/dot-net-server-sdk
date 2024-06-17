using eppo_sdk.dto;
using eppo_sdk.dto.bandit;

namespace eppo_sdk.logger
{
    public interface IAssignmentLogger
    {
        void LogAssignment(AssignmentLogData assignmentLogData);
        void LogBanditAction(BanditLogEvent banditLogEvent);
    }
}