using Blog.Models;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Blog.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            using (var database = new BlogDbContext())
            {
                var articles = database.Articles.Include(a => a.Author).ToList();
                return View(articles);
            }
        }

        public ActionResult Search(string name)
        {
            using (var database = new BlogDbContext())
            {
                var article = database.Articles.Where(a => a.Title == name).Include(a => a.Author).ToList();
                if(article.Count == 0)
                    ModelState.AddModelError("name", "Result is empty.");

                return View("Index", article);
            }
        }

        [Authorize]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public ActionResult Create(Article article)
        {
            if (ModelState.IsValid)
            {
                using (var database = new BlogDbContext())
                {
                    var authorId = database.Users.Where(u => u.UserName == this.User.Identity.Name).First().Id;

                    article.AuthorId = authorId;

                    database.Articles.Add(article);
                    database.SaveChanges();

                    return RedirectToAction("Index");
                }
            }
            return View(article);
        }
        [HttpGet]
        public ActionResult Info(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            using (var db = new BlogDbContext())
            {
                var article = db.Articles.Where(a => a.Id == id).Include(a => a.Author).FirstOrDefault();

                if (article == null)
                {
                    ModelState.AddModelError("", "Article not found.");
                    return View("Error", ModelState.Values.SelectMany(v => v.Errors).ToList());
                }

                return View(article);
            }            
        }
        
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            using (var db = new BlogDbContext())
            {
                var article = db.Articles.Where(a => a.Id == id).Include(a => a.Author).FirstOrDefault();

                if (article == null)
                {
                    ModelState.AddModelError("", "Article not found.");
                    return View("Error", ModelState.Values.SelectMany(v => v.Errors).ToList());
                }

                db.Articles.Remove(article);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
        }
        
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            using (var db = new BlogDbContext())
            {
                var article = db.Articles.Where(a => a.Id == id).FirstOrDefault();

                if (article == null)
                {
                    ModelState.AddModelError("", "Article not found.");
                    return View("Error", ModelState.Values.SelectMany(v => v.Errors).ToList());
                }

                if (!IsUserAuthorizedToEdit(article))
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

                var model = new ArticleViewModel();
                model.Id = article.Id;
                model.Title = article.Title;
                model.Content = article.Content;

                return View(model);
            }
        }

        [HttpPost]
        public ActionResult Edit(ArticleViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var db = new BlogDbContext())
                {
                    var article = db.Articles.FirstOrDefault(a => a.Id == model.Id);

                    article.Title = model.Title;
                    article.Content = model.Content;
                    db.Entry(article).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
            }
            return View(model);
        }

        private bool IsUserAuthorizedToEdit(Article article)
        {
            bool isAdmin = this.User.IsInRole("Admin");
            bool isAuthor = article.IsAuthor(this.User.Identity.Name);

            return isAdmin || isAuthor;
        }
    }
}