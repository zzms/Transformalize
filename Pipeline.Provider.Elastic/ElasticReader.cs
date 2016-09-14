﻿#region license
// Transformalize
// A Configurable ETL Solution Specializing in Incremental Denormalization.
// Copyright 2013 Dale Newman
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
using Pipeline.Contracts;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using Elasticsearch.Net;
using Newtonsoft.Json;
using Pipeline.Configuration;

namespace Pipeline.Provider.Elastic {

    public class ElasticReader : IRead {

        readonly IElasticLowLevelClient _client;
        readonly IConnectionContext _context;
        readonly Field[] _fields;
        readonly string[] _fieldNames;
        readonly IRowFactory _rowFactory;
        readonly ReadFrom _readFrom;
        readonly string _typeName;

        public ElasticReader(
            IConnectionContext context,
            Field[] fields,
            IElasticLowLevelClient client,
            IRowFactory rowFactory,
            ReadFrom readFrom
        ) {

            _context = context;
            _fields = fields;
            _fieldNames = fields.Select(f => _readFrom == ReadFrom.Input ? f.Name : f.Alias.ToLower()).ToArray();
            _client = client;
            _rowFactory = rowFactory;
            _readFrom = readFrom;
            _typeName = readFrom == ReadFrom.Input ? context.Entity.Name : context.Entity.Alias.ToLower();

        }

        private string WriteQuery(
            IEnumerable<Field> fields,
            ReadFrom readFrom,
            IContext context,
            int from = 0,
            int size = 10
        ) {

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);

            using (var writer = new JsonTextWriter(sw)) {
                writer.WriteStartObject();

                writer.WritePropertyName("from");
                writer.WriteValue(from);
                writer.WritePropertyName("size");
                writer.WriteValue(size);

                writer.WritePropertyName("_source");
                writer.WriteStartObject();
                writer.WritePropertyName("includes");
                writer.WriteStartArray();
                foreach (var field in fields) {
                    writer.WriteValue(readFrom == ReadFrom.Input ? field.Name : field.Alias.ToLower());
                }
                writer.WriteEndArray();
                writer.WriteEndObject();

                if (readFrom == ReadFrom.Input) {
                    if (context.Entity.Filter.Any()) {
                        writer.WritePropertyName("query");
                        writer.WriteStartObject();
                        writer.WritePropertyName("constant_score");
                        writer.WriteStartObject();
                        writer.WritePropertyName("filter");
                        writer.WriteStartObject();
                        writer.WritePropertyName("bool");
                        writer.WriteStartObject();
                        writer.WritePropertyName("must");
                        writer.WriteStartArray();

                        foreach (var filter in context.Entity.Filter) {
                            writer.WriteStartObject();
                            writer.WritePropertyName("term");
                            writer.WriteStartObject();
                            writer.WritePropertyName(filter.LeftField.Name);
                            writer.WriteValue(filter.Value);
                            writer.WriteEndObject();
                            writer.WriteEndObject();
                        }

                        writer.WriteEndArray();
                        writer.WriteEndObject();
                        writer.WriteEndObject();
                        writer.WriteEndObject();
                        writer.WriteEndObject();
                    }
                } else {
                    writer.WritePropertyName("query");
                    writer.WriteStartObject();
                    writer.WritePropertyName("constant_score");
                    writer.WriteStartObject();
                    writer.WritePropertyName("filter");
                    writer.WriteStartObject();
                    writer.WritePropertyName("term");
                    writer.WriteStartObject();
                    writer.WritePropertyName("tfldeleted");
                    writer.WriteValue(false);
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                    writer.WriteEndObject();

                }

                if (context.Entity.Order.Any()) {
                    writer.WritePropertyName("sort");
                    writer.WriteStartArray();

                    foreach (var orderBy in context.Entity.Order) {
                        Field field;
                        if (context.Entity.TryGetField(orderBy.Field, out field)) {
                            var name = _readFrom == ReadFrom.Input ? field.Name : field.Alias.ToLower();
                            writer.WriteStartObject();
                            writer.WritePropertyName(name);
                            writer.WriteStartObject();
                            writer.WritePropertyName("order");
                            writer.WriteValue(orderBy.Sort);
                            writer.WriteEndObject();
                            writer.WriteEndObject();
                        }
                    }

                    writer.WriteEndArray();

                }

                writer.WriteEndObject();
                writer.Flush();
                return sb.ToString();
            }

        }

        public IEnumerable<IRow> Read() {
            ElasticsearchResponse<DynamicResponse> response;

            string body;
            if (_context.Entity.IsPageRequest()) {
                var from = (_context.Entity.Page * _context.Entity.PageSize) - _context.Entity.PageSize;
                body = WriteQuery(_fields, _readFrom, _context, from, _context.Entity.PageSize);
            } else {
                body = WriteQuery(_fields, _readFrom, _context, 0, 0);
                response = _client.Search<DynamicResponse>(_context.Connection.Index, _typeName, body);
                if (response.Success) {
                    var hits = response.Body["hits"] as ElasticsearchDynamicValue;
                    if (hits != null && hits.HasValue) {
                        var properties = hits.Value as IDictionary<string, object>;
                        if (properties != null && properties.ContainsKey("total")) {
                            var size = Convert.ToInt32(properties["total"]) + 1;
                            body = WriteQuery(_fields, _readFrom, _context, 0, size);
                        }
                    }
                }
            }
            _context.Debug(() => body);

            response = _client.Search<DynamicResponse>(_context.Connection.Index, _typeName, body);

            if (response.Success) {
                _context.Entity.Hits = Convert.ToInt32((response.Body["hits"]["total"] as ElasticsearchDynamicValue).Value);
                var hits = response.Body["hits"]["hits"] as ElasticsearchDynamicValue;
                if (hits != null && hits.HasValue) {
                    var docs = hits.Value as IEnumerable<object>;
                    if (docs != null) {
                        foreach (var doc in docs) {
                            var row = _rowFactory.Create();
                            var dict = doc as IDictionary<string, object>;
                            if (dict != null && dict.ContainsKey("_source")) {
                                var source = dict["_source"] as IDictionary<string, object>;
                                if (source != null) {
                                    for (var i = 0; i < _fields.Length; i++) {
                                        var field = _fields[i];
                                        row[field] = field.Convert(source[_fieldNames[i]]);
                                    }
                                }
                            }
                            _context.Increment();
                            yield return row;
                        }
                    }
                }

            } else {
                _context.Error(response.DebugInformation);
            }

        }
    }
}
