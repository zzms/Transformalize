#region license
// Transformalize
// Configurable Extract, Transform, and Load
// Copyright 2013-2016 Dale Newman
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
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Transformalize.Transforms {

    public enum FilterType {
        Exclude,
        Include
    }

    public class FilterTransform : BaseTransform {
        private readonly Func<IRow, bool> _filter;
        private readonly FilterType _filterType;

        public FilterTransform(IContext context, FilterType filterType) : base(context, null) {
            _filterType = filterType;
            _filter = GetFunc(SingleInput(), context.Transform.Operator, SingleInput().Convert(context.Transform.Value));
        }

        public override IRow Transform(IRow row) {
            throw new NotImplementedException();
        }

        public override IEnumerable<IRow> Transform(IEnumerable<IRow> rows) {
            return _filterType == FilterType.Include ? rows.Where(row => _filter(row)) : rows.Where(row => !_filter(row));
        }

        public static Func<IRow, bool> GetFunc(Field input, string @operator, object value) {
            // equal,notequal,lessthan,greaterthan,lessthanequal,greaterthanequal,=,==,!=,<,<=,>,>=
            switch (@operator) {
                case "notequal":
                case "!=":
                    return row => row[input].Equals(value);
                case "lessthan":
                case "<":
                    return row => ((IComparable)row[input]).CompareTo(value) < 0;
                case "lessthanequal":
                case "<=":
                    return row => ((IComparable)row[input]).CompareTo(value) < 0;
                case "greaterthan":
                case ">":
                    return row => ((IComparable)row[input]).CompareTo(value) > 0;
                case "greaterthanequal":
                case ">=":
                    return row => ((IComparable)row[input]).CompareTo(value) >= 0;
                default:
                    return row => row[input].Equals(value);
            }
        }
    }
}