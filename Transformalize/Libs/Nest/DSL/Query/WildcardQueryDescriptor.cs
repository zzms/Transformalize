﻿using System;
using System.Linq.Expressions;
using Transformalize.Libs.Newtonsoft.Json;
using Transformalize.Libs.Newtonsoft.Json.Converters;
using Transformalize.Libs.Nest.Domain.Marker;
using Transformalize.Libs.Nest.DSL.Query.Behaviour;
using Transformalize.Libs.Nest.Enums;

namespace Transformalize.Libs.Nest.DSL.Query
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public interface IWildcardQuery : ITermQuery
	{
		[JsonProperty(PropertyName = "rewrite")]
		[JsonConverter(typeof (StringEnumConverter))]
		RewriteMultiTerm? Rewrite { get; set; }
	}

	public class WildcardQuery<T> : WildcardQuery
		where T : class
	{
		public WildcardQuery(Expression<Func<T, object>> field)
		{
			this.Field = field;
		}
	}
	public class WildcardQuery : PlainQuery, IWildcardQuery
	{
		bool IQuery.IsConditionless { get { return false; } }
		PropertyPathMarker IFieldNameQuery.GetFieldName() { return this.Field; }

		void IFieldNameQuery.SetFieldName(string fieldName) { this.Field = fieldName; }

		public PropertyPathMarker Field { get; set; }
		public object Value { get; set; }
		public double? Boost { get; set; }
		public RewriteMultiTerm? Rewrite { get; set; }


		protected override void WrapInContainer(IQueryContainer container)
		{
			container.Wildcard = this;
		}
	}

	public class WildcardQueryDescriptor<T> : 
		TermQueryDescriptorBase<WildcardQueryDescriptor<T>, T>, 
		IWildcardQuery 
		where T : class
	{
		RewriteMultiTerm? IWildcardQuery.Rewrite { get; set; }

		public WildcardQueryDescriptor<T> Rewrite(RewriteMultiTerm rewrite)
		{
			((IWildcardQuery)this).Rewrite = rewrite;
			return this;
		}

		PropertyPathMarker IFieldNameQuery.GetFieldName()
		{
			return ((IWildcardQuery)this).Field;
		}

		void IFieldNameQuery.SetFieldName(string fieldName)
		{
			((IWildcardQuery)this).Field = fieldName;
		}
	}
}