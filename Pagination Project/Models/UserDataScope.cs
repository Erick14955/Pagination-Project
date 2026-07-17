namespace Pagination_Project.Models
{
    public sealed class UserDataScope
    {
        public Guid UserId { get; init; }
        public Guid? EmployeeId { get; init; }
        public short? EmployeeTypeId { get; init; }
        public string EmployeeTypeCode { get; init; } = string.Empty;
        public bool ViewAllEmployeeTypes { get; init; }

        public bool HasDepartmentAccess =>
            ViewAllEmployeeTypes || EmployeeTypeId.HasValue;

        public bool CanAccess(short employeeTypeId) =>
            ViewAllEmployeeTypes || EmployeeTypeId == employeeTypeId;
    }
}
