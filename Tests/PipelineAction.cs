﻿#region license
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
using Autofac;
using Transformalize.Actions;
using Transformalize.Configuration;
using Transformalize.Contracts;
using Transformalize.Ioc.Autofac;

namespace Tests {
    public class PipelineAction : IAction {

        private readonly Process _process;
        private readonly IContext _context;

        public PipelineAction(Process process, IContext context) {
            _process = process;
            _context = context;
        }

        public ActionResponse Execute() {
            var response = new ActionResponse();
            if (!_process.Enabled) {
                response.Code = 503;
                response.Message = "Process is disabled.";
                return response;

            }

            using (var scope = DefaultContainer.Create(_process, _context?.Logger)) {
                scope.Resolve<IProcessController>().Execute();
            }

            return response;
        }
    }
}
