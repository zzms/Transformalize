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
using System.Linq;
using Transformalize.Configuration;
using Transformalize.Libs.EnterpriseLibrary.Validation;
using Transformalize.Libs.EnterpriseLibrary.Validation.Validators;
using Transformalize.Libs.NLog;
using Transformalize.Libs.Rhino.Etl;

namespace Transformalize.Main {

    public class EntityConfigurationLoader {

        private const string DEFAULT = "[default]";
        private readonly Logger _log = LogManager.GetLogger("tfl");
        private readonly Process _process;

        public EntityConfigurationLoader(Process process) {
            _process = process;
        }

        public Entity Read(EntityConfigurationElement element, short entityIndex) {

            Validate(element);

            var threading = _process.PipelineThreading;

            if (!string.IsNullOrEmpty(element.PipelineThreading)) {
                PipelineThreading elementThreading;
                if (Enum.TryParse(element.PipelineThreading, true, out elementThreading)) {
                    threading = elementThreading;
                }
            }

            var entity = new Entity() {
                ProcessName = _process.Name,
                Schema = element.Schema,
                PipelineThreading = threading,
                Name = element.Name,
                Prefix = element.Prefix,
                Group = element.Group,
                Delete = element.Delete,
                PrependProcessNameToOutputName = element.PrependProcessNameToOutputName,
                Sample = element.Sample,
                Top = element.Top,
                DetectChanges = element.DetectChanges,
                TrimAll = element.TrimAll,
                NoLock = element.NoLock,
                Unicode = element.Unicode,
                VariableLength = element.VariableLength,
                SqlOverride = element.SqlOverride.Sql,
                SqlKeysOverride = element.SqlKeysOverride.Sql,
                Alias = string.IsNullOrEmpty(element.Alias) ? element.Name : element.Alias,
                InputOperation = element.InputOperation,
                Index = entityIndex
            };

            GuardAgainstInvalidGrouping(element, entity);
            GuardAgainstMissingPrimaryKey(element);
            GuardAgainstNoFields(element, entityIndex, entity);

            short autoIndex = 0;

            foreach (FieldConfigurationElement f in element.Fields) {
                var fieldType = GetFieldType(f, entityIndex == 0);

                var field = new FieldReader(_process, entity).Read(f, fieldType);
                if (field.Index.Equals(short.MaxValue)) {
                    field.Index = autoIndex;
                }

                entity.Fields.Add(field);

                if (f.PrimaryKey) {
                    entity.PrimaryKey.Add(field);
                }

                autoIndex++;
            }

            foreach (FieldConfigurationElement cf in element.CalculatedFields) {
                var fieldReader = new FieldReader(_process, entity, usePrefix: false);
                var fieldType = GetFieldType(cf, entityIndex == 0);
                var field = fieldReader.Read(cf, fieldType);

                if (field.Index.Equals(short.MaxValue)) {
                    field.Index = autoIndex;
                }

                field.IsCalculated = true;
                field.Input = false;
                entity.CalculatedFields.Add(field);
                if (cf.PrimaryKey) {
                    entity.PrimaryKey.Add(field);
                }
                autoIndex++;
            }

            LoadVersion(element, entity);

            // wire up connections
            if (!string.IsNullOrEmpty(element.Connection) && _process.Connections.ContainsKey(element.Connection)) {
                var connection = _process.Connections[element.Connection];
                entity.Input.Add(new NamedConnection() { Connection = connection, Name = connection.Name });
            }
            entity.Input.AddRange(PrepareIo(element.Input, entity.Fields));
            entity.Output = PrepareIo(element.Output, entity.Fields);

            return entity;
        }

        private void GuardAgainstNoFields(EntityConfigurationElement element, short entityIndex, Entity entity)
        {
            if (_process.Options.Mode != "metadata" && element.Fields.Count == 0 && _process.Connections.ContainsKey(element.Connection))
            {
                try
                {
                    _log.Info("Detecting fields.");
                    var connection = _process.Connections[element.Connection];
                    var schema = connection.GetEntitySchema(entity.Name, entity.Schema, entityIndex == 0);
                    if (schema.Fields.Any())
                    {
                        foreach (var field in schema.Fields)
                        {
                            var f = new FieldConfigurationElement {
                                                                      Type = field.Type,
                                                                      Length = field.Length,
                                                                      PrimaryKey = field.FieldType.Equals(FieldType.PrimaryKey) || field.FieldType.Equals(FieldType.MasterKey),
                                                                      Output = true,
                                                                      Default = string.Empty,
                                                                      Name = field.Name,
                                                                      Input = true,
                                                                      Precision = field.Precision,
                                                                      Scale = field.Scale
                                                                  };
                            element.Fields.Add(f);
                        }
                        _log.Info("Detected {0} fields.", schema.Fields.Count);
                    }
                }
                catch (Exception ex)
                {
                    throw new TransformalizeException("No fields defined.  Unable to detect them. {0}", ex.Message);
                }
                finally
                {
                    if (element.Fields.Count == 0)
                    {
                        throw new TransformalizeException("No fields defined.  Unable to detect them.");
                    }
                }
            }
        }

        private List<NamedConnection> PrepareIo(IoElementCollection collection, Fields fields) {
            var namedConnections = new List<NamedConnection>();

            var validator = ValidationFactory.CreateValidator<IoConfigurationElement>();
            foreach (IoConfigurationElement io in collection) {
                var results = validator.Validate(io);

                if (results.IsValid) {
                    if (_process.Connections.ContainsKey(io.Connection)) {
                        Func<Row, bool> shouldRun = row => true;

                        if (!io.RunField.Equals(string.Empty)) {
                            var f = io.RunField;
                            var match = fields.Find(f).ToArray();
                            if (match.Length > 0) {
                                var field = match[0];
                                if (io.RunType.Equals(DEFAULT)) {
                                    io.RunType = field.SimpleType;
                                }
                                var op = (ComparisonOperator)Enum.Parse(typeof(ComparisonOperator), io.RunOperator, true);
                                var simpleType = Common.ToSimpleType(io.RunType);
                                var value = Common.ConversionMap[simpleType](io.RunValue);
                                shouldRun = row => Common.CompareMap[op](row[field.Alias], value);
                            } else {
                                _log.Warn("Field {0} specified in {1} output doesn't exist.  It will not affect the output.", io.RunField, io.Name);
                            }
                        }

                        namedConnections.Add(new NamedConnection {
                            Name = io.Name,
                            Connection = _process.Connections[io.Connection],
                            ShouldRun = shouldRun
                        });
                    } else {
                        _log.Warn("Can't add output {0} because connection {1} doesn't exist.", io.Name, io.Connection);
                    }
                } else {
                    _log.Warn("Output {0} is invalid.", io.Name);
                    foreach (var reason in results) {
                        _log.Warn(reason.Message);
                    }
                }
            }
            return namedConnections;
        }

        private void Validate(EntityConfigurationElement element) {
            var validator = ValidationFactory.CreateValidator<EntityConfigurationElement>();
            var results = validator.Validate(element);
            if (!results.IsValid) {
                foreach (var result in results) {
                    _log.Error(result.Message);
                }
                throw new TransformalizeException("Entity validation failed. See error log.");
            }
        }

        private void GuardAgainstMissingPrimaryKey(EntityConfigurationElement element) {

            if (element.Fields.Cast<FieldConfigurationElement>().Any(f => f.PrimaryKey))
                return;

            if (element.CalculatedFields.Cast<FieldConfigurationElement>().Any(cf => cf.PrimaryKey))
                return;

            _log.Info("Adding TflHashCode primary key for {0}.", element.Name);
            var pk = new FieldConfigurationElement {
                Name = "TflHashCode",
                Type = "System.Int32",
                PrimaryKey = true,
                Transforms = new TransformElementCollection {
                    new TransformConfigurationElement {Method = "concat", Parameter = "*"},
                    new TransformConfigurationElement {Method = "gethashcode"}
                }
            };
            element.CalculatedFields.Insert(pk);
            if (string.IsNullOrEmpty(element.Version)) {
                element.Version = "TflHashCode";
            }
        }

        private void LoadVersion(EntityConfigurationElement element, Entity entity) {
            if (String.IsNullOrEmpty(element.Version))
                return;

            if (entity.Fields.HaveField(element.Version)) {
                entity.Version = entity.Fields.Find(element.Version).First();
            } else {
                if (entity.CalculatedFields.HaveField(element.Version)) {
                    entity.Version = entity.CalculatedFields.Find(element.Version).First();
                } else {
                    throw new TransformalizeException("version field reference '{0}' is undefined in {1}.", element.Version, element.Name);
                }
            }
            entity.Version.Output = true;
        }

        private static void GuardAgainstInvalidGrouping(EntityConfigurationElement element, Entity entity) {
            if (entity.Group) {
                if (!element.Fields.Cast<FieldConfigurationElement>().Any(f => f.Output && string.IsNullOrEmpty(f.Aggregate)))
                    return;

                if (!element.CalculatedFields.Cast<FieldConfigurationElement>().Any(f => f.Output && string.IsNullOrEmpty(f.Aggregate)))
                    return;

                throw new TransformalizeException("Entity {0} is set to group, but not all your output fields have aggregate defined.", entity.Alias);
            }

            if (!element.Fields.Cast<FieldConfigurationElement>().Any(f => f.Output && !string.IsNullOrEmpty(f.Aggregate)))
                return;

            if (!element.CalculatedFields.Cast<FieldConfigurationElement>().Any(f => f.Output && !string.IsNullOrEmpty(f.Aggregate)))
                return;

            throw new TransformalizeException("Entity {0} is not set to group, but one of your output fields has an aggregate defined.", entity.Alias);
        }

        private static FieldType GetFieldType(FieldConfigurationElement element, bool isMaster) {
            FieldType fieldType;
            if (element.PrimaryKey) {
                fieldType = isMaster ? FieldType.MasterKey : FieldType.PrimaryKey;
            } else {
                fieldType = FieldType.NonKey;
            }
            return fieldType;
        }

    }
}