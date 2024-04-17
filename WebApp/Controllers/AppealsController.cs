using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApp.Domain;

namespace WebApp.Controllers
{
    public class AppealsController(IMemoryCache cache) : Controller
    {
        // Get list containing all appeals from cache
        private List<Appeal> GetAppeals()
        {
            List<Appeal>? appeals;
            if (!cache.TryGetValue("ActiveAppeals", out appeals))
            {
                appeals = new List<Appeal>();
            }

            return appeals!;
        }
        
        // Show all the active appeals
        public IActionResult Index()
        {
            var appeals = GetAppeals();
            
            appeals = appeals
                .Where(a => !a.IsResolved)
                .OrderByDescending(a => a.ResolutionDeadline)
                .ToList();

            return View(appeals);
        }

        // Open form for creating new appeal
        public IActionResult Create()
        {
            return View();
        }

        // Create new appeal
        [HttpPost]
        public IActionResult Create(Appeal appeal)
        {
            if (ModelState.IsValid)
            {
                appeal.Id = Guid.NewGuid();
                appeal.IsResolved = false;
                appeal.EntryTime = DateTime.Now;
                

                var appeals = GetAppeals();
                appeals.Add(appeal);
                cache.Set("ActiveAppeals", appeals);

                return RedirectToAction(nameof(Index));
            }
            
            return View(appeal);
        }

        // Mark appeal with passed id parameter as resolved
        public IActionResult Resolve(Guid id)
        {
            var appeals = GetAppeals();

            var appeal = appeals.FirstOrDefault(a => a.Id == id);
            if (appeal != null)
            {
                appeal.IsResolved = true;
                cache.Set("ActiveAppeals", appeals);
            }

            return RedirectToAction(nameof(Index));
        }

        // Delete an appeal
        public IActionResult Delete(Guid id)
        {
            var appeals = GetAppeals();

            var appeal = appeals.FirstOrDefault(a => a.Id == id);
            if (appeal != null)
            {
                appeals.Remove(appeal);
                cache.Set("ActiveAppeals", appeals);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
