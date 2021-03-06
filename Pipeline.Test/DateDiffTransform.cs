﻿#region license
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

using System.Linq;
using NUnit.Framework;

namespace Transformalize.Test {

    [TestFixture]
    public class DateDiffTransform {

        [Test(Description = "DateDiff Transformation")]
        public void DateDiff1() {

            var xml = @"
    <add name='TestProcess'>
      <entities>
        <add name='TestData' >
          <rows>
            <add StartDate='2016-06-01' EndDate='2016-08-01' />
          </rows>
          <fields>
            <add name='StartDate' type='datetime' />
            <add name='EndDate' type='datetime' />
          </fields>
          <calculated-fields>
            <add name='Years' type='int' t='copy(StartDate,EndDate).datediff(year)' />
            <add name='Days' type='int' t='copy(StartDate,EndDate).datediff(day)' />
            <add name='Minutes' type='int' t='copy(StartDate,EndDate).datediff(minute)' />
            <add name='Hours' type='double' t='copy(StartDate,EndDate).datediff(hour)' />
            <add name='MovingUtc' type='int' t='copy(EndDate).datediff(hour,UTC)' />
            <add name='MovingEst' type='int' t='copy(EndDate).datediff(hour,Eastern Standard Time)' />
          </calculated-fields>
        </add>
      </entities>
    </add>
            ".Replace('\'', '"');

            var composer = new CompositionRoot();
            var controller = composer.Compose(xml);
            var output = controller.Read().ToArray();

            var cf = composer.Process.Entities.First().CalculatedFields.ToArray();
            Assert.AreEqual(0, output[0][cf[0]]);
            Assert.AreEqual(61, output[0][cf[1]]);
            Assert.AreEqual(87840, output[0][cf[2]]);
            Assert.AreEqual(1464, output[0][cf[3]]);
            //Assert.AreEqual(1640, output[0][cf[4]]);
            //Assert.AreEqual(1644, output[0][cf[5]]);
        }
    }
}
