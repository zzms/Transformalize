#region License

// /*
// Transformalize - Replicate, Transform, and Denormalize Your Data...
// Copyright (C) 2013 Dale Newman
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// */

#endregion

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Transformalize.Configuration;
using Transformalize.Extensions;
using Transformalize.Libs.Ninject;
using Transformalize.Libs.NLog;
using Transformalize.Libs.NLog.Config;
using Transformalize.Libs.NLog.Targets;
using Transformalize.Libs.NLog.Targets.Wrappers;
using Transformalize.Libs.NVelocity.App;
using Transformalize.Libs.RazorEngine;
using Transformalize.Main.Providers;
using Transformalize.Main.Providers.File;
using Transformalize.Main.Transform;

namespace Transformalize.Main {

    public class ProcessReader : IReader<Process> {

        private readonly ProcessConfigurationElement _element;
        private readonly Logger _log = LogManager.GetLogger("tfl");
        private readonly Options _options;
        private readonly string _processName = string.Empty;
        private Process _process;
        private readonly string[] _transformToFields = new[] { "fromxml", "fromregex", "fromjson", "fromsplit" };

        public ProcessReader(ProcessConfigurationElement process, ref Options options) {
            AddShortHandTransforms(process);
            _element = Adapt(process, _transformToFields);
            _processName = process.Name;
            _options = options;
        }

        public Process Read() {

            if (_element == null) {
                throw new TransformalizeException("Can't find a process named {0}.", _processName);
            }
            _process = new Process(_element.Name) {
                Options = _options,
                TemplateContentType = _element.TemplateContentType.Equals("raw") ? Encoding.Raw : Encoding.Html,
                Enabled = _element.Enabled,
                FileInspectionRequest = _element.FileInspection == null ? new FileInspectionRequest() : _element.FileInspection.GetInspectionRequest(),
                Star = _element.Star,
                View = _element.View,
                Mode = _element.Mode,
                StarEnabled = _element.StarEnabled,
                TimeZone = string.IsNullOrEmpty(_element.TimeZone) ? TimeZoneInfo.Local.Id : _element.TimeZone,
                PipelineThreading = (PipelineThreading)Enum.Parse(typeof(PipelineThreading), _element.PipelineThreading, true),
                Kernal = new StandardKernel(new NinjectBindings(_element))
            };

            // options mode overrides process node
            if (_options.Mode != Common.DefaultValue && _options.Mode != _process.Mode) {
                _process.Mode = _options.Mode;
            }
            _log.Info("Mode: {0}", _process.Mode);

            //shared across the process
            var connectionFactory = new ConnectionFactory(_process);
            foreach (ProviderConfigurationElement element in _element.Providers) {
                connectionFactory.Providers[element.Name.ToLower()] = element.Type;
            }
            _process.Connections = connectionFactory.Create(_element.Connections);
            if (!_process.Connections.ContainsKey("output")) {
                _log.Warn("No output connection detected.  Defaulting to internal.");
                _process.OutputConnection = connectionFactory.Create(new ConnectionConfigurationElement() { Name = "output", Provider = "internal" });
            } else {
                _process.OutputConnection = _process.Connections["output"];
            }

            //logs set after connections, because they may depend on them
            LoadLogConfiguration(_element, ref _process);
            SetLog(ref _process);

            _process.Scripts = new ScriptReader(_element.Scripts).Read();
            _process.Actions = new ActionReader(_process).Read(_element.Actions);
            _process.Templates = new TemplateReader(_process, _element.Templates).Read();
            _process.SearchTypes = new SearchTypeReader(_element.SearchTypes).Read();
            new MapLoader(ref _process, _element.Maps).Load();

            if (_process.Templates.Any(t => t.Value.Engine.Equals("velocity", StringComparison.OrdinalIgnoreCase))) {
                Velocity.Init();
                _process.VelocityInitialized = true;
            }

            //these depend on the shared process properties
            new EntitiesLoader(ref _process, _element.Entities).Load();
            new OperationsLoader(ref _process, _element.Entities).Load();

            _process.Relationships = new RelationshipsReader(_process, _element.Relationships).Read();
            new ProcessOperationsLoader(ref _process, _element.CalculatedFields).Load();
            new EntityRelationshipLoader(ref _process).Load();

            Summarize();

            return _process;
        }

        private static void LoadLogConfiguration(ProcessConfigurationElement element, ref Process process) {

            process.LogRows = element.Log.Rows;

            if (element.Log.Count == 0)
                return;

            if (process.Options.MemoryTarget != null) {
                process.Log.Add(new Log {
                    Provider = ProviderType.Internal,
                    Name = "memory",
                    MemoryTarget = process.Options.MemoryTarget,
                    Level = process.Options.LogLevel
                });
            }

            foreach (LogConfigurationElement logElement in element.Log) {
                var log = new Log {
                    Name = logElement.Name,
                    Subject = logElement.Subject,
                    From = logElement.From,
                    To = logElement.To,
                    Layout = logElement.Layout,
                    File = logElement.File
                };

                if (logElement.Connection != Common.DefaultValue) {
                    if (process.Connections.ContainsKey(logElement.Connection)) {
                        log.Connection = process.Connections[logElement.Connection];
                        if (log.Connection.Type == ProviderType.File && log.File.Equals(Common.DefaultValue)) {
                            log.File = log.Connection.File;
                        }
                    } else {
                        throw new TransformalizeException("You are referencing an invalid connection name in your log configuration.  {0} is not confiured in <connections/>.", logElement.Connection);
                    }
                }

                try {
                    log.Level = LogLevel.FromString(logElement.LogLevel);
                    log.Provider = (ProviderType)Enum.Parse(typeof(ProviderType), logElement.Provider, true);
                } catch (Exception ex) {
                    throw new TransformalizeException("Log configuration invalid. {0}", ex.Message);
                }
                process.Log.Add(log);
            }
        }

        public void SetLog(ref Process process) {

            if (process.Log.Count > 0) {
                var config = new LoggingConfiguration();

                foreach (var log in process.Log) {
                    switch (log.Provider) {
                        case ProviderType.Console:
                            //console
                            var consoleTarget = new ColoredConsoleTarget {
                                Name = log.Name,
                                Layout = log.Layout.Equals(Common.DefaultValue) ? @"${date:format=HH\:mm\:ss} | ${level} | " + process.Name + " | ${gdc:item=entity} | ${message}" : log.Layout
                            };
                            var consoleRule = new LoggingRule("tfl", log.Level, consoleTarget);
                            config.AddTarget(log.Name, consoleTarget);
                            config.LoggingRules.Add(consoleRule);
                            break;
                        case ProviderType.File:
                            var fileTarget = new AsyncTargetWrapper(new FileTarget {
                                Name = log.Name,
                                FileName = log.File.Equals(Common.DefaultValue) ? "${basedir}/logs/tfl-" + process.Name + "-${date:format=yyyy-MM-dd}.log" : log.File,
                                Layout = log.Layout.Equals(Common.DefaultValue) ? @"${date:format=HH\:mm\:ss} | ${level} | " + process.Name + " | ${gdc:item=entity} | ${message}" : log.Layout
                            });
                            var fileRule = new LoggingRule("tfl", log.Level, fileTarget);
                            config.AddTarget(log.Name, fileTarget);
                            config.LoggingRules.Add(fileRule);
                            break;
                        case ProviderType.Internal:
                            var memoryRule = new LoggingRule("tfl", log.Level, log.MemoryTarget);
                            config.LoggingRules.Add(memoryRule);
                            break;
                        case ProviderType.Mail:
                            if (log.Connection == null) {
                                throw new TransformalizeException("The mail logger needs to reference a mail connection in <connections/> collection.");
                            }
                            var mailTarget = new MailTarget {
                                Name = log.Name,
                                SmtpPort = log.Connection.Port.Equals(0) ? 25 : log.Connection.Port,
                                SmtpUserName = log.Connection.User,
                                SmtpPassword = log.Connection.Password,
                                SmtpServer = log.Connection.Server,
                                EnableSsl = log.Connection.EnableSsl,
                                Subject = log.Subject.Equals(Common.DefaultValue) ? "Tfl Error (" + process.Name + ")" : log.Subject,
                                From = log.From,
                                To = log.To,
                                Layout = log.Layout.Equals(Common.DefaultValue) ? @"${date:format=HH\:mm\:ss} | ${level} | ${gdc:item=entity} | ${message}" : log.Layout
                            };
                            var mailRule = new LoggingRule("tfl", LogLevel.Error, mailTarget);
                            config.AddTarget(log.Name, mailTarget);
                            config.LoggingRules.Add(mailRule);
                            break;
                        default:
                            throw new TransformalizeException("Log does not support {0} provider.", log.Provider);
                    }
                }

                LogManager.Configuration = config;
            }

            GlobalDiagnosticsContext.Set("process", Common.LogLength(process.Name));
            GlobalDiagnosticsContext.Set("entity", Common.LogLength("All"));
        }



        private static ProcessConfigurationElement Adapt(ProcessConfigurationElement process, IEnumerable<string> transformToFields) {

            foreach (var field in transformToFields) {
                while (new TransformFieldsToParametersAdapter(process).Adapt(field) > 0) {
                    new TransformFieldsMoveAdapter(process).Adapt(field);
                };
            }

            return process;
        }

        /// <summary>
        /// Converts t attribute to configuration items for the whole process
        /// </summary>
        /// <param name="process"></param>
        private static void AddShortHandTransforms(ProcessConfigurationElement process) {
            foreach (EntityConfigurationElement entity in process.Entities) {
                foreach (FieldConfigurationElement field in entity.Fields) {
                    AddShortHandTransforms(field);
                }
                foreach (FieldConfigurationElement field in entity.CalculatedFields) {
                    AddShortHandTransforms(field);
                }
            }
            foreach (FieldConfigurationElement field in process.CalculatedFields) {
                AddShortHandTransforms(field);
            }
        }

        /// <summary>
        /// Converts t attribute to configuration items for one field.
        /// </summary>
        /// <param name="f">the field</param>
        private static void AddShortHandTransforms(FieldConfigurationElement f) {
            var transforms = new List<TransformConfigurationElement>(Common.Split(f.ShortHand, ';').Where(t => !string.IsNullOrEmpty(t)).Select(ShortHandFactory.Interpret));
            var collection = new TransformElementCollection();
            foreach (var transform in transforms) {
                foreach (FieldConfigurationElement field in transform.Fields) {
                    AddShortHandTransforms(field);
                }
                collection.Add(transform);
            }
            foreach (TransformConfigurationElement transform in f.Transforms) {
                foreach (FieldConfigurationElement field in transform.Fields) {
                    AddShortHandTransforms(field);
                }
                collection.Add(transform);
            }
            f.Transforms = collection;
        }

        private void Summarize() {

            if (!_log.IsDebugEnabled)
                return;

            _log.Debug("Process Loaded.");
            _log.Debug("{0} Provider{1}.", _process.Providers.Count, _process.Providers.Count.Plural());
            _log.Debug("{0} Connection{1}.", _process.Connections.Count, _process.Connections.Count.Plural());
            _log.Debug("{0} Entit{1}.", _process.Entities.Count, _process.Entities.Count.Pluralize());
            _log.Debug("{0} Relationship{1}.", _process.Relationships.Count, _process.Relationships.Count.Plural());
            _log.Debug("{0} Script{1}.", _process.Scripts.Count, _process.Scripts.Count.Plural());
            _log.Debug("{0} Template{1}.", _process.Templates.Count, _process.Templates.Count.Plural());
            _log.Debug("{0} SearchType{1}.", _process.SearchTypes.Count, _process.SearchTypes.Count.Plural());
            _log.Debug("{0} Log{0}", _process.Log.Count, _process.Log.Count.Plural());

            var mapCount = _process.MapStartsWith.Count + _process.MapEquals.Count + _process.MapEndsWith.Count;
            _log.Debug("{0} Map{1}.", mapCount, mapCount.Plural());
        }
    }
}