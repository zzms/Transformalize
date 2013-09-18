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

using System.Data.SqlClient;

namespace Transformalize.Main.Providers.SqlServer
{
    public class SqlServerEntityBatchReader : IEntityBatchReader
    {
        public int ReadNext(Entity entity)
        {
            using (var cn = new SqlConnection(entity.OutputConnection.ConnectionString))
            {
                cn.Open();
                var cmd = new SqlCommand("SELECT ISNULL(MAX(TflBatchId),0)+1 FROM [dbo].[TflBatch] WHERE ProcessName = @ProcessName;", cn);
                cmd.Parameters.Add(new SqlParameter("@ProcessName", entity.ProcessName));
                return (int) cmd.ExecuteScalar();
            }
        }
    }
}