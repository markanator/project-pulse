using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheBugTracker.Extensions;
using TheBugTracker.Models;
using TheBugTracker.Models.Enums;
using TheBugTracker.Services.Interfaces;
using TheBugTracker.ViewModels;

namespace TheBugTracker.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly IRoleService _rolesService;
        private readonly ILookupService _lookupService;
        private readonly IFileService _fileService;
        private readonly IProjectService _projectService;
        private readonly UserManager<BTUser> _userManagerService;
        private readonly ICompanyInfoService _companyInfoService;

        public ProjectsController(IRoleService roles, ILookupService lookup, IFileService file, IProjectService project, UserManager<BTUser> userManager, ICompanyInfoService companyInfo)
        {
            _rolesService = roles;
            _lookupService = lookup;
            _fileService = file;
            _projectService = project;
            _userManagerService = userManager;
            _companyInfoService = companyInfo;
        }

        // GET: MyProjects
        public async Task<IActionResult> MyProjects()
        {
            string userId = _userManagerService.GetUserId(User);
            List<Project> projects = await _projectService.GetUserProjectsAsync(userId);

            return View(projects);
        }

        // GET: /Projects/AllProjects
        public async Task<IActionResult> AllProjects()
        {

            List<Project> projects = new();
            if (User.IsInRole(nameof(Roles.Admin)) || User.IsInRole(nameof(Roles.Admin)))
            {
                projects = await _companyInfoService.GetAllProjectsAsync(User.Identity.GetCompanyId().Value);
            }
            else
            {
                projects = await _projectService.GetAllProjectsByCompanyAsync(User.Identity.GetCompanyId().Value);
            }

            return View(projects);
        }

        // GET: /Projects/ArchivedProjects
        public async Task<IActionResult> ArchivedProjects()
        {
            List<Project> projects = await _projectService.GetArchivedProjectsByCompanyAsync(User.Identity.GetCompanyId().Value);

            return View(projects);
        }

        // GET: Projects/UnassignedProjects
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnassignedProjects()
        {
            var projects = await _projectService.GetUnassignedProjectsAsync(User.Identity.GetCompanyId().Value);

            return View(projects);
        }

        // Get: Projects/AssignPM
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignPM(int id)
        {
            AssignPMViewModel model = new();
            model.Project = await _projectService.GetProjectByIdAsync(id, User.Identity.GetCompanyId().Value);
            model.ProjectManagerList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(Roles.ProjectManager), User.Identity.GetCompanyId().Value), "Id", "FullName");

            return View(model);
        }

        // Get: Projects/AssignPM
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPM(AssignPMViewModel model)
        {
            if (!string.IsNullOrEmpty(model.PMID))
            {
                await _projectService.AddProjectManagerAsync(model.PMID, model.Project.Id);

                return RedirectToAction(nameof(Details), new { id = model.Project.Id });
            }

            return RedirectToAction(nameof(AssignPM), new { id = model.Project.Id });
        }


        // GET: /Projects/AssignMembers
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> AssignMembers(int id)
        {
            var model = new ProjectMembersViewModel();
            model.Project = await _projectService.GetProjectByIdAsync(id, User.Identity.GetCompanyId().Value);

            List<BTUser> devs = await _rolesService.GetUsersInRoleAsync(nameof(Roles.Developer), User.Identity.GetCompanyId().Value);
            List<BTUser> submitters = await _rolesService.GetUsersInRoleAsync(nameof(Roles.Submitter), User.Identity.GetCompanyId().Value);
            // combine above fetch members
            List<BTUser> companyMembers = devs.Concat(submitters).ToList();
            // preselected members already in project
            List<string> ogProjectMembers = model.Project.Members.Select(m => m.Id).ToList();

            model.Members = new MultiSelectList(companyMembers, "Id", "FullName", ogProjectMembers);

            return View(model);
        }

        // POST: /Projects/AssignMembers
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignMembers(ProjectMembersViewModel model)
        {
            try
            {
                if (model.SelectedMembers != null)
                {
                    // fetch current project members except PM
                    List<string> memberIds = (await _projectService.GetAllProjectMembersExceptPMAsync(model.Project.Id)).Select(m => m.Id).ToList();
                    // remove EXISTING MEMBERS
                    foreach (string memberId in memberIds)
                    {
                        await _projectService.RemoveUserFromProjectAsync(memberId, model.Project.Id);
                    }

                    foreach (var member in model.SelectedMembers)
                    {
                        await _projectService.AddUserToProjectAsync(member, model.Project.Id);
                    }

                    return RedirectToAction(nameof(Details), "Projects", new { id = model.Project.Id });
                }

                return RedirectToAction(nameof(AssignMembers), new { id = model.Project.Id });
            }
            catch (Exception)
            {

                throw;
            }
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _projectService.GetProjectByIdAsync(id.Value, User.Identity.GetCompanyId().Value);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Projects/Create
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Create()
        {
            int companyId = User.Identity.GetCompanyId().Value;

            AddProjectWithPMViewModel vm = new();
            // load selectLists for PMs and PriorityLists
            vm.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(Roles.ProjectManager.ToString(), companyId), "Id", "FullName");
            vm.PriorityList = new SelectList(await _lookupService.GetAllProjectPrioritiesAsync(), "Id", "Name");

            return View(vm);
        }

        // POST: Projects/Create
        [HttpPost, Authorize(Roles = "Admin, ProjectManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddProjectWithPMViewModel model)
        {
            if (model != null)
            {
                int companyId = User.Identity.GetCompanyId().Value;
                try
                {
                    if (model.Project?.ImageFormFile != null)
                    {
                        // add aditional information if available
                        model.Project.ImageFileData = await _fileService.ConverFileToByteArrayAsync(model.Project.ImageFormFile);
                        model.Project.ImageFileName = model.Project.ImageFormFile.FileName;
                        model.Project.ImageContentType = model.Project.ImageFormFile.ContentType;
                    }
                    model.Project.CompanyId = companyId;
                    model.Project.StartDate = model.Project.StartDate.ToUniversalTime();
                    model.Project.EndDate = model.Project.EndDate.ToUniversalTime();
                    // save changes
                    await _projectService.AddNewProjectAsync(model.Project);

                    // add PM if no one was chosen
                    if (!string.IsNullOrEmpty(model.PmId))
                    {
                        await _projectService.AddProjectManagerAsync(model.PmId, model.Project.Id);
                    }
                }
                catch (Exception)
                {

                    throw;
                }
                // TODO: redirect to all projects
                return RedirectToAction(nameof(AllProjects));
            }
            return RedirectToAction("Create");
        }

        // GET: Projects/Edit/5
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Edit(int? id)
        {
            int companyId = User.Identity.GetCompanyId().Value;
            AddProjectWithPMViewModel vm = new();

            vm.Project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

            // load selectLists for PMs and PriorityLists
            vm.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(Roles.ProjectManager.ToString(), companyId), "Id", "FullName");
            vm.PriorityList = new SelectList(await _lookupService.GetAllProjectPrioritiesAsync(), "Id", "Name");

            return View(vm);
        }

        // POST: Projects/Edit/5
        [HttpPost, Authorize(Roles = "Admin, ProjectManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AddProjectWithPMViewModel model)
        {
            if (model != null)
            {
                try
                {
                    if (model.Project?.ImageFormFile != null)
                    {
                        // add aditional information if available
                        model.Project.ImageFileData = await _fileService.ConverFileToByteArrayAsync(model.Project.ImageFormFile);
                        model.Project.ImageFileName = model.Project.ImageFormFile.FileName;
                        model.Project.ImageContentType = model.Project.ImageFormFile.ContentType;
                    }

                    model.Project.StartDate = model.Project.StartDate.ToUniversalTime();
                    model.Project.EndDate = model.Project.EndDate.ToUniversalTime();
                    // save changes
                    await _projectService.UpdateProjectAsync(model.Project);

                    // add PM if no one was chosen
                    if (!string.IsNullOrEmpty(model.PmId))
                    {
                        await _projectService.AddProjectManagerAsync(model.PmId, model.Project.Id);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await this.ProjectExists(model.Project.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                // TODO: redirect to all projects
                return RedirectToAction(nameof(AllProjects));
            }

            return RedirectToAction(nameof(Edit));
        }

        // GET: Projects/Delete/5
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Archive(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var companyId = User.Identity.GetCompanyId().Value;
            var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Delete/5
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpPost, ActionName("Archive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveConfirmed(int id)
        {
            var companyId = User.Identity.GetCompanyId().Value;
            var project = await _projectService.GetProjectByIdAsync(id, companyId);

            await _projectService.ArchiveProjectAsync(project);

            return RedirectToAction(nameof(AllProjects));
        }

        // GET: Projects/Restore/5
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Restore(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _projectService.GetProjectByIdAsync(id.Value, User.Identity.GetCompanyId().Value);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Restore/5
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpPost, ActionName("Restore")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreConfirmed(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id, User.Identity.GetCompanyId().Value);
            await _projectService.RestoreProjectAsync(project);

            return RedirectToAction(nameof(AllProjects));
        }

        private async Task<bool> ProjectExists(int id)
        {
            return (await _projectService.GetArchivedProjectsByCompanyAsync(User.Identity.GetCompanyId().Value)).Any(p => p.Id == id);
        }
    }
}
