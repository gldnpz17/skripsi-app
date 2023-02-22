namespace SkripsiAppBackend.Common.Exceptions
{
    public class UserFacingException : Exception
    {
        public ErrorCodes ErrorCode { get; private set; }

        public UserFacingException (ErrorCodes errorCode) : base(errorCode.ToString())
        {
            ErrorCode = errorCode;
        }

        public enum ErrorCodes
        {
            UNKNOWN_ERROR,
            SPRINT_INVALID_DATE,
            TEAM_NO_SPRINTS,
            TEAM_NO_DEADLINE
        }
    }
}
