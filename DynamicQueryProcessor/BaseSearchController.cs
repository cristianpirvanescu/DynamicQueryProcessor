using System.Data.Entity;
using System.Linq;
using DynamicQueryProcessor.ViewModels;
using System.Data;
using AutoMapper;
using System.Web.Mvc;
using DynamicQueryProcessor.QueryHelpers;

namespace DynamicQueryProcessor.Controllers
{
    public abstract class BaseSearchController<TModel, TViewModel, TContext> : Controller where TViewModel : class, IBaseSearchViewModel, new() where TModel: class where TContext : DbContext
    {
        protected readonly DbContext _context;
        protected readonly IMapper _mapper;
        public BaseSearchController(TContext context, IMapper mapper) 
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpPost]
        public ActionResult GetAll(GridRequestViewModel<TViewModel> requestModel)
        {
            var query = _context.Set<TModel>().AsQueryable();
            AddSpecificQuery(ref query);
            query = query.ProcessDinamicQuery(requestModel);
            
            var noOfPages = query.Count() / requestModel.Pagination.Number;
            var result = query.Skip(requestModel.Pagination.Start).Take(requestModel.Pagination.Number).ToList();
            var models = result.Select(q => _mapper.Map<TViewModel>(q)).ToList();
            var jsonResult = Json(new { Data = models, NoOfPages = noOfPages }, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        protected virtual void AddSpecificQuery(ref IQueryable<TModel> query)
        {
        }
    }
}