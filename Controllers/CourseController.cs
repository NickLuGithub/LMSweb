﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using LMSweb.Models;
using LMSweb.ViewModel;
using System.Security.Claims;
using System.Web.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using LMSweb.Infrastructure.Helpers;

namespace LMSweb.Controllers
{
    [RoutePrefix("Teacher")]

    public class CourseController : Controller
    {
        private LMSmodel db = new LMSmodel();
        
        public ActionResult StudentDetails(string sid,string cid)
        {
            if (sid == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Student student = db.Students.Find(sid, cid);
            if (student == null)
            {
                return HttpNotFound();
            }
            return View(student);
        }

        // GET: Student/Create
        public ActionResult StudentCreate(string cid)
        {
            var s = new Student();
            s.CID = cid;

            
            return View(s);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StudentCreate(Student student, string cid)
        {
            if (ModelState.IsValid)
            {

                student.CID = cid;
                db.Students.Add(student);
                db.SaveChanges();
                return RedirectToAction("StudentManagement",new { cid});
            }

            return View(student);
        }
        public ActionResult StudentEdit(string sid)
        {
            if (sid == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Student student = db.Students.Find(sid);
            if (student == null)
            {
                return HttpNotFound();
            }
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StudentEdit([Bind(Include = "SID,CID,SName,SPassword,Sex,Score")] Student student)
        {
            if (ModelState.IsValid)
            {
                db.Entry(student).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("StudentManagement", "Course", new { student.CID});
            }
            return View(student);
        }

        // GET: Student/Delete/5
        public ActionResult StudentDelete(string sid)
        {
            if (sid == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Student student = db.Students.Find(sid);
            if (student == null)
            {
                return HttpNotFound();
            }
            return View(student);
        }
        [HttpPost, ActionName("StudentDelete")]
        [ValidateAntiForgeryToken]
        public ActionResult StudentDeleteConfirmed(string sid)
        {
            Student student = db.Students.Find(sid);
            db.Students.Remove(student);
            db.SaveChanges();
            return RedirectToAction("StudentManagement", "Course", new { student.CID });
        }

        private string fileSavedPath = WebConfigurationManager.AppSettings["UploadPath"];

        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase file, string cid)
        {
            JObject jo = new JObject();
            string result = string.Empty;

            if (file == null)
            {
                jo.Add("Result", false);
                jo.Add("Msg", "請上傳檔案!");
                jo.Add("error", "請上傳檔案!");
                result = JsonConvert.SerializeObject(jo);
                return Content(result, "application/json");
            }
            if (file.ContentLength <= 0)
            {
                jo.Add("Result", false);
                jo.Add("Msg", "請上傳正確的檔案.");
                jo.Add("error", "請上傳正確的檔案.");
                result = JsonConvert.SerializeObject(jo);
                return Content(result, "application/json");
            }

            string fileExtName = Path.GetExtension(file.FileName).ToLower();

            if (!fileExtName.Equals(".xls", StringComparison.OrdinalIgnoreCase)
                &&
                !fileExtName.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                jo.Add("Result", false);
                jo.Add("Msg", "請上傳 .xls 或 .xlsx 格式的檔案");
                jo.Add("error", "請上傳 .xls 或 .xlsx 格式的檔案");
                result = JsonConvert.SerializeObject(jo);
                return Content(result, "application/json");
            }

            try
            {
                var uploadResult = this.FileUploadHandler(file);
                jo.Add("Result", !string.IsNullOrWhiteSpace(uploadResult));
                jo.Add("Msg", !string.IsNullOrWhiteSpace(uploadResult) ? uploadResult : "");
                //result = this.Import(uploadResult, cid);


                //jo.Add("Result", !string.IsNullOrWhiteSpace(uploadResult));
                //jo.Add("Msg", !string.IsNullOrWhiteSpace(uploadResult) ? uploadResult : "");

                result = JsonConvert.SerializeObject(jo);
            }
            catch (Exception ex)
            {
                jo.Add("Result", false);
                jo.Add("Msg", ex.Message);
                jo.Add("error", ex.Message);
                result = JsonConvert.SerializeObject(jo);
            }
            return Json(result, "application/json");
        }


        private string FileUploadHandler(HttpPostedFileBase file)
        {
            string result;

            if (file == null)
            {
                throw new ArgumentNullException("file", "上傳失敗：沒有檔案！");
            }
            if (file.ContentLength <= 0)
            {
                throw new InvalidOperationException("上傳失敗：檔案沒有內容！");
            }

            try
            {
                string virtualBaseFilePath = Url.Content(fileSavedPath);
                string filePath = HttpContext.Server.MapPath(virtualBaseFilePath);

                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                string newFileName = string.Concat(
                    DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                    Path.GetExtension(file.FileName).ToLower());

                string fullFilePath = Path.Combine(Server.MapPath(fileSavedPath), newFileName);
                file.SaveAs(fullFilePath);

                result = newFileName;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        private ActionResult Import(string savedFileName, string cid)
        {
            var jo = new JObject();
            string result;
            try
            {
                var fileName = string.Concat(Server.MapPath(fileSavedPath), "/", savedFileName);

                var importStudents = new List<Student>();

                var helper = new ImportDataHelper();
                var checkResult = helper.CheckImportData(fileName, importStudents);
                jo.Add("Result", checkResult.Success);
                jo.Add("Msg", checkResult.Success ? string.Empty : checkResult.ErrorMessage);

                if (checkResult.Success)
                {
                    //儲存匯入的資料
                    helper.SaveImportData(importStudents, cid);
                }
                result = JsonConvert.SerializeObject(jo);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Content(result, "application/json");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        private List<SelectListItem> GetStudent(string cid, IEnumerable<int> SelectStudentList = null)
        {
            return new MultiSelectList(db.Students.Where(x => x.@group == null && x.CID == cid), "SID", "SName", SelectStudentList).ToList();
        }

        [HttpGet]
        public ActionResult StudentGroup(string cid)
        {            
            var vmodel = new GroupCreateViewModel();
            vmodel.StudentList = GetStudent(cid);
            vmodel.students = db.Students.Where(x => x.@group != null && x.CID == cid).ToList();
            vmodel.CID = cid;
            
            var course = db.Courses.Where(c => c.CID == cid).Single();
            vmodel.CName = course.CName;

            vmodel.groups = db.Groups.Where(g =>g.CID == cid).ToList();

            //var result = from g in db.Groups
            //             from s in db.Students
            //             where g.GID == s.@group.GID
            //             select new { s.SName, s.@group.GName };

            return View(vmodel);
        }

        [HttpPost]
        public ActionResult StudentGroup(string GName, List<string> StudentList, string cid)
        {
            if (ModelState.IsValid)
            {
                Group group = new Group();
                group.GName = GName;
                group.CID = cid;
                group.Students = (ICollection <Student>)db.Students.Where(x => StudentList.Contains(x.SID)).ToList();
                db.Groups.Add(group);
                db.SaveChanges();
                return new HttpStatusCodeResult(200);
            }
            var vmodel = new GroupCreateViewModel();
            vmodel.StudentList = GetStudent(vmodel.CID);

            return View(vmodel);
        }
        

        public ActionResult StudentManagement(string cid)
        {

            if (cid == null)
            {
                return new HttpStatusCodeResult(404);
            }
            CourseViewModel model = new CourseViewModel();
            var course = db.Courses.Single(c => c.CID == cid);
            model.CID = course.CID;
            model.CName = course.CName;
            model.students = course.Students;
            ViewBag.CID = cid;
            

            return View(model);
        }

        [HttpPost]
        public ActionResult GroupN(int n, string cid)
        {
            
            var stus = GetRandomElements(db.Students.Where(x => x.@group == null && cid == x.CID).ToList());
            List<Group> groups = new List<Group>();
            var left_s = stus.Count % n;
            for(int i = 1; i <= n; i++)
            {
                var g = new Group();
                g.GName = "第" + i.ToString() + "組"+"_"+ g.GID;
                g.Students = new List<Student>();
                g.CID = cid;
                groups.Add(g);
            }
            int g_idx = 0;
            for(int i = 0; i < stus.Count; i++)
            {
                groups[g_idx].Students.Add(stus[i]);
                g_idx++;
                g_idx = g_idx % n;
            }
            for (int i = 0; i < n; i++)
            {
                db.Groups.Add(groups[i]);
            }
            db.SaveChanges();
            return RedirectToAction("StudentGroup", new { cid });

        }

        public static List<t> GetRandomElements<t>(IEnumerable<t> list)
        {
            return list.OrderBy(x => Guid.NewGuid()).ToList();
        }

        public ActionResult GroupDelete(int groupId)
        {

            Group group = db.Groups.Find(groupId);
            if (group == null)
            {
                return HttpNotFound();
            }
            var cid = group.CID;
            group.Students.Clear();
            db.Groups.Remove(group);

            db.SaveChanges();

            return RedirectToAction("StudentGroup", new { cid=cid} );
        }

        public ActionResult GroupStudentDelete(string groupStuId)
        {
            Student student = db.Students.Find(groupStuId);

            if (student == null)
            {
                return HttpNotFound();
            }
            Group group = student.group;
            group.Students.Remove(student);
            student.group = null;


            db.SaveChanges();

            return new HttpStatusCodeResult(200);
        }
        public ActionResult AddStuToOtherGroup(int gid, List<string> StudentList, string CID)
        {
            if (ModelState.IsValid)
            {
                //Group group = db.Groups.Find(gid);
                foreach(var sid in StudentList)
                {
                    Student student = db.Students.Find(sid);
                    
                    Group group = db.Groups.Find(gid);
                    //student.group = group;
                    group.Students.Add(student);
 
                }
                

                //group.Students = (ICollection<Student>)db.Students.Where(x => StudentList.Contains(x.SID)).ToList();
                //db.Groups.Add(group);

                
                db.SaveChanges();
                return new HttpStatusCodeResult(200);
            }
            var vmodel = new GroupCreateViewModel();
            vmodel.StudentList = GetStudent(vmodel.CID);

            return View(vmodel);                                                                                                                                                                                                         

        }

    }
}
