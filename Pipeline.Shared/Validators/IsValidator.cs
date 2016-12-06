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
using Transformalize.Configuration;
using Transformalize.Contracts;
using Transformalize.Transforms;

namespace Transformalize.Validators {
    public class IsValidator : BaseTransform {
      readonly Field _input;
      readonly Func<string, object> _canConvert;

      public IsValidator(IContext context)
            : base(context, "bool") {
            _input = SingleInput();
            if (context.Field.Type.StartsWith("bool", StringComparison.Ordinal)) {
                _canConvert = v => Constants.CanConvert()[context.Transform.Type](v);
            } else {
                _canConvert = v => Constants.CanConvert()[context.Transform.Type](v) ? 
                    string.Empty :
                    $"The value {v} can not be converted to a {context.Transform.Type}.";
            }
        }

        public override IRow Transform(IRow row) {
            row[Context.Field] = _canConvert(row[_input] as string);
            Increment();
            return row;
        }

    }
}