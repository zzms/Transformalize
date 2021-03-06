#region license
// Transformalize
// Configurable Extract, Transform, and Load
// Copyright 2013-2017 Dale Newman
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//       http://www.apache.org/licenses/LICENSE-2.0
//   
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using Cfg.Net;
using Cfg.Net.Contracts;
using Cfg.Net.Ext;
using Cfg.Net.Serializers;
using Transformalize.Configuration.Ext;
using Transformalize.Context;
using Transformalize.Extensions;
using Transformalize.Logging;
using System.Text.RegularExpressions;

namespace Transformalize.Configuration {

    [Cfg(name = "cfg")]
    public class Process : CfgNode, IDisposable {

        /// <summary>
        /// The default shorthand configuration for children processes
        /// </summary>
        [Cfg(value = "Shorthand.xml")]
        public string Shorthand { get; set; }


        private string _name;

        [Cfg(value = "")]
        public string Request { get; set; }

        [Cfg(value = (short)0)]
        public short Status { get; set; }

        [Cfg(value = "OK")]
        public string Message { get; set; }

        [Cfg(value = (long)0)]
        public long Time { get; set; }

        [Cfg]
        public List<LogEntry> Log { get; set; }

        [Cfg(value = "")]
        public string Environment { get; set; }

        [Cfg]
        public List<Environment> Environments { get; set; }

        public Process(
            string cfg,
            IDictionary<string, string> parameters,
            params IDependency[] dependencies)
        : base(dependencies) {
            Load(cfg, parameters);
        }

        public Process(string cfg, params IDependency[] dependencies) : this(cfg, null, dependencies) { }

        public Process(params IDependency[] dependencies) : base(dependencies) { }

        public Process() : base(new XmlSerializer()) { }

        /// <summary>
        /// A name (of your choosing) to identify the process.
        /// </summary>
        [Cfg(value = "", required = true, unique = true)]
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                if (value == null)
                    return;
                if (LogLimit == 0) {
                    LogLimit = value.Length;
                }
            }
        }

        /// <summary>
        /// Optional.
        ///
        /// `True` by default.
        /// 
        /// Indicates the process is enabled.  The included executable (e.g. `tfl.exe`) 
        /// respects this setting and does not run the process if disabled (or `False`).
        /// </summary>
        [Cfg(value = true)]
        public bool Enabled { get; set; }

        /// <summary>
        /// Optional. 
        /// 
        /// A mode reflects the intent of running the process.
        ///  
        /// * `init` wipes everything out
        /// * `validate` just loads, validates the configuration
        /// * <strong>`default`</strong> moves data through the pipeline, from input to output.
        /// 
        /// Aside from these, you may use any mode (of your choosing).  Then, you can control
        /// whether or not templates and/or actions run by setting their modes.
        /// </summary>
        [Cfg(value = "", toLower = true)]
        public string Mode { get; set; }

        /// <summary>
        /// Optional.  Default is `false`
        /// 
        /// If true, process entities in parallel.  If false, process them one by one in their configuration order.
        /// 
        /// Parallel *on* allows you to process all the entities at the same time, potentially faster.
        /// Parallel *off* allows you to have one entity depend on a previous entity's data.
        /// </summary>
        [Cfg(value = false)]
        public bool Parallel { get; set; }

        /// <summary>
        /// Optional.
        /// 
        /// A choice between `defer`, `linq`, `parallel.linq`, `streams`, `parallel.streams`.
        /// 
        /// The default `defer` defers this decision to the entity's Pipeline setting.
        /// </summary>
        [Cfg(value = "defer", domain = "defer,linq,parallel.linq", toLower = true)]
        public string Pipeline { get; set; }

        /// <summary>
        /// Optional (Name + "Star").
        /// 
        /// If your output is a relational database that supports views,
        /// this is the name of a view that projects fields from all the entities in the
        /// star-schema to a a single flat projection.
        /// 
        /// If not set, it is the combination of the process name, and "Star." 
        /// </summary>
        [Cfg(value = "")]
        public string Star { get; set; }

        /// <summary>
        /// Optional (false)
        /// 
        /// If set to true, a <see cref="Flat"/> table is created with the same structure as the <see cref="Star"/> view, and all the data is copied into it.
        /// </summary>
        [Cfg(value = false)]
        public bool Flatten { get; set; }

        /// <summary>
        /// Optional (Name + "Flat")
        /// </summary>
        [Cfg(value = "")]
        public string Flat { get; set; }

        /// <summary>
        /// Optional.
        /// 
        /// Choices are `html` and <strong>`raw`</strong>.
        /// 
        /// This refers to the razor templating engine's content type.  If you're rendering HTML 
        /// markup, use `html`, if not, using `raw` may inprove performance.
        /// </summary>
        [Cfg(value = "raw", domain = "raw,html", toLower = true)]
        public string TemplateContentType { get; set; }

        /// <summary>
        /// Optional.
        /// 
        /// Indicates the data's time zone.
        /// 
        /// It is used as the `to-time-zone` setting for `now()` and `timezone()` transformations
        /// if the `to-time-zone` is not set.
        /// 
        /// NOTE: Normally, you should keep the dates in UTC until presented to the user. 
        /// Then, have the client application convert UTC to the user's time zone.
        /// </summary>
        [Cfg(value = "")]
        public string TimeZone { get; set; }

        /// <summary>
        /// A collection of [Actions](/action)
        /// </summary>
        [Cfg]
        public List<Action> Actions { get; set; }

        /// <summary>
        /// A collection of [Calculated Fields](/calculated-field)
        /// </summary>
        [Cfg]
        public List<Field> CalculatedFields { get; set; }

        /// <summary>
        /// A collection of [Connections](/connection)
        /// </summary>
        [Cfg(required = false)]
        public List<Connection> Connections { get; set; }

        /// <summary>
        /// A collection of [Entities](/entity)
        /// </summary>
        [Cfg(required = false)]
        public List<Entity> Entities { get; set; }

        /// <summary>
        /// A collection of [Maps](/map)
        /// </summary>
        [Cfg]
        public List<Map> Maps { get; set; }

        /// <summary>
        /// A collection of [Relationships](/relationship)
        /// </summary>
        [Cfg]
        public List<Relationship> Relationships { get; set; }

        /// <summary>
        /// A collection of [Scripts](/script)
        /// </summary>
        [Cfg]
        public List<Script> Scripts { get; set; }

        /// <summary>
        /// A collection of [Search Types](/search-type)
        /// </summary>
        [Cfg]
        public List<SearchType> SearchTypes { get; set; }

        /// <summary>
        /// A collection of [Templates](/template)
        /// </summary>
        [Cfg]
        public List<Template> Templates { get; set; }

        [Cfg]
        public List<Parameter> Parameters { get; set; }

        protected override void PreValidate() {
            this.PreValidate(e => Error(e), w => Warn(w));
        }

        public void ModifyLogLimits() {
            var entitiesAny = Entities.Any();
            var fieldsAny = GetAllFields().Any(f => f.Transforms.Any());
            var transformsAny = GetAllTransforms().Any();

            LogLimit = Name.Length;
            EntityLogLimit = entitiesAny ? Entities.Select(e => e.Alias.Length).Max() : 10;
            FieldLogLimit = fieldsAny ? GetAllFields().Where(f => f.Transforms.Any()).Select(f => f.Alias.Length).Max() : 10;
            TransformLogLimit = transformsAny ? GetAllTransforms().Select(t => t.Method.Length).Max() : 10;
        }

        public void ModifyKeys() {

            // entities
            foreach (var entity in Entities) {
                entity.Key = Name + entity.Alias;
            }

            // templates
            foreach (var template in Templates) {
                template.Key = Name + template.Name;
            }

            // actions do not have unique names, so they get a counter too
            var actionIndex = 0;
            foreach (var action in Actions) {
                action.Key = action.Key + action.Type + ++actionIndex;
            }

            // connections
            foreach (var connection in Connections) {
                connection.Key = Name + connection.Name;
            }
        }

        /// <summary>
        /// Log limits, set by ModifyLogLimits
        /// </summary>
        public int LogLimit { get; set; }
        public int EntityLogLimit { get; set; }
        public int FieldLogLimit { get; set; }
        public int TransformLogLimit { get; set; }

        protected override void Validate() {
            this.Validate(e => Error(e), w => Warn(w));
        }

        public List<Field> ParametersToFields(IEnumerable<Parameter> parameters, Field defaultField) {

            if (defaultField == null) {
                throw new ArgumentNullException(nameof(defaultField));
            }

            var fields = parameters
                .Where(p => p.IsField(this))
                .Select(p => p.AsField(this))
                .ToList();

            if (!fields.Any()) {
                fields.Add(defaultField);
            }
            return fields;
        }

        protected override void PostValidate() {
            if (Errors().Length != 0)
                return;

            if (Entities.Any()) {
                Entities.First().IsMaster = true;
            }
            ModifyKeys();
            ModifyLogLimits();
            ModifyRelationshipToMaster();
            ModifyIndexes();

            // create entity field's matcher
            foreach (var entity in Entities) {
                var pattern = string.Join("|", entity.GetAllFields().Where(f => !f.System).Select(f => f.Alias));
#if NETS10
                entity.FieldMatcher = new Regex(pattern);
#else
                entity.FieldMatcher = new Regex(pattern, RegexOptions.Compiled);
#endif
            }

        }

        public void ModifyIndexes() {
            for (short i = 0; i < Entities.Count; i++) {

                var context = new PipelineContext(new NullLogger(), this, Entities[i]);
                context.Entity.Index = i;

                foreach (var field in context.Entity.GetAllFields()) {
                    field.EntityIndex = i;
                }

                if (!context.Entity.IsMaster)
                    continue;

                // set the master indexes
                short masterIndex = -1;
                var fields = context.GetAllEntityFields().ToArray();
                foreach (var field in fields) {
                    field.MasterIndex = ++masterIndex;
                }

                // set the process calculated fields starting where master entity fields left off
                if (!CalculatedFields.Any())
                    continue;

                var index = fields.Where(f => f.Index < short.MaxValue).Select(f => f.Index).Max();
                foreach (var field in CalculatedFields) {
                    field.Index = ++index;
                }
            }

            foreach (var field in GetAllFields()) {
                var tCount = 0;
                foreach (var transform in field.Transforms) {
                    transform.Index = tCount++;
                }
            }

        }

        void ModifyRelationshipToMaster() {
            foreach (var entity in Entities) {
                entity.RelationshipToMaster = ReadRelationshipToMaster(entity);
            }
        }

        IEnumerable<Relationship> ReadRelationshipToMaster(Entity entity) {

            if (entity.IsMaster)
                return new List<Relationship>();

            var relationships = Relationships.Where(r => r.Summary.RightEntity.Equals(entity)).ToList();

            if (relationships.Any() && !relationships.Any(r => r.Summary.LeftEntity.IsMaster)) {
                var leftEntity = relationships.Last().Summary.LeftEntity;
                relationships.AddRange(ReadRelationshipToMaster(leftEntity));
            }
            return relationships;
        }

        public IEnumerable<Transform> GetAllTransforms() {
            var transforms = Entities.SelectMany(entity => entity.GetAllTransforms()).ToList();
            transforms.AddRange(CalculatedFields.SelectMany(field => field.Transforms));
            return transforms;
        }

        public Entity GetEntity(string nameOrAlias) {
            var entity = Entities.FirstOrDefault(e => e.Alias == nameOrAlias);
            return entity ?? Entities.FirstOrDefault(e => e.Name != e.Alias && e.Name == nameOrAlias);
        }

        public bool TryGetEntity(string nameOrAlias, out Entity entity) {
            entity = GetEntity(nameOrAlias);
            return entity != null;
        }

        public IEnumerable<Field> GetAllFields() {
            var fields = new List<Field>();
            foreach (var e in Entities) {
                fields.AddRange(e.GetAllFields());
            }
            fields.AddRange(CalculatedFields);
            return fields;
        }

        public bool HasMultivaluedSearchType() {
            return GetAllFields().Select(f => f.SearchType).Distinct().Any(st => SearchTypes.First(s => s.Name == st).MultiValued);
        }

        public IEnumerable<Field> GetSearchFields() {
            var fields = new List<Field>();
            var starFields = GetStarFields().ToArray();

            fields.AddRange(starFields[0].Where(f => f.SearchType != "none"));
            fields.AddRange(starFields[1].Where(f => f.SearchType != "none"));
            return fields;
        }

        public IEnumerable<List<Field>> GetStarFields() {
            const int master = 0;
            const int other = 1;

            var starFields = new List<Field>[2];

            starFields[master] = new List<Field>();
            starFields[other] = new List<Field>();

            foreach (var entity in Entities) {
                if (entity.IsMaster) {
                    starFields[master].AddRange(new PipelineContext(new NullLogger(), this, entity).GetAllEntityOutputFields());
                } else {
                    starFields[other].AddRange(entity.GetAllOutputFields().Where(f => f.KeyType == KeyType.None && !f.System && f.Type != "byte[]"));
                }
            }
            return starFields;
        }

        public Connection Output() {
            return Connections.FirstOrDefault(c => c.Name == "output");
        }

        /// <summary>
        /// clone process, remove entities, and create entity needed for calculated fields
        /// </summary>
        /// <returns>A made-up process that represents the denormalized output's fields that contribute to calculated fields</returns>
        public Process ToCalculatedFieldsProcess() {
            // clone process, remove entities, and create entity needed for calculated fields
            var calc = this.Clone();
            calc.LogLimit = LogLimit;
            calc.EntityLogLimit = EntityLogLimit;
            calc.FieldLogLimit = FieldLogLimit;
            calc.TransformLogLimit = TransformLogLimit;

            calc.Entities.Clear();
            calc.CalculatedFields.Clear();
            calc.Relationships.Clear();

            var entity = new Entity();
            entity.Name = "Calculated";
            entity.Alias = entity.Name;
            entity.Key = calc.Name + entity.Alias;
            entity.Connection = "output";
            entity.Fields.Add(new Field {
                Name = Constants.TflKey,
                Alias = Constants.TflKey,
                PrimaryKey = true,
                System = true,
                Input = true,
                Type = "int"
            });

            // Add fields that calculated fields depend on
            entity.Fields.AddRange(CalculatedFields
                .SelectMany(f => f.Transforms)
                .SelectMany(t => t.Parameters)
                .Where(p => !p.HasValue() && p.IsField(this))
                .Select(p => p.AsField(this).Clone())
                .Where(f => f.Output)
                .Distinct()
                .Except(CalculatedFields)
            );

            var mapFields = CalculatedFields
                .SelectMany(cf => cf.Transforms)
                .Where(t => t.Method == "map")
                .Select(t => Maps.First(m => m.Name == t.Map))
                .SelectMany(m => m.Items)
                .Where(i => i.Parameter != string.Empty)
                .Select(i => i.AsParameter().AsField(this))
                .Distinct()
                .Except(entity.Fields)
                .Select(f => f.Clone());

            entity.Fields.AddRange(mapFields);

            entity.CalculatedFields.AddRange(CalculatedFields.Select(cf => cf.Clone()));
            foreach (var parameter in entity.GetAllFields().SelectMany(f => f.Transforms).SelectMany(t => t.Parameters)) {
                parameter.Entity = string.Empty;
            }

            foreach (var field in entity.Fields) {
                field.Source = Utility.GetExcelName(field.EntityIndex) + "." + field.FieldName();
            }

            foreach (var field in entity.CalculatedFields) {
                field.Source = Utility.GetExcelName(field.EntityIndex) + "." + field.FieldName();
            }

            entity.ModifyIndexes();
            calc.Entities.Add(entity);
            calc.ModifyKeys();
            calc.ModifyIndexes();
            return calc;
        }

        public bool TryGetField(string aliasOrName, out Field field) {
            foreach (var f in Entities.Select(entity => entity.GetField(aliasOrName)).Where(f => f != null)) {
                field = f;
                return true;
            }
            field = null;
            return false;
        }

        [Cfg(value = "")]
        public string Description { get; set; }

        [Cfg(value = "")]
        public string MaxMemory { get; set; }

        [Cfg]
        public List<Schedule> Schedule { get; set; }

        public bool Preserve { get; set; }

        [Cfg]
        public List<CfgRow> Rows { get; set; }

        [Cfg(value = false)]
        public bool ReadOnly { get; set; }

        [Cfg(value = "sqlite", domain = "sqlite,sqlce", toLower = true)]
        public string InternalProvider { get; set; }

        [Cfg(value=false)]
        public bool Buffer { get; set; }

        public List<Parameter> GetActiveParameters() {
            if (!Environments.Any())
                return new List<Parameter>();

            return string.IsNullOrEmpty(Environment) ? Environments.First().Parameters : Environments.First(e => e.Name == Environment).Parameters;
        }

        public void Dispose() {
            if (Preserve)
                return;

            Log?.Clear();
            Entities?.Clear();
            Actions?.Clear();
            CalculatedFields?.Clear();
            Connections?.Clear();
            Environments?.Clear();
            Maps?.Clear();
            Relationships?.Clear();
            Scripts?.Clear();
            SearchTypes?.Clear();
            Templates?.Clear();
        }

        public bool IsFirstRun() {
            return !Entities.Any() || Entities.First().IsFirstRun;
        }

        public bool OutputIsRelational() {
            return Output() != null && Constants.AdoProviderSet().Contains(Output().Provider);
        }

        public bool OutputIsConsole() {
            // check if this is a master job with actions, and no real entities
            if (Actions.Count > 0 && Entities.Count == 1 && Entities.First().GetAllFields().All(f => f.System)) {
                return false;
            }
            return Connections.Any(c => c.Name == Constants.OriginalOutput && c.Provider == "console" || c.Name == "output" && c.Provider == "console");
        }
    }
}