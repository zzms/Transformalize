using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Transformalize.Libs.Rhino.Etl;
using Transformalize.Libs.Rhino.Etl.Operations;
using Transformalize.Main;

namespace Transformalize.Operations.Transform {
    public class FromXmlOperation : AbstractOperation {

        private const StringComparison IC = StringComparison.OrdinalIgnoreCase;
        private readonly string _inKey;
        private readonly bool _searchAttributes;
        private readonly Dictionary<string, Field> _nameMap = new Dictionary<string, Field>();
        private static readonly XmlReaderSettings Settings = new XmlReaderSettings {
            IgnoreWhitespace = true,
            IgnoreComments = true
        };

        public FromXmlOperation(string inKey, IEnumerable<KeyValuePair<string, Field>> fields) {

            _inKey = inKey;

            foreach (var field in fields) {
                if (!_searchAttributes && field.Value.NodeType.Equals("attribute", IC)) {
                    _searchAttributes = true;
                }
                _nameMap[field.Value.Name] = field.Value;
            }

        }

        public override IEnumerable<Row> Execute(IEnumerable<Row> rows) {
            foreach (var row in rows)
            {
                string startKey = null;
                var innerRows = new List<Row>();
                
                using (var reader = XmlReader.Create(new StringReader(row[_inKey].ToString()), Settings)) {
                    while (reader.Read()) {

                        if (_nameMap.ContainsKey(reader.Name)) {

                            // must while here because reader.Read*Xml advances the reader
                            while (_nameMap.ContainsKey(reader.Name) && reader.IsStartElement()) {
                                InnerRow(ref startKey, reader.Name, row, ref innerRows);

                                var field = _nameMap[reader.Name];
                                var value = field.ReadInnerXml ? reader.ReadInnerXml() : reader.ReadOuterXml();
                                if (value != string.Empty)
                                    row[field.Alias] = Common.ConversionMap[field.SimpleType](value);
                            }

                        } else if (_searchAttributes && reader.HasAttributes) {
                            for (var i = 0; i < reader.AttributeCount; i++) {
                                reader.MoveToNextAttribute();
                                if (!_nameMap.ContainsKey(reader.Name))
                                    continue;

                                InnerRow(ref startKey, reader.Name, row, ref innerRows);

                                var field = _nameMap[reader.Name];
                                if (!string.IsNullOrEmpty(reader.Value)) {
                                    row[field.Alias] = Common.ConversionMap[field.SimpleType](reader.Value);
                                }
                            }
                        }
                    }
                }
                AddInnerRow(row, ref innerRows);
                foreach (var innerRow in innerRows) {
                    yield return innerRow;
                }
            }
        }

        private static bool ShouldYieldRow(ref string startKey, string key) {
            if (startKey == null) {
                startKey = key;
            } else if (startKey.Equals(key)) {
                return true;
            }
            return false;
        }

        private void InnerRow(ref string startKey, string key, Row row, ref List<Row> innerRows) {
            if (!ShouldYieldRow(ref startKey, key))
                return;

            AddInnerRow(row, ref innerRows);
        }

        private void AddInnerRow(Row row, ref List<Row> innerRows) {
            var innerRow = row.Clone();
            innerRow[_inKey] = string.Empty;
            innerRows.Add(innerRow);
        }
    }

}
