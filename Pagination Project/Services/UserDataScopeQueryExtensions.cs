using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public static class UserDataScopeQueryExtensions
    {
        public static IQueryable<Empleados> ApplyScope(
            this IQueryable<Empleados> query,
            UserDataScope scope)
        {
            return scope.ViewAllEmployeeTypes
                ? query
                : query.Where(x => x.EmployeeTypeId == scope.EmployeeTypeId);
        }

        public static IQueryable<Libros> ApplyScope(
            this IQueryable<Libros> query,
            UserDataScope scope)
        {
            return scope.ViewAllEmployeeTypes
                ? query
                : query.Where(x => x.EmployeeTypeId == scope.EmployeeTypeId);
        }

        public static IQueryable<Asignaciones> ApplyScope(
            this IQueryable<Asignaciones> query,
            UserDataScope scope)
        {
            return scope.ViewAllEmployeeTypes
                ? query
                : query.Where(x => x.Libro != null &&
                                   x.Libro.EmployeeTypeId == scope.EmployeeTypeId);
        }

        public static IQueryable<Evaluaciones> ApplyScope(
            this IQueryable<Evaluaciones> query,
            UserDataScope scope)
        {
            return scope.ViewAllEmployeeTypes
                ? query
                : query.Where(x => x.Asignacion != null &&
                                   x.Asignacion.Libro != null &&
                                   x.Asignacion.Libro.EmployeeTypeId == scope.EmployeeTypeId);
        }
    }
}
