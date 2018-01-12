using System;

namespace DynamicQueryProcessor.ViewModels
{
    public enum FilterOperatorViewModel
    {
        Contains = 0,
        Equals = 1,
        GreaterOrEquals = 2,
        LessOrEquals = 3
    }

    public class SortGridViewModel
    {
        public string Predicate { get; set; }
        public bool Reverse { get; set; }
    }
    
    public class PaginationGridViewModel
    {
        public int Number { get; set; } = 10;
        public int NumberOfPages { get; set; }
        public int Start { get; set; }
        public int TotalItemCount { get; set; }
    }

    public class SearchGridViewModel<T> where T : new()
    {
        public T PredicateObject { get; set; } = new T();
    }

    public class GridRequestViewModel<T> where T : new()
    {
        public PaginationGridViewModel Pagination { get; set; } = new PaginationGridViewModel();
        public SortGridViewModel Sort { get; set; } = new SortGridViewModel();
        public SearchGridViewModel<T> Search { get; set; } = new SearchGridViewModel<T>();
    }

}