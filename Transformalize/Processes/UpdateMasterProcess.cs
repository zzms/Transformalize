/*
Transformalize - Replicate, Transform, and Denormalize Your Data...
Copyright (C) 2013 Dale Newman

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Linq;
using Transformalize.Libs.Rhino.Etl.Core;
using Transformalize.Model;
using Transformalize.Operations;

namespace Transformalize.Processes {

    public class UpdateMasterProcess : EtlProcess {

        private readonly Process _process;

        public UpdateMasterProcess(ref Process process)
            : base(process.Name) {
            _process = process;
        }

        protected override void Initialize() {
            var last = _process.Entities.Last().Name;
            foreach (var entity in _process.Entities) {
                if (entity.Name.Equals(last))
                    RegisterLast(new EntityUpdateMaster(_process, entity));
                else {
                    Register(new EntityUpdateMaster(_process, entity));
                }
            }
        }

        protected override void PostProcessing() {

            var errors = GetAllErrors().ToArray();
            if (errors.Any()) {
                foreach (var error in errors) {
                    Error(error.InnerException, "Message: {0}\r\nStackTrace:{1}\r\n", error.Message, error.StackTrace);
                }
                throw new InvalidOperationException("Houstan.  We have a problem.");
            }

            base.PostProcessing();
        }

    }

}
