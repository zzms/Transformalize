﻿#region license
// Transformalize
// Copyright 2013 Dale Newman
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//  
//      http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Core.Contents;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Themes;
using Orchard.UI.Notify;
using Transformalize.Contracts;
using Pipeline.Web.Orchard.Models;
using Pipeline.Web.Orchard.Services;
using System.IO;
using Orchard.Autoroute.Services;
using Orchard.FileSystems.AppData;
using Orchard.Services;
using Pipeline.Web.Orchard.Services.Contracts;
using Transformalize.Configuration;
using Transformalize.Extensions;
using Process = Transformalize.Configuration.Process;

namespace Pipeline.Web.Orchard.Controllers {

    [ValidateInput(false), Themed(true)]
    public class CfgController : Controller {

        const string FileTimestamp = "yyyy-MM-dd-HH-mm-ss";
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private static readonly HashSet<string> _renderedOutputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "map", "page" };

        private readonly IOrchardServices _orchardServices;
        private readonly IProcessService _processService;
        private readonly ISortService _sortService;
        private readonly ISecureFileService _secureFileService;
        private readonly ICfgService _cfgService;
        private readonly ISlugService _slugService;
        private readonly IAppDataFolder _appDataFolder;
        private readonly IClock _clock;
        private readonly IBatchCreateService _batchCreateService;
        private readonly IBatchWriteService _batchWriteService;
        private readonly IBatchRunService _batchRunService;
        private readonly IBatchRedirectService _batchRedirectService;
        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        public CfgController(
            IOrchardServices services,
            IProcessService processService,
            ISortService sortService,
            ISecureFileService secureFileService,
            ICfgService cfgService,
            ISlugService slugService,
            IAppDataFolder appDataFolder,
            IClock clock,
            IBatchCreateService batchCreateService,
            IBatchWriteService batchWriteService,
            IBatchRunService batchRunService,
            IBatchRedirectService batchRedirectService
        ) {
            _clock = clock;
            _appDataFolder = appDataFolder;
            _orchardServices = services;
            _processService = processService;
            _secureFileService = secureFileService;
            _cfgService = cfgService;
            _sortService = sortService;
            _slugService = slugService;
            _batchCreateService = batchCreateService;
            _batchWriteService = batchWriteService;
            _batchRunService = batchRunService;
            _batchRedirectService = batchRedirectService;
            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public ActionResult List(string tagFilter) {

            // Sticky Tag Filter
            if (Request.RawUrl.EndsWith("List") || Request.RawUrl.Contains("List?")) {
                tagFilter = Session[Common.TagFilterName] != null ? Session[Common.TagFilterName].ToString() : Common.AllTag;
            } else {
                Session[Common.TagFilterName] = tagFilter;
            }

            if (!User.Identity.IsAuthenticated)
                System.Web.Security.FormsAuthentication.RedirectToLoginPage(Request.RawUrl);

            var viewModel = new ConfigurationListViewModel(
                _cfgService.List(tagFilter),
                Common.Tags<PipelineConfigurationPart, PipelineConfigurationPartRecord>(_orchardServices),
                tagFilter
            );

            return View(viewModel);
        }

        public ActionResult Report(int id) {

            var timer = new Stopwatch();
            timer.Start();

            var process = new Process { Name = "Report" };

            var part = _orchardServices.ContentManager.Get(id).As<PipelineConfigurationPart>();
            if (part == null) {
                process.Name = "Not Found";
            } else {

                var user = _orchardServices.WorkContext.CurrentUser == null ? "Anonymous" : _orchardServices.WorkContext.CurrentUser.UserName ?? "Anonymous";

                if (_orchardServices.Authorizer.Authorize(Permissions.ViewContent, part)) {

                    process = _processService.Resolve(part.EditorMode, part.EditorMode);

                    var parameters = Common.GetParameters(Request, _secureFileService, _orchardServices);
                    if (part.NeedsInputFile && Convert.ToInt32(parameters[Common.InputFileIdName]) == 0) {
                        _orchardServices.Notifier.Add(NotifyType.Error, T("This transformalize expects a file."));
                        process.Name = "File Not Found";
                    }

                    process.Load(part.Configuration, parameters);
                    process.Buffer = false; // no buffering for reports
                    process.ReadOnly = true;  // force reporting to omit system fields

                    // secure actions
                    var actions = process.Actions.Where(a => !a.Before && !a.After && !a.Description.StartsWith("Batch", StringComparison.OrdinalIgnoreCase));
                    foreach (var action in actions) {
                        var p = _orchardServices.ContentManager.Get(action.Id);
                        if (!_orchardServices.Authorizer.Authorize(Permissions.ViewContent, p)) {
                            action.Description = "BatchUnauthorized";
                        }
                    }

                    var output = process.Output();

                    if (output.Provider.In("internal", "file")) {

                        Common.TranslatePageParametersToEntities(process, parameters, "page");

                        // change process for export and batch purposes
                        var reportType = Request["output"] ?? "page";
                        if (!_renderedOutputs.Contains(reportType)) {

                            if (reportType == "batch" && Request.HttpMethod.Equals("POST") && parameters.ContainsKey("action")) {

                                var action = process.Actions.FirstOrDefault(a => a.Description == parameters["action"]);

                                if (action != null) {

                                    // check security
                                    var actionPart = _orchardServices.ContentManager.Get(action.Id);
                                    if (actionPart != null && _orchardServices.Authorizer.Authorize(Permissions.ViewContent, actionPart)) {

                                        // security okay
                                        parameters["entity"] = process.Entities.First().Alias;
                                        var batchParameters = _batchCreateService.Create(process, parameters);

                                        batchParameters["count"] = parameters.ContainsKey("count") ? parameters["count"] : "0";
                                        var count = _batchWriteService.Write(Request, process, batchParameters);

                                        if (count > 0) {

                                            if (_batchRunService.Run(action, batchParameters)) {
                                                if (action.Url == string.Empty) {
                                                    if (batchParameters.ContainsKey("BatchId")) {
                                                        _orchardServices.Notifier.Information(T($"Processed {count} records in batch {batchParameters["BatchId"]}."));
                                                    } else {
                                                        _orchardServices.Notifier.Information(T($"Processed {count} records."));
                                                    }
                                                } else {
                                                    return _batchRedirectService.Redirect(action.Url, batchParameters);
                                                }
                                            }
                                        }
                                    } else {
                                        return new HttpUnauthorizedResult("You do not have access to this bulk action.");
                                    }
                                }

                            } else { // export
                                ConvertToExport(user, process, part, reportType, parameters);
                                process.Load(process.Serialize(), parameters);
                            }
                        }

                        if (Request["sort"] != null) {
                            _sortService.AddSortToEntity(process.Entities.First(), Request["sort"]);
                        }

                        if (process.Errors().Any()) {
                            foreach (var error in process.Errors()) {
                                _orchardServices.Notifier.Add(NotifyType.Error, T(error));
                            }
                        } else {
                            if (process.Entities.Any(e => !e.Fields.Any(f => f.Input))) {
                                _orchardServices.WorkContext.Resolve<ISchemaHelper>().Help(process);
                            }

                            if (!process.Errors().Any()) {

                                var runner = _orchardServices.WorkContext.Resolve<IRunTimeExecute>();
                                try {

                                    runner.Execute(process);
                                    process.Request = "Run";
                                    process.Time = timer.ElapsedMilliseconds;

                                    if (process.Errors().Any()) {
                                        foreach (var error in process.Errors()) {
                                            _orchardServices.Notifier.Add(NotifyType.Error, T(error));
                                        }
                                        process.Status = 500;
                                        process.Message = "There are errors in the pipeline.  See log.";
                                    } else {
                                        process.Status = 200;
                                        process.Message = "Ok";
                                    }

                                    var o = process.Output();
                                    switch (o.Provider) {
                                        case "kml":
                                        case "geojson":
                                        case "file":
                                            Response.AddHeader("content-disposition", "attachment; filename=" + o.File);
                                            switch (o.Provider) {
                                                case "kml":
                                                    Response.ContentType = "application/vnd.google-earth.kml+xml";
                                                    break;
                                                case "geojson":
                                                    Response.ContentType = "application/vnd.geo+json";
                                                    break;
                                                default:
                                                    Response.ContentType = "application/csv";
                                                    break;
                                            }
                                            Response.Flush();
                                            Response.End();
                                            return new EmptyResult();
                                        case "excel":
                                            return new FilePathResult(o.File, ExcelContentType) {
                                                FileDownloadName = _slugService.Slugify(part.Title()) + ".xlsx"
                                            };
                                        default:  // page and map are rendered to page
                                            break;
                                    }
                                } catch (Exception ex) {
                                    Logger.Error(ex, ex.Message);
                                    _orchardServices.Notifier.Error(T(ex.Message));
                                }
                            }
                        }
                    }
                } else {
                    _orchardServices.Notifier.Warning(user == "Anonymous" ? T("Sorry. Anonymous users do not have permission to view this report. You may need to login.") : T("Sorry {0}. You do not have permission to view this report.", user));
                }
            }

            return View(new ReportViewModel(process, part));

        }

        private void ConvertToExport(string user, Process process, PipelineConfigurationPart part, string exportType, IDictionary<string, string> parameters) {
            var o = process.Output();
            switch (exportType) {
                case "xlsx":
                    if (!_appDataFolder.DirectoryExists(Common.FileFolder)) {
                        _appDataFolder.CreateDirectory(Common.FileFolder);
                    }

                    var fileName = $"{user}-{_clock.UtcNow.ToString(FileTimestamp)}-{_slugService.Slugify(part.Title())}.xlsx";

                    o.Provider = "excel";
                    o.File = _appDataFolder.MapPath(_appDataFolder.Combine(Common.FileFolder, fileName));
                    break;
                case "geojson":
                    o.Stream = true;
                    o.Provider = "geojson";
                    o.File = _slugService.Slugify(part.Title()) + ".geojson";
                    break;
                case "kml":
                    o.Stream = true;
                    o.Provider = "kml";
                    o.File = _slugService.Slugify(part.Title()) + ".kml";
                    break;
                default: //csv
                    o.Stream = true;
                    o.Provider = "file";
                    o.Delimiter = ",";
                    o.File = _slugService.Slugify(part.Title()) + ".csv";
                    break;
            }

            parameters["page"] = "0";

            foreach (var entity in process.Entities) {

                entity.Page = 0;
                entity.Fields.RemoveAll(f => f.System);

                foreach (var field in entity.GetAllFields()) {
                    field.Output = field.Output && field.Export == "defer" || field.Export == "true";
                }
            }
        }

        [Themed(false)]
        [HttpGet]
        public ActionResult Download(int id) {

            var part = _orchardServices.ContentManager.Get(id).As<PipelineConfigurationPart>();

            var process = new Process { Name = "Export" };

            if (part == null) {
                process.Name = "Not Found";
                return new FileStreamResult(GenerateStreamFromString(process.Serialize()), "text/xml") { FileDownloadName = id + ".xml" };
            }

            if (!_orchardServices.Authorizer.Authorize(Permissions.ViewContent, part)) {
                process.Name = "Not Authorized";
                return new FileStreamResult(GenerateStreamFromString(process.Serialize()), "text/xml") { FileDownloadName = id + ".xml" };
            }

            return new FileStreamResult(GenerateStreamFromString(part.Configuration), "text/" + part.EditorMode) { FileDownloadName = _slugService.Slugify(part.Title()) + "." + part.EditorMode };

        }


        public Stream GenerateStreamFromString(string s) {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }


    }
}