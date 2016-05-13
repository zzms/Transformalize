#region license
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

using Pipeline.Context;
using Pipeline.Contracts;
using Pipeline.Extensions;

namespace Pipeline.Transforms {
   public class LeftTransform : BaseTransform, ITransform {

      readonly int _length;
      readonly IField _input;

      public LeftTransform(PipelineContext context)
            : base(context) {
         _length = context.Transform.Length;
         _input = SingleInput();
      }

      public IRow Transform(IRow row) {
         row.SetString(Context.Field, row.GetString(_input).Left(_length));
         Increment();
         return row;
      }

   }
}