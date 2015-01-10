using System.Reflection;
using System;

namespace Serialization
{

    public abstract class GetSet
    {
		public int Priority = 100;
        public PropertyInfo Info;
        public string Name;
        public FieldInfo FieldInfo;
        public object Vanilla;
        public bool CollectionType;
        public Func<object, object> Get;
		public Action<object, object> Set;
		public bool IsStatic;
		
		public MemberInfo MemberInfo
		{
			get
			{
				return Info != null ? (MemberInfo)Info : (MemberInfo)FieldInfo;
			}
		}
    }
}