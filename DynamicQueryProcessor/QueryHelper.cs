using DynamicQueryProcessor.FilterOperatorAttributes;
using DynamicQueryProcessor.ViewModels;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DynamicQueryProcessor.QueryHelpers
{
    public static class QueryHelper
    {
        private static readonly MethodInfo OrderByMethod =
            typeof(Queryable).GetMethods().Single(method =>
           method.Name == "OrderBy" && method.GetParameters().Length == 2);

        private static readonly MethodInfo OrderByDescendingMethod =
            typeof(Queryable).GetMethods().Single(method =>
           method.Name == "OrderByDescending" && method.GetParameters().Length == 2);
        public static bool PropertyExists<T>(string propertyName)
        {
            return typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase |
                BindingFlags.Public | BindingFlags.Instance) != null;
        }
        
        public static IQueryable<T> ProcessDinamicQuery<T, P>(
          this IQueryable<T> source, GridRequestViewModel<P> requestModel, char propertyNavigationSplitter = '_') where P : new()
        {
            var query = source;
            bool isSorted = false;
            foreach (var aProperty in requestModel.Search.PredicateObject.GetType().GetProperties())
            {
                if (aProperty.GetValue(requestModel.Search.PredicateObject) == null)
                    continue;
                var param = Expression.Parameter(typeof(T), "p");
                
                if (typeof(P).GetProperty(aProperty.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance).PropertyType == typeof(string))
                {

                    string[] props = aProperty.Name.Split(propertyNavigationSplitter);
                    if (!PropertyExists<T>(props[0]))
                        continue;
                    Type type = typeof(T);
                    ParameterExpression arg = Expression.Parameter(type, "x");
                    Expression expr = arg;
                    foreach (string prop in props)
                    {
                        PropertyInfo pi = type.GetProperty(prop);
                        expr = Expression.Property(expr, pi);
                        type = pi.PropertyType;
                    }

                    var attr = (FilterOperatorAttribute[])aProperty.GetCustomAttributes(typeof(FilterOperatorAttribute), false);
                    if (attr.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(attr[0].DbField))
                            expr = Expression.Property(arg, attr[0].DbField);
                    }
                    var value = Expression.Constant(aProperty.GetValue(requestModel.Search.PredicateObject));

                    var containsmethod = type.GetMethod("Contains", new[] { typeof(string) });
                    var call = Expression.Call(expr, containsmethod, value);
                    var lambda = Expression.Lambda<Func<T, bool>>(call, arg);
                    query = query.Where(lambda);
                }
                else
                {
                    string[] props = aProperty.Name.Split(propertyNavigationSplitter);
                    if (!PropertyExists<T>(props[0]))
                        continue;
                    Type type = typeof(T);
                    ParameterExpression arg = Expression.Parameter(type, "x");
                    Expression expr = arg;
                    foreach (string prop in props)
                    {
                        PropertyInfo pi = type.GetProperty(prop);
                        expr = Expression.Property(expr, pi);
                        type = pi.PropertyType;
                    }
                    var value = Expression.Constant(aProperty.GetValue(requestModel.Search.PredicateObject));

                    FilterOperatorViewModel _operator = FilterOperatorViewModel.Equals;
                    var attr = (FilterOperatorAttribute[])aProperty.GetCustomAttributes(typeof(FilterOperatorAttribute), false);
                    if (attr.Length > 0)
                    {
                        if (attr[0].Operator != null)
                            _operator = attr[0].Operator.Value;
                        if (!string.IsNullOrEmpty(attr[0].DbField))
                            expr = Expression.Property(arg, attr[0].DbField);
                    }

                    if (typeof(P).GetProperty(aProperty.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance).PropertyType == typeof(DateTime?))
                    {
                        var dateVal = ((DateTime)aProperty.GetValue(requestModel.Search.PredicateObject)).AddDays(1).AddTicks(-1);
                        if (_operator == FilterOperatorViewModel.GreaterOrEquals)
                            dateVal = dateVal.AddDays(-1);
                        value = Expression.Constant(dateVal);
                    }

                    BinaryExpression binaryExpression = Expression.Equal(Expression.Property(arg, aProperty.Name), value);
                    if (_operator == FilterOperatorViewModel.GreaterOrEquals)
                        binaryExpression = Expression.GreaterThanOrEqual(Expression.Property(arg, aProperty.Name), value);
                    if (_operator == FilterOperatorViewModel.LessOrEquals)
                        binaryExpression = Expression.LessThanOrEqual(Expression.Property(arg, aProperty.Name), value);

                    var exp = Expression.Lambda<Func<T, bool>>(binaryExpression, arg);
                    query = query.Where(exp);
                }
            }

            if (!string.IsNullOrEmpty(requestModel.Sort.Predicate))
            {
                isSorted = true;
                if (requestModel.Sort.Reverse)
                    query = query.OrderByDescending(requestModel.Sort.Predicate);
                else
                    query = query.OrderBy(requestModel.Sort.Predicate);
            }
            if (!isSorted)
                query = query.OrderBy("Id");
            return query;
        }

        public static IOrderedQueryable<T> OrderBy<T>(
            this IQueryable<T> source,
            string property, char propertyNavigationSplitter = '_')
        {
            return ApplyOrder<T>(source, property, "OrderBy");
        }

        public static IOrderedQueryable<T> OrderByDescending<T>(
            this IQueryable<T> source,
            string property, char propertyNavigationSplitter = '_')
        {
            return ApplyOrder<T>(source, property, "OrderByDescending");
        }

        public static IOrderedQueryable<T> ThenBy<T>(
            this IOrderedQueryable<T> source,
            string property, char propertyNavigationSplitter = '_')
        {
            return ApplyOrder<T>(source, property, "ThenBy");
        }

        public static IOrderedQueryable<T> ThenByDescending<T>(
            this IOrderedQueryable<T> source,
            string property, char propertyNavigationSplitter = '_')
        {
            return ApplyOrder<T>(source, property, "ThenByDescending");
        }

        static IOrderedQueryable<T> ApplyOrder<T>(
            IQueryable<T> source,
            string property,
            string methodName, char propertyNavigationSplitter = '_')
        {
            string[] props = property.Split(propertyNavigationSplitter);
            Type type = typeof(T);
            ParameterExpression arg = Expression.Parameter(type, "x");
            Expression expr = arg;
            foreach (string prop in props)
            {
                PropertyInfo pi = type.GetProperty(prop);
                expr = Expression.Property(expr, pi);
                type = pi.PropertyType;
            }
            Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
            LambdaExpression lambda = Expression.Lambda(delegateType, expr, arg);

            object result = typeof(Queryable).GetMethods().Single(
                    method => method.Name == methodName
                            && method.IsGenericMethodDefinition
                            && method.GetGenericArguments().Length == 2
                            && method.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(T), type)
                    .Invoke(null, new object[] { source, lambda });
            return (IOrderedQueryable<T>)result;
        }
    }
}