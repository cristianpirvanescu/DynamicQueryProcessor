using DynamicQueryProcessor.ViewModels;
using System;

namespace DynamicQueryProcessor.FilterOperatorAttributes
{
    public class FilterOperatorAttribute : Attribute
    {
        public FilterOperatorAttribute(string dbField)
        {
            this.DbField = dbField;
        }
        public FilterOperatorAttribute(FilterOperatorViewModel oper, string dbField = null)
        {
            this.Operator = oper;
            this.DbField = dbField;
        }
        public FilterOperatorViewModel? Operator { get; set; }
        public string DbField { get; set; }
    }
}