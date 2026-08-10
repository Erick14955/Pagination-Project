using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public static class UserDataScopeQueryExtensions
    {
        public static IQueryable<Empleados> ApplyScope(
            this IQueryable<Empleados> query,
            UserDataScope scope)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(scope);

            if (scope.ViewAllEmployeeTypes)
            {
                return query;
            }

            var employeeTypeId =
                ObtenerEmployeeTypeIdRequerido(scope);

            return query.Where(
                x => x.EmployeeTypeId == employeeTypeId);
        }

        public static IQueryable<Libros> ApplyScope(
            this IQueryable<Libros> query,
            UserDataScope scope)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(scope);

            if (scope.ViewAllEmployeeTypes)
            {
                return query;
            }

            var employeeTypeId =
                ObtenerEmployeeTypeIdRequerido(scope);

            return query.Where(
                x => x.EmployeeTypeId == employeeTypeId);
        }

        public static IQueryable<Asignaciones> ApplyScope(
            this IQueryable<Asignaciones> query,
            UserDataScope scope)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(scope);

            if (scope.ViewAllEmployeeTypes)
            {
                return query;
            }

            var employeeTypeId =
                ObtenerEmployeeTypeIdRequerido(scope);

            return query.Where(
                x => x.Libro!.EmployeeTypeId == employeeTypeId);
        }

        public static IQueryable<Evaluaciones> ApplyScope(
            this IQueryable<Evaluaciones> query,
            UserDataScope scope)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(scope);

            if (scope.ViewAllEmployeeTypes)
            {
                return query;
            }

            var employeeTypeId =
                ObtenerEmployeeTypeIdRequerido(scope);

            return query.Where(
                x =>
                    x.Asignacion!.Libro!.EmployeeTypeId ==
                    employeeTypeId);
        }

        private static short ObtenerEmployeeTypeIdRequerido(
            UserDataScope scope)
        {
            if (scope.EmployeeTypeId.HasValue)
            {
                return scope.EmployeeTypeId.Value;
            }

            throw new InvalidOperationException(
                "The current data scope does not contain an employee type.");
        }
    }
}